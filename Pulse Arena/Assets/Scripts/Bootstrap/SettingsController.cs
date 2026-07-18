using Architecture.Services.Interfaces;
using Data;
using UI.Settings;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Owns the single shared settings panel instance (lazily created, kept across scenes) and opens it. Both the
    ///     main menu and the in-game pause call into this; the panel hides itself via its own close button.
    /// </summary>
    public class SettingsController : ISettingsController
    {
        private readonly GameSettings _gameSettings;
        private readonly ISettingsService _settings;
        private readonly IWindowFactory _windowFactory;

        private SettingsView _view;

        public SettingsController(GameSettings gameSettings, ISettingsService settings, IWindowFactory windowFactory)
        {
            _gameSettings = gameSettings;
            _settings = settings;
            _windowFactory = windowFactory;
        }

        public void Open()
        {
            if (!EnsureView())
                return;

            _view.Show();
        }

        private bool EnsureView()
        {
            if (_view != null)
                return true;

            _view = _windowFactory.CreatePersistent<SettingsView>(_gameSettings.Prefabs.SettingsPanelPrefab,
                "SettingsPanelPrefab");

            if (_view == null)
                return false;

            CameraData camera = _gameSettings.CameraData;
            float min = camera != null ? camera.MinZoom : 0.5f;
            float max = camera != null ? camera.MaxZoom : 1.5f;

            _view.Bind(_settings, min, max);
            return true;
        }
    }
}