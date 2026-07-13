using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    ///     One level-select tile (a passive View): shows the level name plus either a padlock (locked) or its earned
    ///     stars, and raises <see cref="Clicked" /> when an unlocked tile is tapped. <see cref="LevelSelectView" />
    ///     clones this off a disabled template in the prefab and calls <see cref="Bind" />. All the child refs are
    ///     inspector-wired, so the tile's look is tuned in the prefab, not in code.
    /// </summary>
    public class LevelTileView : MonoBehaviour
    {
        private static readonly Color LockedTint = new(0.62f, 0.66f, 0.74f, 1f);
        private static readonly Color SurvivalTint = new(1f, 0.66f, 0.55f, 1f);
        private static readonly Color StarEarned = new(1f, 0.94f, 0.5f, 1f);
        private static readonly Color StarEmpty = new(0.9f, 0.9f, 0.95f, 0.3f);

        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private Image _lock;
        [SerializeField] private TextMeshProUGUI _endless;
        [SerializeField] private Image[] _stars;

        public event Action Clicked;

        public void Bind(string levelName, bool unlocked, int stars, bool survival)
        {
            _name.text = levelName;
            _background.color = !unlocked ? LockedTint : survival ? SurvivalTint : Color.white;
            _button.interactable = unlocked;

            _lock.gameObject.SetActive(!unlocked);

            if (_endless != null)
                _endless.gameObject.SetActive(unlocked && survival);

            bool showStars = unlocked && !survival;

            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(showStars);
                _stars[i].color = i < stars ? StarEarned : StarEmpty;
            }

            _button.onClick.RemoveAllListeners();

            if (unlocked)
                _button.onClick.AddListener(() => Clicked?.Invoke());
        }
    }
}
