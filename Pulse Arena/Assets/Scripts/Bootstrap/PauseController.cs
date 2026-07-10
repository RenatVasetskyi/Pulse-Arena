using System;
using Architecture.Services.Interfaces;
using UI.Pause;
using UnityEngine;

namespace Game.Scene
{
    /// <summary>
    ///     Drives the in-game pause: mechanically freezes gameplay via <see cref="IPauseService" /> (each system
    ///     caches + restores its own state — NOT Time.timeScale=0), mutes gameplay input, and shows the pause
    ///     panel. Buttons are wired to resume, open settings, restart and quit-to-menu (the last two as callbacks).
    /// </summary>
    public class PauseController
    {
        private readonly IInputService _inputService;
        private readonly IPauseService _pauseService;
        private readonly Action _quitToMenu;
        private readonly Action _restart;
        private readonly ISettingsController _settingsController;
        private readonly ISlowMoService _slowMoService;
        private readonly PausePanelView _view;

        private bool _paused;

        public PauseController(PausePanelView view, IInputService inputService,
            ISettingsController settingsController, ISlowMoService slowMoService, IPauseService pauseService,
            Action restart, Action quitToMenu)
        {
            _view = view;
            _inputService = inputService;
            _settingsController = settingsController;
            _slowMoService = slowMoService;
            _pauseService = pauseService;
            _restart = restart;
            _quitToMenu = quitToMenu;

            _view.ResumeClicked += Resume;
            _view.SettingsClicked += OnSettings;
            _view.RestartClicked += OnRestart;
            _view.MenuClicked += OnMenu;
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

        public void Pause()
        {
            if (_paused)
                return;

            _paused = true;
            _slowMoService?.Pause();  // suspend an in-progress bullet-time (do NOT lose it)
            _pauseService.Pause();    // mechanically freeze every gameplay system at its current state
            _inputService.Disable();
            _view.Show();
        }

        public void Resume()
        {
            if (!_paused)
                return;

            _paused = false;
            _pauseService.Unpause();  // continue every system from exactly where it froze
            _slowMoService?.Resume(); // continue any suspended bullet-time
            _inputService.Enable();
            _view.Close();
        }

        public void Toggle()
        {
            if (_paused)
                Resume();
            else
                Pause();
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