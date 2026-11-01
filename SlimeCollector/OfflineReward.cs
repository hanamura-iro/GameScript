using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class OfflineReward : MonoBehaviour
{
    public static OfflineReward instance;
    public static bool IsOpen => instance != null && instance.isOpen;

    [Header("보상 설정")]
    public float maxHours = 4f;          // 최대 몇 시간까지 쌓이는지
    [Range(0f, 1f)]
    public float rewardRate = 0.5f;      // 초당 수익의 몇 %를 줄지
    public float minSeconds = 60f;       // 이보다 짧게 비웠으면 팝업 없음

    [Header("UI 연결")]
    public RectTransform panel;
    public TextMeshProUGUI timeText;     //2시간 15분 동안
    public TextMeshProUGUI amountText;   //+3만 2천원
    public Button claimButton;           //그냥 받기
    public Button doubleButton;          //광고 보고 2배 받기

    [Header("연출")]
    public float countUpDuration = 0.8f;

    private CanvasGroup root;
    private bool isOpen;
    private bool waitingAd;
    private long pendingReward;
    private double pendingSeconds;
    private bool reachedCap;
    private long pauseTicks;
    private Coroutine countRoutine;

    private void Awake()
    {
        instance = this;
        root = GetComponent<CanvasGroup>();

        if (claimButton != null) claimButton.onClick.AddListener(Claim);
        if (doubleButton != null) doubleButton.onClick.AddListener(ClaimDouble);

        Hide();
    }
    private IEnumerator Start()
    {
        yield return null;

        long lastSave = SaveManager.instance != null ? SaveManager.instance.LastSaveTicks : 0;
        if (lastSave > 0)
        {
            TryShow(DateTime.UtcNow.Ticks - lastSave);
        }
    }

    private void Update()
    {
        if (doubleButton != null)
        {
            doubleButton.interactable = isOpen && !waitingAd
                && AdManager.instance != null && AdManager.instance.IsReady;
        }

        if (claimButton != null)
        {
            claimButton.interactable = isOpen && !waitingAd;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (AdManager.instance != null && AdManager.instance.IsShowing) return;

        if (paused)
        {
            pauseTicks = DateTime.UtcNow.Ticks;
        }
        else if (pauseTicks > 0)
        {
            TryShow(DateTime.UtcNow.Ticks - pauseTicks);
            pauseTicks = 0;
        }
    }

    private void TryShow(long elapsedTicks)
    {
        if (elapsedTicks <= 0) return;   //기기 시계를 과거로 돌린 경우

        double seconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
        if (seconds < minSeconds) return;

        double maxSeconds = maxHours * 3600.0;
        double capped = Math.Min(seconds, maxSeconds);
        if (seconds > maxSeconds) reachedCap = true;

        double income = CurrentIncomePerSecond();
        long reward = (long)(income * capped * rewardRate);
        if (reward <= 0) return;

        //팝업이 떠 있는 상태에서 또 다녀오면 합산
        pendingReward += reward;
        pendingSeconds = Math.Min(pendingSeconds + capped, maxSeconds);

        if (timeText != null)
        {
            string text = $"{FormatTime(pendingSeconds)} 동안";
            if (reachedCap) text += $"\n(최대 {maxHours:0.#}시간까지 쌓여요)";
            timeText.text = text;
        }

        if (!isOpen)
        {
            StartCoroutine(OpenRoutine());
        }

        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountUpRoutine(pendingReward));
    }

    private double CurrentIncomePerSecond()
    {
        if (SlimeEconomy.instance == null) return 0;

        double total = 0;
        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go == null) continue;

            SlimeBase slimeBase = go.GetComponent<SlimeBase>();
            if (slimeBase != null)
            {
                total += SlimeEconomy.instance.GetIncome((int)slimeBase.Slimetype);
            }
        }
        return total;
    }

    public void Claim()
    {
        ClaimWith(1);
    }

    public void ClaimDouble()
    {
        if (!isOpen || waitingAd || AdManager.instance == null) return;

        waitingAd = true;
        bool shown = AdManager.instance.ShowRewarded(
            () => { waitingAd = false; ClaimWith(2); },
            () => { waitingAd = false; });

        if (!shown) waitingAd = false;
    }

    private void ClaimWith(int multiplier)
    {
        if (!isOpen) return;

        GameManager.instance.currentGold += pendingReward * multiplier;
        pendingReward = 0;
        pendingSeconds = 0;
        reachedCap = false;

        Haptics.Vibrate(40, 180);

        if (countRoutine != null)
        {
            StopCoroutine(countRoutine);
            countRoutine = null;
        }

        StartCoroutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        isOpen = true;
        root.blocksRaycasts = true;
        root.interactable = true;

        float t = 0f;
        const float duration = 0.35f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            root.alpha = Mathf.Clamp01(t / 0.2f);
            if (panel != null) panel.localScale = Vector3.one * EaseOutBack(k);
            yield return null;
        }

        root.alpha = 1f;
        if (panel != null) panel.localScale = Vector3.one;
    }

    private IEnumerator CloseRoutine()
    {
        isOpen = false;
        root.interactable = false;

        float t = 0f;
        const float duration = 0.15f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            root.alpha = 1f - k;
            if (panel != null) panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
            yield return null;
        }

        Hide();
    }

    private IEnumerator CountUpRoutine(long target)
    {
        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / countUpDuration);
            float ease = 1f - (1f - k) * (1f - k);
            SetAmount((long)(target * ease));
            yield return null;
        }

        SetAmount(target);
        countRoutine = null;
    }

    private void SetAmount(long value)
    {
        if (amountText != null)
        {
            amountText.text = "+" + GameManager.FormatKoreanNumber(value);
        }
    }

    private void Hide()
    {
        isOpen = false;
        root.alpha = 0f;
        root.blocksRaycasts = false;
        root.interactable = false;
    }

    private static string FormatTime(double seconds)
    {
        int hours = (int)(seconds / 3600);
        int minutes = (int)(seconds % 3600 / 60);

        if (hours > 0) return $"{hours}시간 {minutes}분";
        return $"{minutes}분";
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}