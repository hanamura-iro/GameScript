using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool isData = false;

    [Header("슬라임들")]
    public GameObject[] Slimes;
    public Transform[] SlimeSpwanPos;

    [Header("보유 슬라임")]
    public List<GameObject> SlimeData;

    [Header("UI 연결")]
    public TextMeshProUGUI goldText;      // 현재 골드를 표시할 텍스트 UI
    public GameObject textPrefab;         // 팝업될 텍스트 프리팹 (TMP 텍스트 컴포넌트 필요)
    public Transform canvasTransform;     // 팝업 텍스트가 생성될 Canvas

    [Header("터치 골드 예외")]
    [Tooltip("터치해도 골드 지급을 막지 않는 오브젝트 (예: 화면 전체를 덮는 배경 이미지)")]
    public List<GameObject> touchAreaObjects = new List<GameObject>();

    [Header("텍스트 연출 설정")]
    public float moveSpeed = 100f;        // 위로 날아가는 속도
    public float alphaSpeed = 3f;         // 투명해지는 속도 (클수록 빨리 사라짐)

    [Header("성능")]
    public int targetFrameRate = 60;      

    [Header("현재 골드")]
    public long currentGold = 0;          // 현재 골드량

    private class FloatingPopup
    {
        public RectTransform rect;
        public CanvasGroup canvasGroup; // 텍스트,아이콘을 한 번에 투명하게 만들기 위한 그룹
    }

    private List<FloatingPopup> activePopups = new List<FloatingPopup>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private long shownGold = long.MinValue;

    private void Awake()
    {
        instance = this;
        if (!isData)
        {
            isData = true;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnScreenClick;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnScreenClick;
        EnhancedTouchSupport.Disable();
    }

    private void OnScreenClick(Finger finger)
    {
        if (DiscoveryPopup.IsOpen) return;

        Vector2 touchPos = finger.currentTouch.screenPosition;

        if (IsTouchBlocked(touchPos)) return;

        GiveTouchGold(touchPos);
    }
    public void GiveTouchGold(Vector2 screenPos)
    {
        long amount = SlimeEconomy.instance != null ? SlimeEconomy.instance.TouchGold : 0;
        if (amount <= 0) return;

        currentGold += amount;
        SpawnFloatingText(screenPos, amount);
    }

    private bool IsTouchBlocked(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (!touchAreaObjects.Contains(result.gameObject))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateGoldUI()
    {
        if (goldText == null || currentGold == shownGold) return;

        shownGold = currentGold;
        goldText.text = FormatKoreanNumber(currentGold);
    }

    private const int MaxDisplayUnits = 2;
    private static readonly string[] BigUnits = { "", "만", "억", "조", "경" };
    private static readonly List<long> chunkBuffer = new List<long>();
    private static readonly List<string> partBuffer = new List<string>();

    public static string FormatKoreanNumber(long value, bool withWon = true)
    {
        string won = withWon ? "원" : "";

        if (value == 0) return "0" + won;

        chunkBuffer.Clear();
        partBuffer.Clear();
        long remaining = value;

        while (remaining > 0)
        {
            chunkBuffer.Add(remaining % 10000);
            remaining /= 10000;
        }

        for (int i = chunkBuffer.Count - 1; i >= 0; i--)
        {
            if (partBuffer.Count >= MaxDisplayUnits) break;

            long chunkValue = chunkBuffer[i];
            if (chunkValue == 0) continue;

            if (i == 0)
            {
                long thousands = chunkValue / 1000;
                long remainder = chunkValue % 1000;

                if (thousands > 0 && partBuffer.Count < MaxDisplayUnits) partBuffer.Add($"{thousands}천");
                if (remainder > 0 && partBuffer.Count < MaxDisplayUnits) partBuffer.Add(remainder.ToString());
            }
            else
            {
                partBuffer.Add($"{chunkValue}{BigUnits[i]}");
            }
        }

        return string.Join(" ", partBuffer) + won;
    }

    private void SpawnFloatingText(Vector2 position, long amount)
    {
        if (textPrefab == null || canvasTransform == null) return;

        GameObject go = Instantiate(textPrefab, position, Quaternion.identity, canvasTransform);

        TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = "+" + FormatKoreanNumber(amount, false);
        }

        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = go.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        activePopups.Add(new FloatingPopup
        {
            rect = go.GetComponent<RectTransform>(),
            canvasGroup = canvasGroup
        });
    }

    private void Update()
    {
        UpdateGoldUI();

        if (activePopups.Count == 0) return;

        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            FloatingPopup popup = activePopups[i];

            if (popup.rect == null)
            {
                activePopups.RemoveAt(i);
                continue;
            }

            popup.rect.anchoredPosition += Vector2.up * moveSpeed * Time.deltaTime;
            popup.canvasGroup.alpha -= alphaSpeed * Time.deltaTime;

            if (popup.canvasGroup.alpha <= 0)
            {
                GameObject targetObj = popup.rect.gameObject;
                activePopups.RemoveAt(i);
                Destroy(targetObj);
            }
        }
    }
}