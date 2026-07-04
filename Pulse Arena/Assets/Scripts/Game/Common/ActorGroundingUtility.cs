using UnityEngine;

namespace Game.Common
{
    public static class ActorGroundingUtility
    {
        private const float GroundClearance = 0.02f;
        private const float DefaultProbeDistance = 8f;
        private const float GroundNormalThreshold = 0.55f;

        public static bool SnapToGround(Transform actor, float probeDistance = DefaultProbeDistance)
        {
            if (actor == null)
                return false;

            float bottomOffset = GetBottomOffset(actor);
            Vector3 origin = actor.position + Vector3.up * (bottomOffset + probeDistance * 0.5f);
            float distance = probeDistance + bottomOffset;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);

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
            position.y = bestHit.point.y + bottomOffset + GroundClearance;
            actor.position = position;

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

        private static bool IsValidGroundHit(Transform actor, RaycastHit hit)
        {
            if (hit.collider == null || hit.normal.y <= GroundNormalThreshold)
                return false;

            return !hit.collider.transform.IsChildOf(actor);
        }
    }
}
