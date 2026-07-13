using Data;
using Game.Visuals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Enemy
{
    /// <summary>
    ///     Decides WHEN the mid-run flourish (the karate somersault) should fire. Owned and ticked by
    ///     <see cref="States.EnemyChaseState" /> so it only ever runs while the enemy is actually chasing — never
    ///     while attacking, knocked, grabbed or dead. <see cref="ShouldFlip" /> returns true at a random interval,
    ///     but only while the target is farther than <see cref="EnemyData.FlourishMinPlayerDistance" /> so the enemy
    ///     never shows off inside striking range; the chase then hands off to <see cref="States.EnemyFlipState" />
    ///     (which plays the clip — this class only decides, it never touches the visual). Model-agnostic: a visual
    ///     without the flourish clip reports <see cref="IEnemyVisual.SupportsRunFlourish" /> false and is skipped, so
    ///     this is inert for the skeleton/wolf and only the karate reacts. The interval is re-rolled on every chase
    ///     entry, so a fresh approach always waits a full interval before the first flip.
    /// </summary>
    public class EnemyFlourish
    {
        private readonly EnemyContext _context;
        private float _cooldown;

        public EnemyFlourish(EnemyContext context)
        {
            _context = context;
        }

        /// <summary>Re-roll the wait before the next flourish — called on each chase entry so a fresh chase never flips instantly.</summary>
        public void Rearm()
        {
            _cooldown = Random.Range(_context.Data.FlourishMinInterval, _context.Data.FlourishMaxInterval);
        }

        /// <summary>True when it is time to flourish: the interval elapsed AND the target is far enough to show off.</summary>
        public bool ShouldFlip(float deltaTime)
        {
            if (_context.Visual == null || !_context.Visual.SupportsRunFlourish)
                return false;

            _cooldown -= deltaTime;

            if (_cooldown > 0f)
                return false;

            Rearm(); // re-arm regardless: a too-close roll waits out another full interval rather than retrying every frame

            return !IsTargetWithin(_context.Data.FlourishMinPlayerDistance);
        }

        private bool IsTargetWithin(float distance)
        {
            Transform target = _context.Target;

            if (target == null)
                return false;

            Vector3 offset = target.position - _context.Transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= distance * distance;
        }
    }
}
