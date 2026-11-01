using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdManager : MonoBehaviour
{
    public static AdManager instance;

    [Header("광고 단위 ID")]
    public bool useTestAds = true; // 출시 전까지 반드시 true
    public string rewardedAdUnitId = ""; // 출시용 실제 보상형 광고 ID

    [Header("안내 문구")]
    public string notReadyMessage = "광고를 불러오는 중입니다.";

    private const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";

    private static bool initialized;

    private RewardedAd rewardedAd;
    private bool loading;
    private float retryDelay = 2f;

    private Action pendingReward;
    private Action pendingFail;
    private bool rewardEarned;

    public bool IsReady => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsShowing { get; private set; }
    public string LastError { get; private set; } = "";

    private string UnitId =>
        useTestAds || string.IsNullOrEmpty(rewardedAdUnitId) ? TestRewardedId : rewardedAdUnitId;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        if (initialized)
        {
            Load();
            return;
        }

        MobileAds.Initialize(status =>
        {
            initialized = true;
            Load();
        });
    }

    private void OnDestroy()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }

    private void Load()
    {
        if (loading || IsReady) return;
        loading = true;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(UnitId, new AdRequest(), (ad, error) =>
        {
            loading = false;

            if (error != null || ad == null)
            {
                LastError = error != null ? $"{error.GetCode()}: {error.GetMessage()}" : "알 수 없음";
                Debug.LogWarning($"[AdManager] 광고 로드 실패 - {LastError}");

                Invoke(nameof(Load), retryDelay);
                retryDelay = Mathf.Min(retryDelay * 2f, 60f);
                return;
            }

            Debug.Log("[AdManager] 광고 로드 성공");
            LastError = "";
            retryDelay = 2f;
            rewardedAd = ad;

            ad.OnAdFullScreenContentClosed += () =>
            {
                Finish();
                Load();
            };

            ad.OnAdFullScreenContentFailed += adError =>
            {
                rewardEarned = false;
                Finish();
                Load();
            };
        });
    }

    public bool ShowRewarded(Action onRewarded, Action onFailed = null)
    {
        if (IsShowing)
        {
            onFailed?.Invoke();
            return false;
        }

        if (!IsReady)
        {
            ToastMessage.Show(notReadyMessage);

            CancelInvoke(nameof(Load));
            retryDelay = 2f;
            Load();

            onFailed?.Invoke();
            return false;
        }

        pendingReward = onRewarded;
        pendingFail = onFailed;
        rewardEarned = false;
        IsShowing = true;

        AudioListener.pause = true;
        rewardedAd.Show(reward => rewardEarned = true);
        return true;
    }

    private void Finish()
    {
        AudioListener.pause = false;
        IsShowing = false;

        Action reward = pendingReward;
        Action fail = pendingFail;
        pendingReward = null;
        pendingFail = null;

        if (rewardEarned) reward?.Invoke();
        else fail?.Invoke();

        rewardEarned = false;
    }
}