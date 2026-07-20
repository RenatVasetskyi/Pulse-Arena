using Architecture.Services.Interfaces;
using Data;

namespace Architecture.Services
{
    /// <summary>
    ///     <see cref="IHapticService" /> over the BenoitFreslon/Vibration plugin. iOS gets a real light Taptic impact
    ///     (previously a fixed ~500ms system buzz); Android gets a short one-shot whose length comes from
    ///     <see cref="HapticData" /> — the plugin exposes no amplitude control there, so duration is the only
    ///     lightness knob.
    /// </summary>
    public class HapticService : IHapticService
    {
        private readonly GameSettings _gameSettings;
        private readonly ISettingsService _settingsService;

        public HapticService(ISettingsService settingsService, GameSettings gameSettings)
        {
            _settingsService = settingsService;
            _gameSettings = gameSettings;

            InitializePlugin();
        }

        public void PlayLight()
        {
            if (_settingsService == null || !_settingsService.VibrationEnabled)
                return;

#if UNITY_IOS && !UNITY_EDITOR
            Vibration.VibrateIOS(ImpactFeedbackStyle.Light);
#elif UNITY_ANDROID && !UNITY_EDITOR
            Vibration.VibrateAndroid(_gameSettings.Haptics.PlayerHitDurationMs);
#endif
        }

        // The plugin caches its JNI handles here; without it every Android call dereferences a null vibrator.
        private void InitializePlugin()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Vibration.Init();
#endif
        }
    }
}
