using System.Collections;

namespace Architecture.Services.Interfaces
{
    public interface ILoadingScreen
    {
        IEnumerator Hide();
        void SetProgress(float progress);
        IEnumerator Show();
        void ShowImmediate();
    }
}