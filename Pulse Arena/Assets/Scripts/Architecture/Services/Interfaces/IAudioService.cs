using Data;
using UnityEngine;

namespace Architecture.Services.Interfaces
{
    public interface IAudioService
    {
        void PlaySfx(GameSfx sfx);
        void PlayMusic(AudioClip clip);
        void StopMusic();
    }
}
