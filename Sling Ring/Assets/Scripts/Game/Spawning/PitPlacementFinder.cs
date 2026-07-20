using Data;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Pit placement geometry: a few tries to land a spot inside the play ring clear of the player and every
    ///     enemy (a <see cref="Physics.CheckSphere" /> on the enemy layer). Kept separate from the spawn cadence so
    ///     the placement rules are one focused, independently-tunable unit.
    /// </summary>
    public class PitPlacementFinder : IPitPlacementFinder
    {
        private const int MaxPlacementTries = 8;

        private Vector3 _center;
        private PitData _data;
        private LayerMask _enemyLayer;
        private Transform _player;

        public void Initialize(Vector3 center, Transform player, PitData data, LayerMask enemyLayer)
        {
            _center = center;
            _player = player;
            _data = data;
            _enemyLayer = enemyLayer;
        }

        public bool TryFind(float scale, out Vector3 position)
        {
            float clearance = _data.TriggerRadius * scale + _data.SpawnClearance;
            float playerClearance = Mathf.Max(_data.MinPlayerDistance, clearance);

            for (int i = 0; i < MaxPlacementTries; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(_data.MinRadius, _data.MaxRadius);
                Vector3 candidate = _center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (_player != null && SpawnGeometry.HorizontalDistance(candidate, _player.position) < playerClearance)
                    continue;

                if (Physics.CheckSphere(candidate + Vector3.up, clearance, _enemyLayer))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }
    }
}