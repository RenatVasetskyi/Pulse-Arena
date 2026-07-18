using UnityEngine;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Instantiates a UI window/panel prefab and returns its <typeparamref name="T" /> component, logging a
    ///     clear error (and returning null) if the prefab is unassigned or missing the component. One place all
    ///     windows are created, so presenters never repeat the Instantiate-or-LogError dance.
    /// </summary>
    public interface IWindowFactory
    {
        /// <summary>Scene-scoped window: lives and dies with the scene that created it (HUD, game-over, panels).</summary>
        T Create<T>(GameObject prefab, string prefabName) where T : Component;

        /// <summary>Cross-scene window (DontDestroyOnLoad) — e.g. the settings panel shared by menu and match.</summary>
        T CreatePersistent<T>(GameObject prefab, string prefabName) where T : Component;
    }
}
