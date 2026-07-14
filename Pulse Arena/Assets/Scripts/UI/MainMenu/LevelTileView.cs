using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    ///     One level-select tile (a passive View): shows the level name plus either a padlock (locked) or its earned
    ///     stars, and raises <see cref="Clicked" /> when an unlocked tile is tapped. <see cref="LevelSelectView" />
    ///     binds it via <see cref="Bind" />. The locked/disabled look is driven by the <see cref="_button" />'s own
    ///     colour transition (authored on the prefab), so this never recolours the tile — it only toggles the lock and
    ///     stars.
    /// </summary>
    public class LevelTileView : MonoBehaviour
    {
        private static readonly Color StarEarned = new(1f, 0.94f, 0.5f, 1f);
        private static readonly Color StarEmpty = new(0.9f, 0.9f, 0.95f, 0.3f);

        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private Image _lock;
        [SerializeField] private GameObject _starsHolder;
        [SerializeField] private Image[] _stars;

        public event Action Clicked;

        public void Bind(string levelName, bool unlocked, int stars, bool survival)
        {
            _name.text = levelName;
            _button.interactable = unlocked;

            _lock.gameObject.SetActive(!unlocked);

            bool showStars = unlocked && !survival;
            _starsHolder.SetActive(showStars);

            for (int i = 0; i < _stars.Length; i++)
                _stars[i].color = i < stars ? StarEarned : StarEmpty;

            _button.onClick.RemoveAllListeners();

            if (unlocked)
                _button.onClick.AddListener(() => Clicked?.Invoke());
        }
    }
}
