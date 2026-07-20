using Architecture.Services.Interfaces;
using Data;
using Game.Combat;
using Game.Player;
using Game.Spawning;
using UI.Hud;

namespace Game.Scene
{
    /// <summary>
    ///     First-run tutorial: on the player's very first game it teaches the core loop by DOING — a grab prompt
    ///     holds until an enemy is grabbed, a fling prompt until the first throw, then the kill/win rule, plus
    ///     one-shot booster (first orb), dash (first hit) and ultimate (first charge) hints. Each is a
    ///     <see cref="HudHintView" /> card. Persisted via <see cref="IOnboardingState.OnboardingSeen" /> so it runs
    ///     once ever; bound/unbound by <see cref="GameWorldBuilder" /> like the other collaborators, a no-op once seen.
    /// </summary>
    public class OnboardingController
    {
        private const float Hold = 0f; // keep the hint up until the next Show/Hide

        private readonly GameSettings _gameSettings;
        private readonly IOnboardingState _onboarding;
        private readonly IPickupSpawner _pickupSpawner;
        private readonly ISuperMeterService _superMeterService;

        private bool _active;
        private bool _boosterShown;
        private bool _coreDone;
        private bool _dashShown;
        private GameHud _hud;
        private int _lastHealth;
        private int _maxHealth;
        private bool _orbAvailable;
        private PlayerController _player;
        private EnemySlingshot _slingshot;
        private bool _ultimateShown;

        public OnboardingController(IOnboardingState onboarding, ISuperMeterService superMeterService,
            IPickupSpawner pickupSpawner, GameSettings gameSettings)
        {
            _onboarding = onboarding;
            _superMeterService = superMeterService;
            _pickupSpawner = pickupSpawner;
            _gameSettings = gameSettings;
        }

        /// <summary>Start the tutorial for this match — a no-op (nothing subscribed) once the player has seen it.</summary>
        public void Bind(PlayerController player, GameHud hud)
        {
            if (_onboarding.OnboardingSeen || player == null || hud == null)
                return;

            _player = player;
            _hud = hud;
            _slingshot = player.Slingshot;
            _lastHealth = player.Health;
            _maxHealth = player.Health; // spawns at full; corrected on the first HealthChanged
            _active = true;

            _player.HealthChanged += OnPlayerHealthChanged;
            _superMeterService.Ready += OnSuperReady;
            _pickupSpawner.PickupSpawned += OnPickupSpawned;

            if (_slingshot != null)
            {
                _slingshot.EnemyGrabbed += OnEnemyGrabbed;
                _slingshot.EnemyLaunched += OnEnemyLaunched;
            }

            _hud.SetTransientSuppressed(true); // mute the combo popup + pickup toasts while the tutorial plays
            ShowHint(_gameSettings.Onboarding.GrabHint, Hold);
        }

        public void Unbind()
        {
            if (!_active)
                return;

            _active = false;
            _player.HealthChanged -= OnPlayerHealthChanged;
            _superMeterService.Ready -= OnSuperReady;
            _pickupSpawner.PickupSpawned -= OnPickupSpawned;

            if (_slingshot != null)
            {
                _slingshot.EnemyGrabbed -= OnEnemyGrabbed;
                _slingshot.EnemyLaunched -= OnEnemyLaunched;
            }

            _hud?.SetTransientSuppressed(false);
            _hud?.HideHint();
        }

        private void OnEnemyGrabbed()
        {
            if (_coreDone)
                return;

            ShowHint(_gameSettings.Onboarding.FlingHint, Hold);
        }

        // First successful throw = the core loop is learned; bank it so the tutorial never runs again, and teach the
        // kill + win rule. The booster/dash/ultimate one-shots below still fire for the rest of THIS match.
        private void OnEnemyLaunched(float chargeProgress)
        {
            if (_coreDone)
                return;

            _coreDone = true;
            ShowHint(_gameSettings.Onboarding.KillHint, _gameSettings.Onboarding.HintDuration);
            _onboarding.MarkOnboardingSeen();
        }

        // An orb is on the field now — teach boosters IF the player is already hurt (otherwise it's irrelevant).
        private void OnPickupSpawned(string message, float duration)
        {
            _orbAvailable = true;
            TryShowBoosterHint();
        }

        // Teach dash the moment it matters — the first time the player actually takes a hit. Also (re)checks the
        // booster hint, since taking damage may be what makes the heal advice relevant.
        private void OnPlayerHealthChanged(int health, int maxHealth)
        {
            bool tookDamage = health < _lastHealth;
            _lastHealth = health;
            _maxHealth = maxHealth;

            if (tookDamage && !_dashShown)
            {
                _dashShown = true;
                ShowHint(_gameSettings.Onboarding.DashHint, _gameSettings.Onboarding.HintDuration);
                return;
            }

            TryShowBoosterHint();
        }

        // The heal hint only helps when it is actionable: the player is below max HP AND an orb is (or has become)
        // available — fired from whichever of those two conditions completes the pair.
        private void TryShowBoosterHint()
        {
            if (_boosterShown || !_orbAvailable || _lastHealth >= _maxHealth)
                return;

            _boosterShown = true;
            ShowHint(_gameSettings.Onboarding.BoosterHint, _gameSettings.Onboarding.HintDuration);
        }

        private void OnSuperReady()
        {
            if (_ultimateShown)
                return;

            _ultimateShown = true;
            ShowHint(_gameSettings.Onboarding.UltimateHint, _gameSettings.Onboarding.HintDuration);
        }

        private void ShowHint(string message, float autoHide)
        {
            _hud?.ShowHint(message, autoHide);
        }
    }
}
