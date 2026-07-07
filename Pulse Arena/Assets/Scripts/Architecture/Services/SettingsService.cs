using System;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    /// PlayerPrefs-backed user settings. Defaults come from the config (AudioData / CameraData) on first
    /// run; every setter persists immediately and raises <see cref="Changed"/> so live systems update.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string MasterKey = "settings.master";
        private const string MusicKey = "settings.music";
        private const string SfxKey = "settings.sfx";
        private const string CameraFxKey = "settings.camerafx";
        private const string ZoomKey = "settings.zoom";
        private const string VibrateKey = "settings.vibrate";

        private readonly float _minZoom;
        private readonly float _maxZoom;

        private float _master;
        private float _music;
        private float _sfx;
        private bool _cameraFx;
        private float _zoom;
        private bool _vibrate;

        public event Action Changed;

        public float MasterVolume => _master;
        public float MusicVolume => _music;
        public float SfxVolume => _sfx;
        public bool CameraEffectsEnabled => _cameraFx;
        public float CameraZoom => _zoom;
        public bool VibrationEnabled => _vibrate;

        public SettingsService(GameSettings gameSettings)
        {
            AudioData audio = gameSettings.AudioData;
            CameraData camera = gameSettings.CameraData;

            _minZoom = camera != null ? camera.MinZoom : 0.5f;
            _maxZoom = camera != null ? camera.MaxZoom : 1.5f;

            _master = PlayerPrefs.GetFloat(MasterKey, audio != null ? audio.MasterVolume : 1f);
            _music = PlayerPrefs.GetFloat(MusicKey, audio != null ? audio.MusicVolume : 0.45f);
            _sfx = PlayerPrefs.GetFloat(SfxKey, audio != null ? audio.SfxVolume : 0.85f);
            _cameraFx = PlayerPrefs.GetInt(CameraFxKey, 1) == 1;
            _zoom = PlayerPrefs.GetFloat(ZoomKey, camera != null ? camera.DefaultZoom : 1f);
            _vibrate = PlayerPrefs.GetInt(VibrateKey, 1) == 1;
        }

        public void SetMasterVolume(float value)
        {
            _master = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterKey, _master);
            Commit();
        }

        public void SetMusicVolume(float value)
        {
            _music = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, _music);
            Commit();
        }

        public void SetSfxVolume(float value)
        {
            _sfx = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, _sfx);
            Commit();
        }

        public void SetCameraEffectsEnabled(bool enabled)
        {
            _cameraFx = enabled;
            PlayerPrefs.SetInt(CameraFxKey, enabled ? 1 : 0);
            Commit();
        }

        public void SetCameraZoom(float value)
        {
            _zoom = Mathf.Clamp(value, _minZoom, _maxZoom);
            PlayerPrefs.SetFloat(ZoomKey, _zoom);
            Commit();
        }

        public void SetVibrationEnabled(bool enabled)
        {
            _vibrate = enabled;
            PlayerPrefs.SetInt(VibrateKey, enabled ? 1 : 0);
            Commit();
        }

        private void Commit()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
