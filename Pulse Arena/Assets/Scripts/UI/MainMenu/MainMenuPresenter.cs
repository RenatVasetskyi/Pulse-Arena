using System;
using Architecture.States;
using Architecture.States.Interfaces;

namespace UI.MainMenu
{
    public class MainMenuPresenter : IDisposable
    {
        private readonly MainMenuView _view;
        private readonly IStateMachine _stateMachine;

        public MainMenuPresenter(MainMenuView view, IStateMachine stateMachine)
        {
            _view = view;
            _stateMachine = stateMachine;
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
            _view.Hide();
            _stateMachine.Enter<StartGameState>();
        }
    }
}
