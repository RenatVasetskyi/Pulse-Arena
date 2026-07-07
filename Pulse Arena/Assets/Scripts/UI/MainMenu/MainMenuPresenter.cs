using System;
using Architecture.Services.Interfaces;
using Architecture.States;
using Architecture.States.Interfaces;
using Data;

namespace UI.MainMenu
{
    public class MainMenuPresenter : IDisposable
    {
        private readonly MainMenuView _view;
        private readonly IStateMachine _stateMachine;
        private readonly IAudioService _audioService;

        public MainMenuPresenter(MainMenuView view, IStateMachine stateMachine, IAudioService audioService)
        {
            _view = view;
            _stateMachine = stateMachine;
            _audioService = audioService;
        }

        public void Initialize()
        {
            _view.PlayClicked += OnPlayClicked;
            _view.Show();
        }

        public void Dispose()
        {
            if (_view == null)
                return;

            _view.PlayClicked -= OnPlayClicked;
            _view.Dispose();
        }

        private void OnPlayClicked()
        {
            _audioService?.PlaySfx(GameSfx.UiClick);
            _view.Hide();
            _stateMachine.Enter<StartGameState>();
        }
    }
}
