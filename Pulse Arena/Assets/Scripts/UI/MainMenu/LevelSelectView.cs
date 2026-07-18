using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    /// <summary>
    ///     The level-select overlay (a passive View): hand-placed campaign tiles plus a wide always-open survival
    ///     button. <see cref="Build" /> binds each tile to a campaign level, <see cref="BindSurvival" /> points the
    ///     survival button at the endless level; both raise <see cref="LevelChosen" />. Progress logic lives in the presenter.
    /// </summary>
    public class LevelSelectView : MonoBehaviour
    {
        [SerializeField] private LevelTileView[] _tiles;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _survivalButton;
        [SerializeField] private TextMeshProUGUI _survivalScore;
        private int _survivalIndex = -1;

        public event Action<int> LevelChosen;
        public event Action BackPressed;

        private void Awake()
        {
            _backButton.onClick.AddListener(RaiseBack);
            _survivalButton.onClick.AddListener(RaiseSurvival);
            SubscribeTiles();
        }

        public void Build(IReadOnlyList<LevelButtonModel> levels)
        {
            for (int i = 0; i < _tiles.Length; i++)
                BindTile(i, levels);
        }

        /// <summary>Points the survival button at its level and shows the best score — kept apart from the campaign tiles because it shows a score, not a lock/stars.</summary>
        public void BindSurvival(int levelIndex, int bestScore, string scoreFormat)
        {
            _survivalIndex = levelIndex;
            _survivalScore.text = string.Format(scoreFormat, bestScore);
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

        /// <summary>Wires each tile's click once so the reused view (shown/hidden, re-<see cref="Build" />-ed) doesn't stack duplicate subscriptions.</summary>
        private void SubscribeTiles()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                int captured = i;
                _tiles[i].Clicked += () => LevelChosen?.Invoke(captured);
            }
        }

        private void RaiseSurvival()
        {
            LevelChosen?.Invoke(_survivalIndex);
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
