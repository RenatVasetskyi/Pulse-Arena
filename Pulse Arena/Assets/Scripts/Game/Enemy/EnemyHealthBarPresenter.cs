using Game.Enemy.Interfaces;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    ///     Owns the enemy's floating world-space HP bar. Extracted from the controller's old CreateHealthBar
    ///     + the SetHealth forwarding: it instantiates the bar prefab under the enemy, initializes it, and
    ///     forwards health changes. The controller wires <c>ActorHealth.Changed</c> into <see cref="SetHealth" />
    ///     and the ringout drives it to 0; the presenter itself knows nothing about health, scoring or state.
    /// </summary>
    public sealed class EnemyHealthBarPresenter
    {
        private IWorldHealthBar _healthBar;

        /// <summary>
        ///     Instantiates the world health-bar prefab under <paramref name="parent" />, initializes it to
        ///     <paramref name="maxHealth" /> / <paramref name="barHeight" />, and shows the full bar. No-op if
        ///     already created or the prefab is missing (byte-identical to the old CreateHealthBar guards).
        /// </summary>
        public void Create(Transform parent, GameObject prefab, int maxHealth, float barHeight)
        {
            if (_healthBar != null)
                return;

            if (prefab == null)
                return;

            _healthBar = Object.Instantiate(prefab, parent, false).GetComponent<IWorldHealthBar>();
            _healthBar.Initialize(maxHealth, barHeight);
            _healthBar.SetHealth(maxHealth, maxHealth);
        }

        /// <summary>Forwards a health change to the bar (no-op if the bar was never created).</summary>
        public void SetHealth(int current, int max)
        {
            _healthBar?.SetHealth(current, max);
        }
    }
}