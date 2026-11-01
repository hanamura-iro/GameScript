using UnityEngine;

public static class Sfx
{
    private const string MutedKey = "ShapeFit_Muted";

    private static bool muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
    public static bool Muted
    {
        get => muted;
        set
        {
            muted = value;
            PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void Pickup() { if (!muted) RuntimeAudio.PlayTone(420f, 0.05f, 0.35f); }
    public static void Rotate() { if (!muted) RuntimeAudio.PlayTone(520f, 0.05f, 0.35f); }

    private static readonly float[] PlaceScale =
    {
        523.25f, 587.33f, 659.25f, 783.99f, 880.00f,
        1046.50f, 1174.66f, 1318.51f, 1567.98f, 1760.00f
    };

    public static void Place(float fillRatio = 0f)
    {
        if (muted) return;
        int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(fillRatio) * (PlaceScale.Length - 1)), 0, PlaceScale.Length - 1);
        RuntimeAudio.PlayTone(PlaceScale[index], 0.09f, 0.5f);
    }
    public static void Invalid() { if (!muted) RuntimeAudio.PlayTone(160f, 0.15f, 0.4f); }
    public static void Tick() { if (!muted) RuntimeAudio.PlayTone(880f, 0.05f, 0.3f); }
    public static void GameOver() { if (!muted) RuntimeAudio.PlayTone(140f, 0.5f, 0.5f); }

    public static void StageClear(MonoBehaviour runner)
    {
        if (muted) return;
        RuntimeAudio.PlaySequence(runner, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.08f, 0.45f);
    }
}
