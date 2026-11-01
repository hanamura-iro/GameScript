using UnityEngine;

public static class Haptics
{
    private const string PrefKey = "HapticsEnabled";

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
        set => PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static int sdkInt = -1;

    private static void Init()
    {
        if (vibrator != null) return;

        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            sdkInt = version.GetStatic<int>("SDK_INT");
        }
    }
#endif

    public static void Vibrate(int milliseconds, int amplitude)
    {
        if (!Enabled || milliseconds <= 0) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Init();
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;

            amplitude = Mathf.Clamp(amplitude, 1, 255);

            if (sdkInt >= 26)
            {
                using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude))
                {
                    vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                vibrator.Call("vibrate", (long)milliseconds);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"진동 실패: {e.Message}");

            // 기기 진동 API가 실패했을 때만 기본 진동 사용 (강한 진동만)
            // 이 호출이 있어야 빌드 시 진동 권한이 자동으로 들어감
            if (milliseconds >= 30) Handheld.Vibrate();
        }
#endif
    }
}