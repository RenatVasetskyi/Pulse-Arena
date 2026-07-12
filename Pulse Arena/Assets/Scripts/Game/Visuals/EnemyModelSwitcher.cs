using System;
using Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Visuals
{
    /// <summary>
    ///     Enemy-model variety: holds several model visuals (each a child <see cref="IEnemyVisual" />), shows a random
    ///     one per spawn (re-rolled in <see cref="ResetState" /> so pooled reuse varies it) and forwards every
    ///     <see cref="IEnemyVisual" /> call to the active model. So <c>EnemyController</c> still sees a single visual
    ///     while the player faces a mix of skeletons, mushrooms, etc. Lives on its own always-active root; only the
    ///     model children toggle.
    /// </summary>
    public class EnemyModelSwitcher : MonoBehaviour, IEnemyVisual
    {
        [SerializeField] private MonoBehaviour[] _modelBehaviours;
        private IEnemyVisual[] _models;
        private IEnemyVisual _active;

        public event Action DeathCompleted;

        public float AttackHitDelay => _active?.AttackHitDelay ?? -1f;

        public void Initialize(Rigidbody rigidbody, EnemyVisualData visualData)
        {
            ResolveModels();

            for (int i = 0; i < _models.Length; i++)
                _models[i]?.Initialize(rigidbody, visualData);

            PickRandom();
        }

        private void OnDestroy()
        {
            if (_models == null)
                return;

            for (int i = 0; i < _models.Length; i++)
                if (_models[i] != null)
                    _models[i].DeathCompleted -= RaiseDeathCompleted;
        }

        public void SetPaused(bool value)
        {
            _active?.SetPaused(value);
        }

        public void ApplyTypeStyle(EnemyTypeData type)
        {
            _active?.ApplyTypeStyle(type);
        }

        public void PlayDeath()
        {
            _active?.PlayDeath();
        }

        public void PlayGroundBounce()
        {
            _active?.PlayGroundBounce();
        }

        public void PlayHit()
        {
            _active?.PlayHit();
        }

        public void PlayAttack()
        {
            _active?.PlayAttack();
        }

        public void EndAttack()
        {
            _active?.EndAttack();
        }

        public void ResetState()
        {
            PickRandom(); // pooled reuse re-rolls which model shows
            _active?.ResetState();
        }

        public void SetGrabbed(bool isGrabbed)
        {
            _active?.SetGrabbed(isGrabbed);
        }

        public void SetThrown(bool isThrown)
        {
            _active?.SetThrown(isThrown);
        }

        public bool TryGetRopeBounds(out Bounds bounds)
        {
            if (_active != null)
                return _active.TryGetRopeBounds(out bounds);

            bounds = default;
            return false;
        }

        private void ResolveModels()
        {
            if (_models != null)
                return;

            _models = new IEnemyVisual[_modelBehaviours.Length];

            for (int i = 0; i < _modelBehaviours.Length; i++)
                _models[i] = _modelBehaviours[i] as IEnemyVisual;

            // Re-raise whichever model finishes its death (only the active one plays death, so only it fires).
            for (int i = 0; i < _models.Length; i++)
                if (_models[i] != null)
                    _models[i].DeathCompleted += RaiseDeathCompleted;
        }

        private void RaiseDeathCompleted()
        {
            DeathCompleted?.Invoke();
        }

        // Show exactly one model (random) and hide the rest; the shown one becomes the active delegate.
        private void PickRandom()
        {
            if (_models == null || _models.Length == 0)
                return;

            int index = Random.Range(0, _models.Length);
            _active = _models[index];

            for (int i = 0; i < _modelBehaviours.Length; i++)
                if (_modelBehaviours[i] != null)
                    _modelBehaviours[i].gameObject.SetActive(i == index);
        }
    }
}
