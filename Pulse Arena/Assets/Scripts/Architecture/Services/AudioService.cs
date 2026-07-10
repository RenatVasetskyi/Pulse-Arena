using System.Collections.Generic;
using Architecture.Services.Interfaces;
using Data;
using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Persistent (ProjectContext) 2D one-shot SFX player. Clips + volume/pitch come from
    ///     <see cref="AudioData" />; a small round-robin pool of AudioSources lets sounds overlap and
    ///     carry independent pitch. Missing/unassigned clips are silently ignored so partial audio works.
    /// </summary>
    public class AudioService : MonoBehaviour, IAudioService, IPausable
    {
        private const int SourceCount = 8;

        private AudioData _data;
        private Dictionary<GameSfx, SfxEntry> _map;
        private AudioSource _musicSource;

        private int _next;
        private bool _paused;
        private IPauseService _pauseService;
        private ISettingsService _settings;
        private AudioSource[] _sources;

        public void Initialize(AudioData data, ISettingsService settings, IPauseService pauseService)
        {
            _data = data;
            _settings = settings;
            _pauseService = pauseService;
            _map = new Dictionary<GameSfx, SfxEntry>();

            if (_data?.Sfx != null)
            {
                foreach (SfxEntry entry in _data.Sfx)
                    if (entry != null && entry.Clip != null)
                        _map[entry.Id] = entry;
            }

            _sources = new AudioSource[SourceCount];

            for (int i = 0; i < SourceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;

            if (_settings != null)
                _settings.Changed += OnSettingsChanged;

            _pauseService?.Register(this);
        }

        private void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= OnSettingsChanged;

            _pauseService?.Unregister(this);
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