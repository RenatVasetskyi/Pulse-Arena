using UnityEngine;

namespace Game.Arena.Interfaces
{
    public interface IPitFactory
    {
        Pit Create(Vector3 at, float scale, float lifetime, Transform parent);
    }
}
