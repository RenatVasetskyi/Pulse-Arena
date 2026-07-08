using Data;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    /// The pit placement geometry: a few tries to land a spot inside the play ring that is clear of the player
    /// AND every enemy (an <see cref="Physics.CheckSphere"/> on the enemy layer). Kept separate from the spawn
    /// cadence so the placement rules are one focused, independently-tunable unit.
    /// </summary>
    public class PitPlacementFinder : IPitPlacementFinder
    {
        private const int MaxPlacementTries = 8;

        // The pit prefab's base trigger radius (Assets/Prefabs/Game/pit.prefab CapsuleCollider). Scaled by the
        // spawn scale to keep a fresh pit clear of the player and any enemy.
        private const float PitTriggerRadius = 2.5f;

        private Vector3 _center;
        private Transform _player;
        private PitData _data;
        private LayerMask _enemyLayer;

        public void Initialize(Vector3 center, Transform player, PitData data, LayerMask enemyLayer)
        {
            _center = center;
            _player = player;
            _data = data;
            _enemyLayer = enemyLayer;
        }

        public bool TryFind(float scale, out Vector3 position)
        {
            float clearance = PitTriggerRadius * scale + _data.SpawnClearance;
            float playerClearance = Mathf.Max(_data.MinPlayerDistance, clearance);

            for (int i = 0; i < MaxPlacementTries; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(_data.MinRadius, _data.MaxRadius);
                Vector3 candidate = _center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (_player != null && HorizontalDistance(candidate, _player.position) < playerClearance)
                    continue;

                if (Physics.CheckSphere(candidate + Vector3.up, clearance, _enemyLayer))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
