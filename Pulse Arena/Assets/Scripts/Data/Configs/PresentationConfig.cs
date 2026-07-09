using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Presentation Config", menuName = "Pulse Arena/Configs/Presentation Config")]
    public class PresentationConfig : ScriptableObject
    {
        public VfxData Vfx = new();
        public CameraData Camera;
        public UiData Ui = new();
        public AudioData Audio = new();
    }
}