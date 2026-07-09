using System;
using Architecture.Services.Interfaces;
using Architecture.States;
using Architecture.States.Interfaces;
using Data;

namespace UI.MainMenu
{
    public class MainMenuPresenter : IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly ISettingsController _settingsController;
        private readonly IStateMachine _stateMachine;
        private readonly MainMenuView _view;

        public MainMenuPresenter(MainMenuView view, IStateMachine stateMachine, IAudioService audioService,
            ISettingsController settingsController)
        {
            _view = view;
            _stateMachine = stateMachine;
            _audioService = audioService;
            _settingsController = settingsController;
        }

        public void Dispose()
        {
            if (_view == null)
                return;

            _view.PlayClicked -= OnPlayClicked;
            _view.SettingsClicked -= OnSettingsClicked;
            _view.Dispose();
        }

        public void Initialize()
        {
            _view.PlayClicked += OnPlayClicked;
            _view.SettingsClicked += OnSettingsClicked;
            _view.Show();
        }

        private void OnPlayClicked()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _view.Hide();
            _stateMachine.Enter<LoadGameState>();
        }

        private void OnSettingsClicked()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _settingsController?.Open();
        }
    }
}