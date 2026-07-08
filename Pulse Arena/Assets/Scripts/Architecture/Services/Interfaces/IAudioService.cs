using Data;
using UnityEngine;

namespace Architecture.Services.Interfaces
{
    public interface IAudioService
    {
        void PlaySfx(GameSfx sfx);
        void PlaySfx(GameSfx sfx, float pitch);
        void PlayMusic(AudioClip clip);
    }
}
