using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    /// HUD health: swaps each heart between the alive/empty sprite based on the player's HP.
    /// Assign the pre-placed heart Images + the two sprites in the prefab.
    /// </summary>
    public class HudHealthView : MonoBehaviour
    {
        [SerializeField] private Image[] _hearts;
        [SerializeField] private Sprite _aliveSprite;
        [SerializeField] private Sprite _emptySprite;

        private PlayerController _player;

        public void Bind(PlayerController player)
        {
            _player = player;
            _player.HealthChanged += OnHealthChanged;
            OnHealthChanged(player.Health, player.MaxHealth);
        }

        private void OnDestroy()
        {
            if (_player != null)
                _player.HealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(int health, int maxHealth)
        {
            if (_hearts == null)
                return;

            for (int i = 0; i < _hearts.Length; i++)
            {
                if (_hearts[i] == null)
                    continue;

                bool exists = i < maxHealth;
                _hearts[i].enabled = exists;

                if (exists && _aliveSprite != null && _emptySprite != null)
                    _hearts[i].sprite = i < health ? _aliveSprite : _emptySprite;
            }
        }
    }
}
