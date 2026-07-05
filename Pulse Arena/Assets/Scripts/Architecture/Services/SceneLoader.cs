using System;
using System.Collections;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Architecture.Services
{
    public class SceneLoader : ISceneLoader
    {
        private readonly ICoroutineRunner _coroutineRunner;
        private readonly ILoadingScreen _loadingScreen;
        private readonly GameSettings _gameSettings;

        public SceneLoader(ICoroutineRunner coroutineRunner, ILoadingScreen loadingScreen,
            GameSettings gameSettings)
        {
            _coroutineRunner = coroutineRunner;
            _loadingScreen = loadingScreen;
            _gameSettings = gameSettings;
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

            while (Time.unscaledTime - startedAt < _gameSettings.MinLoadingScreenTime)
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
