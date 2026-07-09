using Game.Player;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hud
{
    /// <summary>
    ///     HUD health: swaps each heart between the alive/empty sprite based on the player's HP.
    ///     Assign the pre-placed heart Images + the two sprites in the prefab.
    /// </summary>
    public class HudHealthView : MonoBehaviour
    {
        [SerializeField] private Image[] _hearts;
        [SerializeField] private Sprite _aliveSprite;
        [SerializeField] private Sprite _emptySprite;
        private int _lastHealth = -1;

        private PlayerController _player;

        private void OnDestroy()
        {
            if (_player != null)
                _player.HealthChanged -= OnHealthChanged;
        }

        public void Bind(PlayerController player)
        {
            _player = player;
            _player.HealthChanged += OnHealthChanged;
            OnHealthChanged(player.Health, player.MaxHealth);
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

            AnimateChange(health);
            _lastHealth = health;
        }

        private void AnimateChange(int health)
        {
            if (_lastHealth < 0 || _lastHealth == health)
                return;

            if (health < _lastHealth)
            {
                for (int i = health; i < _lastHealth && i < _hearts.Length; i++)
                    UiTween.Shake(_hearts[i] != null ? _hearts[i].transform : null);
            }
            else
            {
                for (int i = _lastHealth; i < health && i < _hearts.Length; i++)
                    UiTween.Pop(_hearts[i] != null ? _hearts[i].transform : null);
            }
        }
    }
}