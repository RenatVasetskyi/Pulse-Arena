using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace UI
{
    /// <summary>
    ///     UI-side implementation of <see cref="IScorePopupService" />: spawns pooled <see cref="FloatingScoreText" />
    ///     popups. Bound in the composition root, so gameplay depends only on the interface — not on the UI class.
    /// </summary>
    public class ScorePopupService : IScorePopupService
    {
        private readonly GameSettings _gameSettings;

        public ScorePopupService(GameSettings gameSettings)
        {
            _gameSettings = gameSettings;
        }

        public void Show(Vector3 position, string value)
        {
            FloatingScoreText.Create(_gameSettings.Prefabs.FloatingScoreTextPrefab, position, value, _gameSettings.Vfx);
        }
    }
}