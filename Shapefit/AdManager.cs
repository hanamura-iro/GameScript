using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    //출시 전 실제 ID로 교체할 것
    private const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    private const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Header("실제 배포용 광고 단위 ID,비워둘 시 위 ID 사용")]
    public string interstitialAdUnitId = "AD ID";
    public string rewardedAdUnitId = "AD ID";

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private string InterstitialId => string.IsNullOrEmpty(interstitialAdUnitId) ? TestInterstitialId : interstitialAdUnitId;
    private string RewardedId => string.IsNullOrEmpty(rewardedAdUnitId) ? TestRewardedId : rewardedAdUnitId;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MobileAds.Initialize(_ =>
        {
            Debug.Log("[Ads] Google Mobile Ads 초기화");
            LoadInterstitial();
            LoadRewarded();
        });
    }

    //전면광고

    private void LoadInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        var request = new AdRequest();
        InterstitialAd.Load(InterstitialId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[애드몹]전면광고 로드 실패: " + error);
                return;
            }
            interstitialAd = ad;
            interstitialAd.OnAdFullScreenContentClosed += LoadInterstitial;
            interstitialAd.OnAdFullScreenContentFailed += _ => LoadInterstitial();
        });
    }

    //스테이지 클리어 등 자연스러운 타이밍에 호출, 준비 안 됐으면 무시, 다음을 위해 다시 로드.
    //onClosed를 넘기면 광고를 닫았을 때 호출
    public void ShowInterstitial(Action onClosed = null)
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            var adInstance = interstitialAd; // 필드가 아니라 이 인스턴스를 직접 참조 
                                             // 거의 동시에 필드를 null로 바꿔도 구독 해제/콜백이 안전하게 동작
            if (onClosed != null)
            {
                void Handler()
                {
                    adInstance.OnAdFullScreenContentClosed -= Handler;
                    onClosed.Invoke();
                }
                adInstance.OnAdFullScreenContentClosed += Handler;
            }
            adInstance.Show();
        }
        else
        {
            Debug.Log("[애드몹] 전면광고 아직 준비 안 됨, 재로드");
            LoadInterstitial();
        }
    }

    //보상형광고

    private void LoadRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var request = new AdRequest();
        RewardedAd.Load(RewardedId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("[애드몹] 보상형광고 로드 실패: " + error);
                return;
            }
            rewardedAd = ad;
        });
    }

    ///보상형광고 표시. 끝까지 시청해서 보상을 받으면 onReward 콜백 호출
    ///광고가 아직 준비 안 됐으면 onUnavailable 호출
    public void ShowRewarded(Action onReward, Action onUnavailable = null)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            var adInstance = rewardedAd; //ShowInterstitial과 동일하게 이 인스턴스를 직접 참조
                                         //LoadRewarded가 필드를 바꿔도 구독 해제가 안전하게 동작
            void Handler()
            {
                adInstance.OnAdFullScreenContentClosed -= Handler;
                LoadRewarded();
            }
            adInstance.OnAdFullScreenContentClosed += Handler;
            adInstance.Show(_ => onReward?.Invoke());
        }
        else
        {
            Debug.Log("[애드몹] 보상형광고 준비 안 됨");
            onUnavailable?.Invoke();
            LoadRewarded();
        }
    }

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();
}
