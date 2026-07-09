using System;

namespace Architecture.Services.Interfaces
{
    public interface ISceneLoader
    {
        void Load(string sceneName, Action onLoaded = null);
    }
}