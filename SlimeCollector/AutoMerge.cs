using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoMerge : MonoBehaviour
{
    [Header("설정")]
    public float cooldown = 60f;          // 쿨타임 (초)
    public int maxMergesPerUse = 10;      // 한 번 누를 때 최대 합치는 횟수
    public float flyDuration = 0.25f;     // 짝에게 날아가는 시간
    public float flyArcHeight = 0.8f;     // 날아갈 때 포물선 높이
    public float interval = 0.12f;        // 합치기 사이 간격

    [Header("카메라 이동")]
    public bool followWithCamera = true;  // 화면 밖 짝을 합칠 때 카메라가 따라감
    public float visibleMargin = 1f;      // 화면 가장자리에서 이만큼 안쪽에 있어야 "보인다"고 판단
    public float cameraWaitTimeout = 1.2f;// 카메라 도착을 기다리는 최대 시간
    public float showDelay = 0.1f;        // 도착 후 날아가기 전 잠깐 보여주는 시간
    public bool returnCameraAfter = true; // 끝나면 원래 보던 위치로 복귀

    [Header("UI 연결")]
    public Button button;
    public Image cooldownFill;            // Image Type을 Filled로 설정한 이미지
    public TextMeshProUGUI label;
    public string readyText = "모으기";
    public Button adResetButton;          // 광고 보고 쿨타임 초기화 (선택, 쿨타임 중에만 보임)

    private const float PairCheckInterval = 0.3f;

    private float cooldownTimer;
    private bool running;
    private bool cameraFocused;

    private float pairCheckTimer;
    private bool cachedHasPair;
    private int shownLabelKey = int.MinValue;
    private float shownFill = -1f;

    private readonly List<SlimeBase> candidates = new List<SlimeBase>();

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(Use);
        if (adResetButton != null) adResetButton.onClick.AddListener(ResetCooldownWithAd);
    }

    private void OnDisable()
    {
        if (cameraFocused && CameraController.instance != null)
        {
            CameraController.instance.ClearFocus();
            cameraFocused = false;
        }
        running = false;
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.unscaledDeltaTime;

        bool ready = !running && cooldownTimer <= 0f;

        pairCheckTimer -= Time.unscaledDeltaTime;
        if (pairCheckTimer <= 0f)
        {
            pairCheckTimer = PairCheckInterval;
            cachedHasPair = ready && FindPair(out _, out _);
        }

        if (button != null) button.interactable = ready && cachedHasPair;

        float fill = cooldownTimer > 0f ? cooldownTimer / cooldown : 0f;
        if (cooldownFill != null && !Mathf.Approximately(fill, shownFill))
        {
            shownFill = fill;
            cooldownFill.fillAmount = fill;
        }

        int labelKey = cooldownTimer > 0f ? Mathf.CeilToInt(cooldownTimer) : -1;
        if (label != null && labelKey != shownLabelKey)
        {
            shownLabelKey = labelKey;
            label.text = labelKey > 0 ? $"{labelKey}초" : readyText;
        }

        if (adResetButton != null)
        {
            bool show = cooldownTimer > 0f && !running;
            if (adResetButton.gameObject.activeSelf != show) adResetButton.gameObject.SetActive(show);
            adResetButton.interactable = AdManager.instance != null && AdManager.instance.IsReady;
        }
    }

    public void Use()
    {
        if (running || cooldownTimer > 0f) return;
        if (!FindPair(out _, out _)) return;

        cooldownTimer = cooldown;
        cachedHasPair = false;
        StartCoroutine(RunRoutine());
    }

    private void ResetCooldownWithAd()
    {
        if (AdManager.instance == null || cooldownTimer <= 0f) return;

        AdManager.instance.ShowRewarded(() =>
        {
            cooldownTimer = 0f;
            pairCheckTimer = 0f;
        });
    }

    private IEnumerator RunRoutine()
    {
        running = true;
        int count = 0;

        CameraController cc = CameraController.instance;
        float startX = cc != null ? cc.CurrentX : 0f;

        while (count < maxMergesPerUse && FindPair(out SlimeBase from, out SlimeBase to))
        {
            SetControl(from, false);
            SetControl(to, false);

            yield return FocusPairRoutine(from, to);
            yield return FlyRoutine(from, to);

            if (from != null && to != null)
            {
                SlimeMerge.instance.MergePair(from, to);
                count++;
            }
            else if (from != null)
            {
                SetControl(from, true);
                SlimeMove move = from.GetComponent<SlimeMove>();
                if (move != null) move.Drop(from.transform.position);
            }
            else if (to != null)
            {
                SetControl(to, true);
            }

            yield return new WaitForSecondsRealtime(interval);
        }

        if (cameraFocused && cc != null)
        {
            if (returnCameraAfter)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                cc.FocusOn(startX);
                yield return WaitCamera(cc);
            }

            cc.ClearFocus();
            cameraFocused = false;
        }

        running = false;
        pairCheckTimer = 0f;
    }

    private IEnumerator FocusPairRoutine(SlimeBase from, SlimeBase to)
    {
        CameraController cc = CameraController.instance;
        if (!followWithCamera || cc == null || from == null || to == null) yield break;

        float fx = from.transform.position.x;
        float tx = to.transform.position.x;

        if (cc.IsVisible(fx, visibleMargin) && cc.IsVisible(tx, visibleMargin)) yield break;

        float target = Mathf.Abs(fx - tx) < cc.HalfWidth * 1.6f ? (fx + tx) * 0.5f : tx;

        cc.FocusOn(target);
        cameraFocused = true;

        yield return WaitCamera(cc);
        yield return new WaitForSecondsRealtime(showDelay);
    }

    private IEnumerator WaitCamera(CameraController cc)
    {
        float t = 0f;
        while (!cc.FocusArrived && t < cameraWaitTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FlyRoutine(SlimeBase from, SlimeBase to)
    {
        if (from == null || to == null) yield break;

        SetControl(from, false);
        SetControl(to, false);

        Vector3 start = from.transform.position;
        float t = 0f;

        while (t < flyDuration)
        {
            if (from == null || to == null) yield break;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / flyDuration);
            float ease = k * k * (3f - 2f * k);

            Vector3 end = to.transform.position;
            Vector3 pos = Vector3.Lerp(start, end, ease);
            pos.y += Mathf.Sin(k * Mathf.PI) * flyArcHeight;
            from.transform.position = pos;

            yield return null;
        }
    }

    private void SetControl(SlimeBase slime, bool on)
    {
        if (slime == null) return;

        SlimeMove move = slime.GetComponent<SlimeMove>();
        if (move != null) move.enabled = on;

        SlimeDrag drag = slime.GetComponent<SlimeDrag>();
        if (drag != null) drag.enabled = on;
    }

    private bool FindPair(out SlimeBase from, out SlimeBase to)
    {
        from = null;
        to = null;

        int lastType = GameManager.instance.Slimes.Length - 1;

        candidates.Clear();
        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            SlimeBase s = GetAvailable(go, lastType);
            if (s != null) candidates.Add(s);
        }

        int bestType = int.MaxValue;
        float bestDist = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            SlimeBase a = candidates[i];
            int type = (int)a.Slimetype;
            if (type > bestType) continue;

            float ax = a.transform.position.x;

            for (int j = i + 1; j < candidates.Count; j++)
            {
                SlimeBase b = candidates[j];
                if (b.Slimetype != a.Slimetype) continue;

                float dist = Mathf.Abs(ax - b.transform.position.x);
                if (type < bestType || dist < bestDist)
                {
                    bestType = type;
                    bestDist = dist;
                    from = a;
                    to = b;
                }
            }
        }

        return from != null;
    }
    private static SlimeBase GetAvailable(GameObject go, int lastType)
    {
        if (go == null) return null;

        SlimeBase slimeBase = go.GetComponent<SlimeBase>();
        if (slimeBase == null || (int)slimeBase.Slimetype >= lastType) return null;

        SlimeMove move = go.GetComponent<SlimeMove>();
        if (move != null && (!move.enabled || move.isHeld)) return null;

        return slimeBase;
    }
}