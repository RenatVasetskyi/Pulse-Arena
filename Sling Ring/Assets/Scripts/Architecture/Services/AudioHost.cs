using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Unity glue for <see cref="AudioService" />: a thin prefab component owning the actual
    ///     <see cref="AudioSource" />s (one looping music source + a round-robin SFX pool). Holds no audio logic —
    ///     the plain-C# service instantiates and drives it. Pool size lives here so it stays a designer knob.
    /// </summary>
    public class AudioHost : MonoBehaviour
    {
        [Tooltip("How many pooled SFX sources — lets that many one-shots overlap with independent pitch.")]
        [SerializeField] private int _sfxSourceCount = 8;

        public AudioSource MusicSource { get; private set; }
        public AudioSource[] SfxSources { get; private set; }

        private void Awake()
        {
            MusicSource = CreateSource(true);

            SfxSources = new AudioSource[Mathf.Max(1, _sfxSourceCount)];

            for (int i = 0; i < SfxSources.Length; i++)
                SfxSources[i] = CreateSource(false);
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D — global UI/gameplay audio, no positional falloff

            return source;
        }
    }
}
