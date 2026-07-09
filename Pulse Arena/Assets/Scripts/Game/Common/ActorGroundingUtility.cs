using Data;
using UnityEngine;

namespace Game.Common
{
    /// <summary>
    ///     Pure static grounding helpers — snapping actors onto physical ground and probing for it. The tuning
    ///     (<see cref="GroundingData" />) is passed in per call, so there is NO hidden global state and every
    ///     method is a pure function of its inputs (order-independent + testable).
    /// </summary>
    public static class ActorGroundingUtility
    {
        private const string GroundLayerName = "Ground";

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

        public static bool SnapToGround(Transform actor, GroundingData grounding, float probeDistance = -1f,
            float groundClearance = -1f)
        {
            if (!TryGetGroundedPosition(actor, grounding, probeDistance, groundClearance, out Vector3 groundedPosition))
                return false;

            actor.position = groundedPosition;
            return true;
        }

        public static bool TryGetGroundedPosition(Transform actor, GroundingData grounding, float probeDistance,
            float groundClearance, out Vector3 groundedPosition)
        {
            groundedPosition = default;
            ResolveProbeParameters(grounding, ref probeDistance, ref groundClearance);

            if (actor == null)
                return false;

            RaycastHit[] hits = CastGroundProbe(actor, probeDistance, groundClearance, out float bottomOffset);

            if (hits.Length == 0)
                return false;

            if (!TryFindNearestGroundHit(actor, grounding, hits, out RaycastHit bestHit))
                return false;

            groundedPosition = BuildGroundedPosition(actor, bestHit, bottomOffset, groundClearance);
            return true;
        }

        private static Vector3 BuildGroundedPosition(Transform actor, RaycastHit hit, float bottomOffset,
            float groundClearance)
        {
            Vector3 position = actor.position;
            position.y = hit.point.y + bottomOffset + groundClearance;
            return position;
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

        private static int GetGroundMask()
        {
            int groundMask = LayerMask.GetMask(GroundLayerName);
            return groundMask == 0 ? ~0 : groundMask;
        }

        private static bool IsValidGroundHit(Transform actor, GroundingData grounding, RaycastHit hit)
        {
            if (hit.collider == null || hit.normal.y <= grounding.GroundNormalThreshold)
                return false;

            return IsGroundCollider(hit.collider) && !hit.collider.transform.IsChildOf(actor);
        }

        private static void ResolveProbeParameters(GroundingData grounding, ref float probeDistance,
            ref float groundClearance)
        {
            if (probeDistance < 0f)
                probeDistance = grounding.DefaultProbeDistance;

            if (groundClearance < 0f)
                groundClearance = grounding.GroundClearance;
        }

        private static bool TryFindNearestGroundHit(Transform actor, GroundingData grounding, RaycastHit[] hits,
            out RaycastHit bestHit)
        {
            bestHit = default;
            float bestDistance = float.MaxValue;
            bool foundGround = false;

            foreach (RaycastHit hit in hits)
            {
                if (!IsValidGroundHit(actor, grounding, hit))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestHit = hit;
                foundGround = true;
            }

            return foundGround;
        }
    }
}
