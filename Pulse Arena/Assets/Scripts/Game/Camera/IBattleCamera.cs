using UnityEngine;

namespace Game.Cameras
{
    public interface IBattleCamera
    {
        void Follow(Transform target, bool snap = true);
        void Shake(float duration, float strength);
    }
}
