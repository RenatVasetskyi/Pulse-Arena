using UnityEngine;

namespace Game.Cameras
{
    public interface IBattleCamera
    {
        void Follow(Transform target, bool snap = true);
        void PlayLassoLaunch(float chargeProgress);
        void PlayPlayerHit();
        void Shake(float duration, float strength);
        void ZoomIn();
        void ZoomOut();
        void PlayDeathZoom();
    }
}