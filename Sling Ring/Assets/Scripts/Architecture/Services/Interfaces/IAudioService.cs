using Data;
using UnityEngine;

namespace Architecture.Services.Interfaces
{
    public interface IAudioService
    {
        void PlayMusic(AudioClip clip);
        void PlaySfx(GameSfx sfx);
        void PlaySfx(GameSfx sfx, float pitch);
    }
}