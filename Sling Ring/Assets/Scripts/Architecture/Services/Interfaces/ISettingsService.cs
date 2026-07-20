using System;

namespace Architecture.Services.Interfaces
{
    /// <summary>
    ///     Persistent user settings (PlayerPrefs-backed). Consumers read the properties and subscribe to
    ///     <see cref="Changed" /> to react live (audio volumes, screen shake, camera zoom, vibration).
    /// </summary>
    public interface ISettingsService
    {
        event Action Changed;
        bool CameraEffectsEnabled { get; }
        float CameraZoom { get; }
        float MasterVolume { get; }
        float MusicVolume { get; }
        float SfxVolume { get; }
        bool VibrationEnabled { get; }
        void SetCameraEffectsEnabled(bool enabled);
        void SetCameraZoom(float value);

        void SetMasterVolume(float value);
        void SetMusicVolume(float value);
        void SetSfxVolume(float value);
        void SetVibrationEnabled(bool enabled);
    }
}