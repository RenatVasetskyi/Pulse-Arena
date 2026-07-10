using Data;
using UnityEngine;

namespace Game.Spawning
{
    /// <summary>
    ///     Ring-sampling safe-spawn geometry shared by the enemy and pickup spawners. A candidate passes only when
    ///     it sits far enough (horizontally) from the player AND its clearance sphere is empty of blockers. The
    ///     blocker mask combines the obstacle layer (walls/boxes, plus the Default-layer pit and pickup triggers)
    ///     with the enemy layer, so a single overlap test keeps a new spawn out of walls, off suck-hole pits, and
    ///     off other spawns. Trigger colliders are included on purpose (pits and pickups are triggers).
    /// </summary>
    public class SafeSpawnFinder : ISafeSpawnFinder
    {
        private SpawnAreaData _area;
        private LayerMask _blockerMask;
        private Vector3 _center;
        private Transform _player;

        public void Initialize(Vector3 center, Transform player, SpawnAreaData area, LayerMask blockerMask)
        {
            _center = center;
            _player = player;
            _area = area;
            _blockerMask = blockerMask;
        }

        public bool TryFind(out Vector3 position)
        {
            int tries = Mathf.Max(1, _area.MaxTries);

            for (int i = 0; i < tries; i++)
            {
                Vector3 candidate = SampleRing();

                if (IsTooCloseToPlayer(candidate))
                    continue;

                if (IsBlocked(candidate))
                    continue;

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private Vector3 SampleRing()
        {
            float angle = Random.value * Mathf.PI * 2f;
            float radius = Random.Range(_area.MinRadius, _area.MaxRadius);
            return _center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private bool IsTooCloseToPlayer(Vector3 candidate)
        {
            if (_player == null)
                return false;

            return HorizontalDistance(candidate, _player.position) < _area.PlayerClearance;
        }

        private bool IsBlocked(Vector3 candidate)
        {
            Vector3 probe = candidate + Vector3.up * _area.ProbeHeight;
            return Physics.CheckSphere(probe, _area.SpawnClearance, _blockerMask, QueryTriggerInteraction.Collide);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
