using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    ///     The level-select overlay (a passive View): a jungle-temple background with a hand-placed set of ornate
    ///     level tiles. <see cref="Build" /> binds each tile in <see cref="_tiles" /> to one level (index → level) and
    ///     raises <see cref="LevelChosen" /> / <see cref="BackPressed" />; all progress logic lives in the presenter.
    ///     Tiles are positioned by hand on the <c>level_select</c> prefab — no grid, no runtime cloning.
    /// </summary>
    public class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private LevelTileView[] _tiles;
        [SerializeField] private Button _backButton;

        public event Action<int> LevelChosen;
        public event Action BackPressed;

        private void Awake()
        {
            _backButton.onClick.AddListener(RaiseBack);
            SubscribeTiles();
        }

        public void Build(IReadOnlyList<LevelButtonModel> levels)
        {
            for (int i = 0; i < _tiles.Length; i++)
                BindTile(i, levels);
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

        private void BindTile(int index, IReadOnlyList<LevelButtonModel> levels)
        {
            LevelTileView tile = _tiles[index];

            if (index >= levels.Count)
            {
                tile.gameObject.SetActive(false);
                return;
            }

            tile.gameObject.SetActive(true);
            tile.Bind(levels[index].Name, levels[index].Unlocked, levels[index].Stars, levels[index].IsSurvival);
        }

        /// <summary>
        ///     Wires each tile's click to <see cref="LevelChosen" /> exactly once, so the view can be reused
        ///     (shown/hidden and re-<see cref="Build" />-ed) without stacking duplicate subscriptions.
        /// </summary>
        private void SubscribeTiles()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                int captured = i;
                _tiles[i].Clicked += () => LevelChosen?.Invoke(captured);
            }
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
