using System.Collections;

namespace Architecture.Services.Interfaces
{
    public interface ILoadingScreen
    {
        IEnumerator Show();
        void ShowImmediate();
        void SetProgress(float progress);
        IEnumerator Hide();
    }
}
