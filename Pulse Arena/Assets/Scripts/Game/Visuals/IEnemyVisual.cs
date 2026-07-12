using System;
using Data;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     The enemy's swappable visual seam. <c>EnemyController</c> + its states drive the creature through this
    ///     interface so the body can be the procedural primitive blob (<see cref="EnemyPrimitiveVisual" />) or a
    ///     skinned/animated model (<see cref="SkeletonEnemyVisual" />) without the controller knowing which. Pooled
    ///     actors call <see cref="ResetState" /> on reuse; the lasso reads the grab volume via
    ///     <see cref="TryGetRopeBounds" />.
    /// </summary>
    public interface IEnemyVisual
    {
        /// <summary>
        ///     Raised when THIS model's death presentation has finished (the skinned death clip reaches its end
        ///     via an animation event, the primitive's procedural death pop completes, or a model with no death
        ///     clip signals next frame), so <c>EnemyController</c> pools the corpse exactly when the animation is
        ///     done — no guessed timer. The controller subscribes on death and unsubscribes on pool-return.
        /// </summary>
        event Action DeathCompleted;

        /// <summary>
        ///     Seconds from <see cref="PlayAttack" /> to the frame the strike visually connects, so the chase
        ///     state lands damage in sync with THIS model's swing (a long skinned melee clip connects ~1s in,
        ///     the primitive lunge ~0.15s). Negative = "no opinion, use the gameplay default" (EnemyData.AttackHitDelay).
        /// </summary>
        float AttackHitDelay { get; }

        void Initialize(Rigidbody rigidbody, EnemyVisualData visualData);

        void SetPaused(bool value);

        void ApplyTypeStyle(EnemyTypeData type);

        void PlayDeath();

        void PlayGroundBounce();

        void PlayHit();

        void PlayAttack();

        // Force the model out of its attack clip back into locomotion when the attack state exits, so the
        // Animator never lingers mid-swing while the controller has resumed chasing (the state owns its clip's
        // full lifecycle — start AND end). A no-op for models that have no distinct attack clip.
        void EndAttack();

        void ResetState();

        void SetGrabbed(bool isGrabbed);

        void SetThrown(bool isThrown);

        bool TryGetRopeBounds(out Bounds bounds);
    }
}
