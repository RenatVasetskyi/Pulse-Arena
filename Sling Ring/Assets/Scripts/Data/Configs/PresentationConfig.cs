using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Presentation Config", menuName = "Sling Ring/Configs/Presentation Config")]
    public class PresentationConfig : ScriptableObject
    {
        public VfxData Vfx = new();
        public CameraData Camera;
        public UiData Ui = new();
        public AudioData Audio = new();
        public HapticData Haptics = new();
        public OnboardingData Onboarding = new();
    }
}