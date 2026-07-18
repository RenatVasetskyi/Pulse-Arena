using System;
using Architecture.Services.Interfaces;
using Game.Cameras;
using Game.Combat;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     One HUD canvas for the game scene. Holds the sub-views (health/score/wave/zoom) and
    ///     the touch controls (joystick + lasso button), and wires each to its source. All refs
    ///     are optional (null-safe) so a partial HUD works. Assign the components on the prefab root.
    /// </summary>
    public class GameHud : MonoBehaviour, ITouchInput
    {
        [SerializeField] private HudHealthView _health;
        [SerializeField] private HudScoreView _score;
        [SerializeField] private HudWaveView _wave;
        [SerializeField] private HudZoomView _zoom;
        [SerializeField] private HudToastView _toast;
        [SerializeField] private HudTensionView _tension;
        [SerializeField] private HudComboView _combo;
        [SerializeField] private HudDamageFlash _damageFlash;
        [SerializeField] private HudHintView _hint;

        [Header("Touch controls (mobile)")] [SerializeField]
        private VirtualJoystick _joystick;

        [SerializeField] private LassoButton _lassoButton;
        [SerializeField] private AbilityButton _dashButton;
        [SerializeField] private AbilityButton _ultimateButton;

        [Header("Pause")] [SerializeField] private Button _pauseButton;

        private int _lastHealth = -1;
        private bool _suppressTransient;

        private PlayerController _player;

        public event Action PauseRequested;

        Vector2 ITouchInput.Move => _joystick != null ? _joystick.Value : Vector2.zero;
        bool ITouchInput.LassoPressedThisFrame => _lassoButton != null && _lassoButton.PressedThisFrame;
        bool ITouchInput.LassoHeld => _lassoButton != null && _lassoButton.Held;
        bool ITouchInput.LassoReleasedThisFrame => _lassoButton != null && _lassoButton.ReleasedThisFrame;
        bool ITouchInput.DashPressedThisFrame => _dashButton != null && _dashButton.PressedThisFrame;
        bool ITouchInput.UltimatePressedThisFrame => _ultimateButton != null && _ultimateButton.PressedThisFrame;

        private void Awake()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                PauseRequested?.Invoke();

            if (_player != null && _dashButton != null)
                _dashButton.SetCharge(_player.DashCharge01);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.HealthChanged -= OnPlayerHealthChanged;
        }

        public void Bind(PlayerController player, IScoreService score, IBattleCamera camera, string scoreFormat)
        {
            if (_health != null)
                _health.Bind(player);

            if (_score != null)
                _score.Bind(score, scoreFormat);

            if (_zoom != null)
                _zoom.Bind(camera);

            _player = player;

            if (_player != null)
            {
                _lastHealth = _player.Health;
                _player.HealthChanged += OnPlayerHealthChanged;
            }
        }

        public void BindTension(EnemySlingshot slingshot)
        {
            if (_tension != null)
                _tension.Bind(slingshot);
        }

        public void SetCombo(int combo)
        {
            if (_combo == null)
                return;

            if (combo >= 2 && !_suppressTransient)
                _combo.Show(combo);
            else
                _combo.Hide();
        }

        // Onboarding suppresses the combo popup + pickup toasts so its hints read cleanly on the first game.
        public void SetTransientSuppressed(bool suppressed)
        {
            _suppressTransient = suppressed;

            if (suppressed)
                _combo?.Hide();
        }

        public void SetSuperCharge(float charge01)
        {
            // Ultimate charge shows on the ultimate button's radial fill.
            if (_ultimateButton != null)
                _ultimateButton.SetCharge(charge01);
        }

        public void SetWave(int current, int total)
        {
            if (_wave != null)
                _wave.SetWave(current, total);
        }

        public void ShowToast(string message, float duration)
        {
            if (_toast == null || _suppressTransient)
                return;

            _toast.Show(message, duration);
        }

        // autoHide <= 0 keeps the onboarding hint up until the next ShowHint/HideHint (learn-by-doing gating).
        public void ShowHint(string message, float autoHide)
        {
            if (_hint != null)
                _hint.Show(message, autoHide);
        }

        public void HideHint()
        {
            if (_hint != null)
                _hint.Hide();
        }

        // HUD-visual only: the full-screen damage flash. The camera hit-kick + SFX + haptics for the same event
        // live in GameplayFeedbackDirector (which also subscribes to HealthChanged) — the HUD stays passive.
        private void OnPlayerHealthChanged(int health, int maxHealth)
        {
            if (_lastHealth >= 0 && health < _lastHealth && _damageFlash != null)
                _damageFlash.Flash();

            _lastHealth = health;
        }
    }
}