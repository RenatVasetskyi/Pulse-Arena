using UnityEngine;

namespace Architecture.Services
{
    /// <summary>
    ///     Native Android one-shot vibration with amplitude control (API 26+). Used for short/weak damage haptics —
    ///     <see cref="Handheld.Vibrate" /> can only fire a fixed ~500ms full-strength buzz, which is far too strong
    ///     for a hit tick. A no-op in the editor and on non-Android platforms, and best-effort: a haptics failure
    ///     (vendor quirk, missing permission) must never bubble into gameplay.
    /// </summary>
    public static class AndroidHaptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static AndroidJavaClass _effectClass;
#endif

        public static void VibrateOneShot(long milliseconds, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                EnsureVibrator();

                if (_vibrator == null || _effectClass == null)
                    return;

                using (AndroidJavaObject effect =
                       _effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                {
                    _vibrator.Call("vibrate", effect);
                }
            }
            catch (System.Exception)
            {
                // Haptics are cosmetic — swallow so a device/API quirk can never break the game loop.
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Cache the Vibrator + VibrationEffect class once — resolving them every hit would thrash JNI.
        private static void EnsureVibrator()
        {
            if (_vibrator != null)
                return;

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }

            _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
        }
#endif
    }
}
