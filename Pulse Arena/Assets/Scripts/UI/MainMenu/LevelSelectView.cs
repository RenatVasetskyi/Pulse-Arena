using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    ///     The level-select overlay (a passive View): a jungle-temple background with a grid of ornate level tiles.
    ///     It clones <see cref="_tileTemplate" /> once per level into <see cref="_tileContainer" /> and binds each,
    ///     then raises <see cref="LevelChosen" /> / <see cref="BackPressed" />; all progress logic lives in the
    ///     presenter. Everything is authored on the <c>level_select</c> prefab — layout, sprites and colours are
    ///     tuned in the inspector, not in code.
    /// </summary>
    public class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private RectTransform _tileContainer;
        [SerializeField] private LevelTileView _tileTemplate;
        [SerializeField] private Button _backButton;
        private readonly List<LevelTileView> _tiles = new();

        public event Action<int> LevelChosen;
        public event Action BackPressed;

        private void Awake()
        {
            _tileTemplate.gameObject.SetActive(false);
            _backButton.onClick.AddListener(RaiseBack);
        }

        public void Build(IReadOnlyList<LevelButtonModel> levels)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                LevelTileView tile = Instantiate(_tileTemplate, _tileContainer);
                tile.gameObject.SetActive(true);

                int captured = i;
                tile.Clicked += () => LevelChosen?.Invoke(captured);
                tile.Bind(levels[i].Name, levels[i].Unlocked, levels[i].Stars, levels[i].IsSurvival);

                _tiles.Add(tile);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (this != null)
                Destroy(gameObject);
        }

        private void RaiseBack()
        {
            BackPressed?.Invoke();
        }

        /// <summary>Presenter → View data for one level-select tile: what to show, and whether it is tappable.</summary>
        public struct LevelButtonModel
        {
            public string Name;
            public bool Unlocked;
            public int Stars;
            public bool IsSurvival;
        }
    }
}
