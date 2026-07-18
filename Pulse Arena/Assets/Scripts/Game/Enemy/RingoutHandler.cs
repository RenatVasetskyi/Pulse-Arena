using System;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     Everything that happens when an enemy is scored: the guarded kill award (combo + score, once), and —
    ///     for a ringout specifically — the popup text, the audio sting and the particle burst. The
    ///     <c>_killResolved</c> guard awards only once for an enemy that dies by damage AND rings out in the same
    ///     physics step; <see cref="AwardKill" /> returns the amount and hands back the combo multiplier via
    ///     <c>out</c> (used for the ringout SFX pitch). The ringout burst is an authored prefab: its look and its
    ///     t=0 emission burst live on the asset, so this only triggers it.
    /// </summary>
    public sealed class RingoutHandler
    {
        private IAudioService _audioService;
        private GameObject _burstPrefab;
        private IComboService _comboService;
        private EnemyData _data;
        private bool _killResolved;

        private ParticleSystem _ringoutBurst;
        private float _ringoutPitchPerMultiplier;
        private IScorePopupService _scorePopups;
        private IScoreService _scoreService;
        private Transform _transform;
        private Func<EnemyTypeData> _type;

        public void Initialize(Transform transform, EnemyData data, GameObject burstPrefab,
            IScoreService scoreService, IComboService comboService, IScorePopupService scorePopups,
            IAudioService audioService, Func<EnemyTypeData> typeProvider, float ringoutPitchPerMultiplier)
        {
            _transform = transform;
            _data = data;
            _burstPrefab = burstPrefab;
            _scoreService = scoreService;
            _comboService = comboService;
            _scorePopups = scorePopups;
            _audioService = audioService;
            _type = typeProvider;
            _ringoutPitchPerMultiplier = ringoutPitchPerMultiplier;
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
        ///     The scoring + presentation half of ring-out: award the kill (once), play the sting pitched by the
        ///     combo multiplier, and — if the award granted points — spawn the popup text and particle burst. The
        ///     controller keeps the physics/state half.
        /// </summary>
        public void ResolveRingout()
        {
            int awarded = AwardKill(out int multiplier);
            _audioService?.PlaySfx(GameSfx.Ringout, 1f + (multiplier - 1) * _ringoutPitchPerMultiplier);

            if (awarded > 0)
                SpawnRingoutFeedback(awarded);
        }

        private void SpawnRingoutFeedback(int awarded)
        {
            Vector3 feedbackPosition = new(_transform.position.x, _data.RingoutTextHeight, _transform.position.z);
            _scorePopups.Show(feedbackPosition, $"+{awarded}");
            PlayRingoutBurst(feedbackPosition);
        }

        // Restart, don't Play(): Play() rewinds and re-fires the authored t=0 burst only when the system is playing
        // or stopped — on a PAUSED one it silently takes the resume branch (no burst, and it un-pauses the effect
        // behind IPauseService's back). Stop-then-Play is unambiguous from every state.
        private void PlayRingoutBurst(Vector3 position)
        {
            EnsureRingoutBurst();

            if (_ringoutBurst == null)
                return;

            _ringoutBurst.transform.position = position;
            _ringoutBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _ringoutBurst.Play(true);
        }

        /// <summary>Mechanical pause: freeze a mid-flight ringout burst in place (particles ignore a script gate).</summary>
        public void PauseEffect()
        {
            if (_ringoutBurst != null)
                _ringoutBurst.Pause(true);
        }

        // Guarded on isPaused: Play(true) on an already-playing system rewinds to t=0 and re-fires the burst, so a
        // resume without a matching pause (or a stale _paused flag on a pooled enemy) would visibly re-bang it.
        public void ResumeEffect()
        {
            if (_ringoutBurst != null && _ringoutBurst.isPaused)
                _ringoutBurst.Play(true);
        }

        // Spawns the authored burst prefab once per enemy and reuses it; the particle look + material are baked on
        // the asset, so nothing is configured here.
        private void EnsureRingoutBurst()
        {
            if (_ringoutBurst != null || _burstPrefab == null)
                return;

            GameObject burstObject = UnityEngine.Object.Instantiate(_burstPrefab, _transform.parent);
            _ringoutBurst = burstObject.GetComponent<ParticleSystem>();
        }

        private int GetScoreReward()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_data.ScoreReward * _type().ScoreMultiplier));
        }
    }
}