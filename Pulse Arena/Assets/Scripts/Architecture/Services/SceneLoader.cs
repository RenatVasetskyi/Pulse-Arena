using System;
using System.Collections;
using Architecture.Services.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Architecture.Services
{
    public class SceneLoader : ISceneLoader
    {
        private const float MinLoadingScreenTime = 0.35f;

        private readonly ICoroutineRunner _coroutineRunner;
        private readonly ILoadingScreen _loadingScreen;

        public SceneLoader(ICoroutineRunner coroutineRunner, ILoadingScreen loadingScreen)
        {
            _coroutineRunner = coroutineRunner;
            _loadingScreen = loadingScreen;
        }

        public void Load(string sceneName, Action onLoaded = null)
        {
            _coroutineRunner.StartCoroutine(LoadScene(sceneName, onLoaded));
        }

        private IEnumerator LoadScene(string sceneName, Action onLoaded)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                onLoaded?.Invoke();
                yield break;
            }

            yield return _loadingScreen.Show();

            float startedAt = Time.unscaledTime;
            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(sceneName);
            waitNextScene.allowSceneActivation = false;

            while (waitNextScene.progress < 0.9f)
            {
                _loadingScreen.SetProgress(Mathf.Clamp01(waitNextScene.progress / 0.9f));
                yield return null;
            }

            _loadingScreen.SetProgress(1f);

            while (Time.unscaledTime - startedAt < MinLoadingScreenTime)
                yield return null;

            waitNextScene.allowSceneActivation = true;

            while (!waitNextScene.isDone)
                yield return null;

            onLoaded?.Invoke();

            yield return null;
            yield return _loadingScreen.Hide();
        }
    }
}
