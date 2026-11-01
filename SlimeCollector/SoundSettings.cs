using System;
using UnityEngine;

public static class SoundSettings
{
    private const string SfxKey = "SfxEnabled";
    private const string BgmKey = "BgmEnabled";

    public static event Action OnChanged;

    public static bool SfxEnabled
    {
        get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool BgmEnabled
    {
        get => PlayerPrefs.GetInt(BgmKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(BgmKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }
}