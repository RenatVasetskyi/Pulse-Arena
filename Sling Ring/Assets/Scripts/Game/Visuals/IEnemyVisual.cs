using System;
using Data;
using UnityEngine;

namespace Game.Visuals
{
    /// <summary>
    ///     The enemy's swappable visual seam. <c>EnemyController</c> + its states drive the creature through this
    ///     interface so the body is a skinned/animated model (<see cref="SkeletonEnemyVisual" />) without the
    ///     controller knowing which specific model. Pooled actors call <see cref="ResetState" /> on reuse; the lasso
    ///     reads the grab volume via <see cref="TryGetRopeBounds" />.
    /// </summary>
    public interface IEnemyVisual
    {
        /// <summary>
        ///     Raised when this model's death presentation finishes (death clip end via animation event, or next
        ///     frame for a model with no death clip), so <c>EnemyController</c> pools the corpse exactly then — no
        ///     guessed timer. Controller subscribes on death, unsubscribes on pool-return.
        /// </summary>
        event Action DeathCompleted;

        /// <summary>
        ///     Seconds from <see cref="PlayAttack" /> to the frame the strike visually connects, so the chase
        ///     state lands damage in sync with THIS model's swing (a long skinned melee clip connects ~1s in).
        ///     Negative = "no opinion, use the gameplay default" (EnemyData.AttackHitDelay).
        /// </summary>
        float AttackHitDelay { get; }

        /// <summary>
        ///     How long the attack STATE should hold for this model's swing to play through (clip length ÷ play
        ///     speed), so a long cinematic attack (the karate spin-flip kick) isn't cut off. <c>EnemyAttackState</c>
        ///     takes the max of this and the default hit-delay+recovery window. 0 = the default window is enough.
        /// </summary>
        float AttackClipDuration { get; }

        /// <summary>
        ///     Whether THIS model has a run-flourish clip (e.g. the karate somersault). <c>EnemyChaseState</c> skips
        ///     the flourish roll for models that report false, so a model without the clip is never asked to play one.
        /// </summary>
        bool SupportsRunFlourish { get; }

        /// <summary>
        ///     Whether THIS model has a spawn-taunt clip. When true the enemy spawns into <c>EnemyTauntState</c>
        ///     (stands its ground + taunts) instead of straight into the chase; false models skip the taunt entirely.
        /// </summary>
        bool HasSpawnTaunt { get; }

        /// <summary>Seconds <c>EnemyTauntState</c> holds the enemy still while the spawn taunt plays (0 for models without one).</summary>
        float SpawnTauntDuration { get; }

        /// <summary>Seconds <c>EnemyFlipState</c> runs the somersault flourish before returning to the chase (0 for models without one).</summary>
        float FlourishDuration { get; }

        void Initialize(Rigidbody rigidbody);

        void SetPaused(bool value);

        void ApplyTypeStyle(EnemyTypeData type);

        void PlayDeath();

        void PlayGroundBounce();

        void PlayHit();

        void PlayAttack();

        // One-shot karate somersault (EnemyFlipState); no-op for models without the clip (gated by SupportsRunFlourish).
        void PlayRunFlourish();

        // One-shot spawn taunt (EnemyTauntState); no-op for models without the clip (gated by HasSpawnTaunt).
        void PlaySpawnTaunt();

        // Force the model out of its attack clip back to locomotion when the attack state exits, so the Animator
        // never lingers mid-swing after the controller resumes chasing. No-op for models with no attack clip.
        void EndAttack();

        void ResetState();

        void SetGrabbed(bool isGrabbed);

        void SetThrown(bool isThrown);

        bool TryGetRopeBounds(out Bounds bounds);
    }
}
