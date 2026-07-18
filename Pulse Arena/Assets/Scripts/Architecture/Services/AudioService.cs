using System;
using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Architecture.Services
{
    /// <summary>
    ///     Persistent (ProjectContext) 2D one-shot SFX + music player. Plain C#, not a MonoBehaviour: the
    ///     <see cref="AudioSource" />s live on an <see cref="AudioHost" /> prefab this service instantiates and drives.
    ///     Clips/volume/pitch come from <see cref="AudioData" />; a round-robin pool lets sounds overlap. Missing clips
    ///     are silently ignored; self-cleans via <see cref="IDisposable" /> at shutdown.
    /// </summary>
    public class AudioService : IAudioService, IPausable, IDisposable
    {
        private readonly AudioData _data;
        private readonly Dictionary<GameSfx, SfxEntry> _map;
        private readonly IPauseService _pauseService;
        private readonly ISettingsService _settings;
        private readonly AudioSource _musicSource;
        private readonly AudioSource[] _sources;
        private readonly GameObject _hostObject;
        private int _next;
        private bool _paused;

        // NonLazy: Zenject builds this after all installers register, so resolving these services here is safe.
        public AudioService(GameSettings gameSettings, ISettingsService settings, IPauseService pauseService)
        {
            _data = gameSettings.AudioData;
            _settings = settings;
            _pauseService = pauseService;
            _map = BuildSfxMap(_data);

            AudioHost host = CreateHost(gameSettings.Prefabs.AudioHostPrefab);
            _hostObject = host.gameObject;
            _musicSource = host.MusicSource;
            _sources = host.SfxSources;

            if (_settings != null)
                _settings.Changed += OnSettingsChanged;

            _pauseService?.Register(this);
        }

        /// <summary>Pauses music + any in-flight SFX at their current sample (Pause, NOT Stop → resumes exactly).</summary>
        public void Pause()
        {
            if (_paused)
                return;

            _paused = true;
            _musicSource?.Pause();

            if (_sources != null)
                foreach (AudioSource source in _sources)
                    source?.Pause();
        }

        public void Resume()
        {
            if (!_paused)
                return;

            _paused = false;
            _musicSource?.UnPause();

            if (_sources != null)
                foreach (AudioSource source in _sources)
                    source?.UnPause();
        }

        public void PlayMusic(AudioClip clip)
        {
            if (_paused || _data == null || _musicSource == null || clip == null)
                return;

            // Already looping this track (e.g. re-entering the game scene) — keep it seamless.
            if (_musicSource.clip == clip && _musicSource.isPlaying)
                return;

            _musicSource.clip = clip;
            _musicSource.volume = MusicScale();
            _musicSource.Play();
        }

        public void PlaySfx(GameSfx sfx)
        {
            PlayInternal(sfx, null);
        }

        public void PlaySfx(GameSfx sfx, float pitch)
        {
            PlayInternal(sfx, Mathf.Clamp(pitch, 0.1f, 3f));
        }

        public void Dispose()
        {
            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;

            _pauseService?.Unregister(this);

            if (_hostObject != null)
                Object.Destroy(_hostObject);
        }

        private AudioHost CreateHost(GameObject prefab)
        {
            AudioHost host = Object.Instantiate(prefab).GetComponent<AudioHost>();
            host.gameObject.name = "AudioService";
            Object.DontDestroyOnLoad(host.gameObject); // ProjectContext-scoped: must survive every scene load.

            return host;
        }

        private static Dictionary<GameSfx, SfxEntry> BuildSfxMap(AudioData data)
        {
            Dictionary<GameSfx, SfxEntry> map = new Dictionary<GameSfx, SfxEntry>();

            if (data?.Sfx == null)
                return map;

            foreach (SfxEntry entry in data.Sfx)
                if (entry != null && entry.Clip != null)
                    map[entry.Id] = entry;

            return map;
        }

        // Live-update the music volume when the player drags a settings slider.
        private void OnSettingsChanged()
        {
            if (_musicSource != null)
                _musicSource.volume = MusicScale();
        }

        private float SfxScale()
        {
            return _settings == null
                ? 1f
                : Mathf.Clamp01(_settings.SfxVolume) * Mathf.Clamp01(_settings.MasterVolume);
        }

        private float MusicScale()
        {
            return _settings == null
                ? 1f
                : Mathf.Clamp01(_settings.MusicVolume) * Mathf.Clamp01(_settings.MasterVolume);
        }

        private void PlayInternal(GameSfx sfx, float? pitch)
        {
            if (_paused || _data == null || _map == null || _sources == null)
                return;

            if (!_map.TryGetValue(sfx, out SfxEntry entry) || entry.Clip == null)
                return;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;

            source.pitch = pitch ?? ResolvePitch(entry);
            source.PlayOneShot(entry.Clip, entry.Volume * SfxScale());
        }

        private static float ResolvePitch(SfxEntry entry)
        {
            if (entry.PitchMax < 0.05f || entry.PitchMax < entry.PitchMin)
                return 1f;

            return Mathf.Clamp(Random.Range(entry.PitchMin, entry.PitchMax), 0.1f, 3f);
        }
    }
}
