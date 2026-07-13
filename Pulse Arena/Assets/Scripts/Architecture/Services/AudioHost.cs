using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     The Unity glue for <see cref="AudioService" />: a thin prefab component that owns the actual
    ///     <see cref="AudioSource" />s (one looping music source + a round-robin SFX pool). It holds NO audio logic —
    ///     the plain-C# <c>AudioService</c> instantiates this prefab from config and drives these sources. This is the
    ///     "service is plain C#, its Unity components live on a host prefab" split, so the service itself needn't be a
    ///     MonoBehaviour. The pool size lives here (on the audio prefab) so it is a designer knob, not a code const.
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
