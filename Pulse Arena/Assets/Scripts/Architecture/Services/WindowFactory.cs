using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     <see cref="IWindowFactory" /> — the single place UI window prefabs are instantiated. Plain C#
    ///     (<see cref="Object.Instantiate(Object)" /> is static), bound on the ProjectContext so every
    ///     presenter/controller creates windows through the same guarded path.
    /// </summary>
    public sealed class WindowFactory : IWindowFactory
    {
        public T Create<T>(GameObject prefab, string prefabName) where T : Component
        {
            return Build<T>(prefab, prefabName, false);
        }

        public T CreatePersistent<T>(GameObject prefab, string prefabName) where T : Component
        {
            return Build<T>(prefab, prefabName, true);
        }

        private static T Build<T>(GameObject prefab, string prefabName, bool persistent) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"{prefabName} is not assigned in Game Settings → Prefabs.");
                return null;
            }

            GameObject instance = Object.Instantiate(prefab);

            if (persistent)
                Object.DontDestroyOnLoad(instance);

            T component = instance.GetComponent<T>();

            if (component == null)
                Debug.LogError($"{prefabName} has no {typeof(T).Name} component.");

            return component;
        }
    }
}
