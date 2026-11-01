using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 아래쪽에 잠깐 떴다 사라지는 안내 문구
public class ToastMessage : MonoBehaviour
{
    public static ToastMessage instance;

    [Header("연결")]
    public RectTransform canvasRect;      //Canvas
    public TMP_FontAsset font;            //한글 폰트

    [Header("표시")]
    public float showTime = 2f;
    public float bottomOffset = 360f;     //화면 아래에서 얼마나 위에 띄울지

    private CanvasGroup group;
    private TextMeshProUGUI text;
    private float timer;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (canvasRect != null) Build();
    }

    public static void Show(string message)
    {
        if (instance == null || instance.text == null)
        {
            Debug.Log(message);
            return;
        }

        instance.text.text = message;
        instance.timer = instance.showTime;
        instance.group.transform.SetAsLastSibling();
    }

    private void Update()
    {
        if (group == null) return;

        if (timer > 0f) timer -= Time.unscaledDeltaTime;

        group.alpha = timer > 0f ? Mathf.Clamp01(timer * 3f) : 0f;
    }

    private void Build()
    {
        GameObject rootGo = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        rootGo.transform.SetParent(canvasRect, false);

        RectTransform rt = rootGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, bottomOffset);
        rt.sizeDelta = new Vector2(860f, 110f);

        Image bg = rootGo.GetComponent<Image>();
        bg.color = new Color32(0, 0, 0, 170);
        bg.raycastTarget = false;

        group = rootGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rt, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        text = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = 38f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}