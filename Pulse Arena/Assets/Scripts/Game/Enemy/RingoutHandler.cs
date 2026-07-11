using System;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     Everything that happens when an enemy is scored: the guarded kill award (combo + score, once),
    ///     and — for a ringout specifically — the popup text, the audio sting and the particle burst.
    ///     Initialized with the services the way <see cref="EnemyImpact" /> is initialized with its context.
    ///     The <c>_killResolved</c> guard (added in A6) is PRESERVED exactly: an enemy that dies by damage
    ///     AND rings out in the same physics step is awarded only once. <see cref="AwardKill" /> returns the
    ///     amount so callers don't recompute it and hands back the combo multiplier via <c>out</c> (used for
    ///     the ringout SFX pitch). <see cref="ResetForSpawn" /> clears the guard on pool reuse.
    ///     The ringout particle burst is kept INLINE here for now (a later batch swaps it for a shared
    ///     BurstFactory).
    /// </summary>
    public sealed class RingoutHandler
    {
        // One shared burst material for every ringout across every enemy — building it per instance (once per
        // pooled enemy per match) leaked a runtime Material each time; a single static one never does.
        private static Material _sharedBurstMaterial;

        private IAudioService _audioService;
        private IComboService _comboService;
        private EnemyData _data;
        private bool _killResolved;

        private ParticleSystem _ringoutBurst;
        private IScorePopupService _scorePopups;
        private IScoreService _scoreService;
        private Transform _transform;
        private Func<EnemyTypeData> _type;
        private VfxData _vfx;

        public void Initialize(Transform transform, EnemyData data, VfxData vfx,
            IScoreService scoreService, IComboService comboService, IScorePopupService scorePopups,
            IAudioService audioService, Func<EnemyTypeData> typeProvider)
        {
            _transform = transform;
            _data = data;
            _vfx = vfx;
            _scoreService = scoreService;
            _comboService = comboService;
            _scorePopups = scorePopups;
            _audioService = audioService;
            _type = typeProvider;
        }

        // Registers the kill with the combo chain and awards score × multiplier ONCE (guarded so an enemy that
        // dies by damage AND rings out in the same physics step can't double-score). Returns the awarded amount
        // so callers don't recompute it; the current multiplier comes back via out (for the ringout SFX pitch).
        public int AwardKill(out int multiplier)
        {
            if (_killResolved)
            {
                multiplier = 1;
                return 0;
            }

            _killResolved = true;
            multiplier = _comboService != null ? _comboService.RegisterKill() : 1;
            int awarded = GetScoreReward() * multiplier;
            _scoreService.Add(awarded);
            return awarded;
        }

        /// <summary>Clears the once-only kill guard for a fresh spawn / pool reuse.</summary>
        public void ResetForSpawn()
        {
            _killResolved = false;
        }

        /// <summary>
        ///     The scoring + presentation half of the old EnterRingoutState: award the kill (once), play the
        ///     ringout sting pitched by the combo multiplier, and — if this award actually granted points —
        ///     spawn the popup text and particle burst. The controller keeps the physics/state half.
        /// </summary>
        public void ResolveRingout()
        {
            int awarded = AwardKill(out int multiplier);
            _audioService?.PlaySfx(GameSfx.Ringout, 1f + (multiplier - 1) * 0.06f);

            if (awarded > 0)
                SpawnRingoutFeedback(awarded);
        }

        private void SpawnRingoutFeedback(int awarded)
        {
            Vector3 feedbackPosition = new(_transform.position.x, _data.RingoutTextHeight, _transform.position.z);
            _scorePopups.Show(feedbackPosition, $"+{awarded}");
            PlayRingoutBurst(feedbackPosition);
        }

        private void PlayRingoutBurst(Vector3 position)
        {
            EnsureRingoutBurst();
            _ringoutBurst.transform.position = position;
            _ringoutBurst.Emit(_vfx.RingoutBurstCount);
        }

        /// <summary>Mechanical pause: freeze a mid-flight ringout burst in place (particles ignore a script gate).</summary>
        public void PauseEffect()
        {
            if (_ringoutBurst != null)
                _ringoutBurst.Pause(true);
        }

        public void ResumeEffect()
        {
            if (_ringoutBurst != null)
                _ringoutBurst.Play(true);
        }

        private void EnsureRingoutBurst()
        {
            if (_ringoutBurst != null)
                return;

            GameObject burstObject = new("Ringout Burst");
            burstObject.transform.SetParent(_transform.parent, false);
            _ringoutBurst = burstObject.AddComponent<ParticleSystem>();
            VfxData vfx = _vfx;

            ParticleSystem.MainModule main = _ringoutBurst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(vfx.RingoutBurstLifetimeMin, vfx.RingoutBurstLifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(vfx.RingoutBurstSpeedMin, vfx.RingoutBurstSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(vfx.RingoutBurstSizeMin, vfx.RingoutBurstSizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(vfx.RingoutColorA, vfx.RingoutColorB);
            main.gravityModifier = vfx.RingoutBurstGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = _ringoutBurst.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _ringoutBurst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            ParticleSystemRenderer particleRenderer = burstObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = GetBurstMaterial();
        }

        private static Material GetBurstMaterial()
        {
            if (_sharedBurstMaterial != null)
                return _sharedBurstMaterial;

            _sharedBurstMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Ringout Burst" };
            return _sharedBurstMaterial;
        }

        private int GetScoreReward()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.ScoreReward * _type().ScoreMultiplier));
        }
    }
}