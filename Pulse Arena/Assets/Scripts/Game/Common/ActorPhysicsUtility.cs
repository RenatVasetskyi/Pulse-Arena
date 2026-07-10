using UnityEngine;

namespace Game.Common
{
    /// <summary>
    ///     Shared physics/visual helpers for the two actors (player + enemy), extracted from byte-identical
    ///     snippets that used to be duplicated in both controllers and both primitive visuals.
    /// </summary>
    public static class ActorPhysicsUtility
    {
        private const float MaxSaneSqrDistance = 100_000_000f; // ~10 000 units from origin
        private const float MaxSaneSqrScale = 10_000f;         // ~100x normal scale

        /// <summary>
        ///     Shifts a child visual up/down so its rendered bottom sits on the parent capsule's feet (plus a
        ///     small ground clearance). Shared by PlayerPrimitiveVisual and EnemyPrimitiveVisual.
        /// </summary>
        public static void AlignVisualBottomToCollider(Transform visual, Renderer[] cachedRenderers,
            float groundClearance = 0.02f)
        {
            if (visual.parent == null)
                return;

            // A blown-up transform (physics NaN) or one flung absurdly far from the origin makes
            // Renderer.bounds log "Invalid worldAABB" every frame; skip alignment until it's cleaned up.
            if (!HasSaneTransform(visual))
                return;

            CapsuleCollider capsule = visual.parent.GetComponent<CapsuleCollider>();

            if (capsule == null)
                return;

            Renderer[] renderers = cachedRenderers ?? visual.GetComponentsInChildren<Renderer>();

            if (!TryGetRenderersBottom(renderers, out float rendererBottom))
                return;

            float actorBottom = GetCapsuleBottom(capsule);
            float worldDelta = actorBottom + groundClearance - rendererBottom;
            float parentScaleY = Mathf.Max(0.001f, Mathf.Abs(visual.parent.lossyScale.y));

            visual.localPosition += Vector3.up * (worldDelta / parentScaleY);
        }

        /// <summary>Adds a downward acceleration so airborne actors fall faster than default gravity.</summary>
        public static void ApplyExtraGravity(Rigidbody rigidbody, float extraGravity)
        {
            if (rigidbody != null)
                rigidbody.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }

        /// <summary>
        ///     Moves a capsule collider's centre so the capsule sits on the transform's feet (centre.y = height/2).
        ///     Returns the collider so callers that cache it (the enemy uses it for rope bounds) can keep the reference.
        /// </summary>
        public static CapsuleCollider NormalizeCapsuleRoot(Transform root)
        {
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();

            if (capsule == null)
                return null;

            Vector3 center = capsule.center;
            center.y = capsule.height * 0.5f;
            capsule.center = center;

            return capsule;
        }

        /// <summary>True if every component of the vector is a real, finite number (no NaN / Infinity).</summary>
        public static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        private static bool HasSaneTransform(Transform visual)
        {
            Vector3 position = visual.position;
            Vector3 scale = visual.lossyScale;

            return IsFinite(position) && IsFinite(scale) &&
                   position.sqrMagnitude < MaxSaneSqrDistance && scale.sqrMagnitude < MaxSaneSqrScale;
        }

        private static float GetCapsuleBottom(CapsuleCollider capsule)
        {
            Transform capsuleTransform = capsule.transform;
            float scaledHeight = capsule.height * Mathf.Abs(capsuleTransform.lossyScale.y);
            float scaledCenterY = capsule.center.y * capsuleTransform.lossyScale.y;

            return capsuleTransform.position.y + scaledCenterY - scaledHeight * 0.5f;
        }

        private static bool TryGetRenderersBottom(Renderer[] renderers, out float bottom)
        {
            bottom = float.MaxValue;
            bool hasRenderer = false;

            foreach (Renderer visualRenderer in renderers)
            {
                if (visualRenderer == null)
                    continue;

                bottom = Mathf.Min(bottom, visualRenderer.bounds.min.y);
                hasRenderer = true;
            }

            return hasRenderer;
        }
    }
}