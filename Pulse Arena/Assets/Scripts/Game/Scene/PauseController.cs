using System;
using Architecture.Services.Interfaces;
using UI.Pause;
using UnityEngine;

namespace Game.Scene
{
    /// <summary>
    /// Drives the in-game pause: freezes time, mutes gameplay input, shows the pause panel. Buttons are
    /// wired to resume, open settings, restart and quit-to-menu (the last two supplied as callbacks).
    /// </summary>
    public class PauseController
    {
        private readonly PausePanelView _view;
        private readonly IInputService _inputService;
        private readonly ISettingsController _settingsController;
        private readonly ISlowMoService _slowMoService;
        private readonly Action _restart;
        private readonly Action _quitToMenu;

        private bool _paused;

        public PauseController(PausePanelView view, IInputService inputService,
            ISettingsController settingsController, ISlowMoService slowMoService, Action restart, Action quitToMenu)
        {
            _view = view;
            _inputService = inputService;
            _settingsController = settingsController;
            _slowMoService = slowMoService;
            _restart = restart;
            _quitToMenu = quitToMenu;

            _view.ResumeClicked += Resume;
            _view.SettingsClicked += OnSettings;
            _view.RestartClicked += OnRestart;
            _view.MenuClicked += OnMenu;
            _view.Hide();
        }

        public bool IsPaused => _paused;

        public void Toggle()
        {
            if (_paused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (_paused)
                return;

            _paused = true;
            _slowMoService?.Stop();
            Time.timeScale = 0f;
            _inputService.Disable();
            _view.Show();
        }

        public void Resume()
        {
            if (!_paused)
                return;

            _paused = false;
            Time.timeScale = 1f;
            _inputService.Enable();
            _view.Hide();
        }

        public void Dispose()
        {
            if (_view == null)
                return;

            _view.ResumeClicked -= Resume;
            _view.SettingsClicked -= OnSettings;
            _view.RestartClicked -= OnRestart;
            _view.MenuClicked -= OnMenu;
        }

        private void OnSettings()
        {
            _settingsController?.Open();
        }

        private void OnRestart()
        {
            Resume();
            _restart?.Invoke();
        }

        private void OnMenu()
        {
            Resume();
            _quitToMenu?.Invoke();
        }
    }
}
