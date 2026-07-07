using UnityEngine;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    /// Spawns floating "+N" score popups in the world. Lets gameplay ask for a popup without knowing which
    /// UI class draws it.
    /// </summary>
    public interface IScorePopupService
    {
        void Show(Vector3 position, string value);
    }
}
