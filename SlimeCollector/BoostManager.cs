using System;
using UnityEngine;

public class BoostManager : MonoBehaviour
{
    public static BoostManager instance;

    public enum BoostType { GoldDouble, AutoTouch }

    [Header("골드 2배")]
    public float goldDoubleMinutes = 5f;
    public int goldMultiplier = 2;

    [Header("자동 터치")]
    public float autoTouchMinutes = 3f;
    public float autoTapsPerSecond = 5f;

    private const string KeyPrefix = "BoostEnd_";
    private readonly long[] endTicks = new long[2];
    private float tapTimer;

    public int GoldMultiplier => IsActive(BoostType.GoldDouble) ? goldMultiplier : 1;

    private void Awake()
    {
        instance = this;

        for (int i = 0; i < endTicks.Length; i++)
        {
            long.TryParse(PlayerPrefs.GetString(KeyPrefix + i, "0"), out endTicks[i]);
        }
    }

    public float DurationSeconds(BoostType type)
    {
        return (type == BoostType.GoldDouble ? goldDoubleMinutes : autoTouchMinutes) * 60f;
    }

    public float Remaining(BoostType type)
    {
        long end = endTicks[(int)type];
        if (end <= 0) return 0f;

        double remain = TimeSpan.FromTicks(end - DateTime.UtcNow.Ticks).TotalSeconds;
        return Mathf.Clamp((float)remain, 0f, DurationSeconds(type));
    }

    public bool IsActive(BoostType type) => Remaining(type) > 0f;

    public void Activate(BoostType type)
    {
        long end = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(DurationSeconds(type)).Ticks;
        endTicks[(int)type] = end;

        PlayerPrefs.SetString(KeyPrefix + (int)type, end.ToString());
        PlayerPrefs.Save();

        Haptics.Vibrate(40, 180);
    }

    private void Update()
    {
        if (!IsActive(BoostType.AutoTouch) || autoTapsPerSecond <= 0f) return;
        if (DiscoveryPopup.IsOpen || OfflineReward.IsOpen) return;

        float interval = 1f / autoTapsPerSecond;
        tapTimer += Time.deltaTime;

        while (tapTimer >= interval)
        {
            tapTimer -= interval;

            Vector2 pos = new Vector2(
                UnityEngine.Random.Range(Screen.width * 0.15f, Screen.width * 0.85f),
                UnityEngine.Random.Range(Screen.height * 0.35f, Screen.height * 0.65f));

            GameManager.instance.GiveTouchGold(pos);
        }
    }
}