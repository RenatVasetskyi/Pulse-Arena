using UnityEngine;

namespace Game.Common
{
    /// <summary>
    ///     Shared physics helpers for the two actors (player + enemy).
    /// </summary>
    public static class ActorPhysicsUtility
    {
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
    }
}
