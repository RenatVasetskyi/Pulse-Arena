using System.Collections;
using Architecture.Services.Interfaces;
using UI.Loading;

namespace Architecture.Services
{
    public class LoadingScreen : ILoadingScreen
    {
        private readonly LoadingScreenView _view;

        public LoadingScreen(LoadingScreenView view)
        {
            _view = view;
            _view.HideImmediate();
        }

        public IEnumerator Hide()
        {
            yield return _view.FadeTo(0f);
            _view.HideImmediate();
        }

        public void SetProgress(float progress)
        {
            _view.SetProgress(progress);
        }

        public IEnumerator Show()
        {
            ShowImmediate();
            yield break;
        }

        public void ShowImmediate()
        {
            _view.SetProgress(0f);
            _view.ShowImmediate();
        }
    }
}