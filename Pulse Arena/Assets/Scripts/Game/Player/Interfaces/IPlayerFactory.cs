using UnityEngine;

namespace Game.Player.Interfaces
{
    public interface IPlayerFactory
    {
        PlayerController Create(Vector3 at, Quaternion rotation, Transform parent);
    }
}
