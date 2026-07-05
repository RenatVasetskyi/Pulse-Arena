using Data;
using UnityEngine;

namespace Game.Common
{
    public static class ActorGroundingUtility
    {
        private const string GroundLayerName = "Ground";

        private static float _groundClearance = 0.02f;
        private static float _defaultProbeDistance = 8f;
        private static float _groundNormalThreshold = 0.55f;

        public static void Configure(GroundingData data)
        {
            if (data == null)
                return;

            _groundClearance = data.GroundClearance;
            _defaultProbeDistance = data.DefaultProbeDistance;
            _groundNormalThreshold = data.GroundNormalThreshold;
        }

        public static bool SnapToGround(Transform actor, float probeDistance = -1f,
            float groundClearance = -1f)
        {
            if (!TryGetGroundedPosition(actor, probeDistance, groundClearance, out Vector3 groundedPosition))
                return false;

            actor.position = groundedPosition;
            return true;
        }

        public static bool TryGetGroundedPosition(Transform actor, float probeDistance,
            float groundClearance, out Vector3 groundedPosition)
        {
            groundedPosition = default;

            if (probeDistance < 0f)
                probeDistance = _defaultProbeDistance;

            if (groundClearance < 0f)
                groundClearance = _groundClearance;

            if (actor == null)
                return false;

            float bottomOffset = GetBottomOffset(actor);
            Vector3 origin = actor.position + Vector3.up * (bottomOffset + probeDistance * 0.5f);
            float distance = probeDistance + bottomOffset + Mathf.Abs(groundClearance);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, GetGroundMask(),
                QueryTriggerInteraction.Ignore);

            if (hits.Length == 0)
                return false;

            RaycastHit bestHit = default;
            float bestDistance = float.MaxValue;
            bool foundGround = false;

            foreach (RaycastHit hit in hits)
            {
                if (!IsValidGroundHit(actor, hit))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestHit = hit;
                foundGround = true;
            }

            if (!foundGround)
                return false;

            Vector3 position = actor.position;
            position.y = bestHit.point.y + bottomOffset + groundClearance;
            groundedPosition = position;
            return true;
        }

        public static float GetBottomOffset(Transform actor)
        {
            if (actor == null)
                return 0f;

            CapsuleCollider capsule = actor.GetComponent<CapsuleCollider>();

            if (capsule == null)
                return 0f;

            float scaledHeight = capsule.height * Mathf.Abs(actor.lossyScale.y);
            float scaledCenterY = capsule.center.y * actor.lossyScale.y;

            return Mathf.Max(0f, scaledHeight * 0.5f - scaledCenterY);
        }

        public static bool IsGroundCollider(Collider collider)
        {
            if (collider == null)
                return false;

            int groundMask = LayerMask.GetMask(GroundLayerName);

            if (groundMask == 0)
                return true;

            return (groundMask & (1 << collider.gameObject.layer)) != 0;
        }

        private static bool IsValidGroundHit(Transform actor, RaycastHit hit)
        {
            if (hit.collider == null || hit.normal.y <= _groundNormalThreshold)
                return false;

            return IsGroundCollider(hit.collider) && !hit.collider.transform.IsChildOf(actor);
        }

        private static int GetGroundMask()
        {
            int groundMask = LayerMask.GetMask(GroundLayerName);
            return groundMask == 0 ? ~0 : groundMask;
        }
    }
}
