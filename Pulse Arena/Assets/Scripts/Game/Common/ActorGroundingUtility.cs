using Data;
using UnityEngine;

namespace Game.Common
{
    public static class ActorGroundingUtility
    {
        private const string GroundLayerName = "Ground";
        private static float _defaultProbeDistance = 8f;

        private static float _groundClearance = 0.02f;
        private static float _groundNormalThreshold = 0.55f;

        public static void Configure(GroundingData data)
        {
            if (data == null)
                return;

            _groundClearance = data.GroundClearance;
            _defaultProbeDistance = data.DefaultProbeDistance;
            _groundNormalThreshold = data.GroundNormalThreshold;
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
            ResolveProbeParameters(ref probeDistance, ref groundClearance);

            if (actor == null)
                return false;

            RaycastHit[] hits = CastGroundProbe(actor, probeDistance, groundClearance, out float bottomOffset);

            if (hits.Length == 0)
                return false;

            if (!TryFindNearestGroundHit(actor, hits, out RaycastHit bestHit))
                return false;

            groundedPosition = BuildGroundedPosition(actor, bestHit, bottomOffset, groundClearance);
            return true;
        }

        private static void ResolveProbeParameters(ref float probeDistance, ref float groundClearance)
        {
            if (probeDistance < 0f)
                probeDistance = _defaultProbeDistance;

            if (groundClearance < 0f)
                groundClearance = _groundClearance;
        }

        private static RaycastHit[] CastGroundProbe(Transform actor, float probeDistance, float groundClearance,
            out float bottomOffset)
        {
            bottomOffset = GetBottomOffset(actor);
            Vector3 origin = actor.position + Vector3.up * (bottomOffset + probeDistance * 0.5f);
            float distance = probeDistance + bottomOffset + Mathf.Abs(groundClearance);
            return Physics.RaycastAll(origin, Vector3.down, distance, GetGroundMask(),
                QueryTriggerInteraction.Ignore);
        }

        private static bool TryFindNearestGroundHit(Transform actor, RaycastHit[] hits, out RaycastHit bestHit)
        {
            bestHit = default;
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

            return foundGround;
        }

        private static Vector3 BuildGroundedPosition(Transform actor, RaycastHit hit, float bottomOffset,
            float groundClearance)
        {
            Vector3 position = actor.position;
            position.y = hit.point.y + bottomOffset + groundClearance;
            return position;
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