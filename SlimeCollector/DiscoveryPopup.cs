using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DiscoveryPopup : MonoBehaviour, IPointerClickHandler
{
    public static DiscoveryPopup instance;
    public static bool IsOpen => instance != null && instance.isOpen;

    [Header("UI 연결")]
    public RectTransform panel;            // 튀어나오는 카드
    public Image slimeImage;
    public Image raysImage;                // 회전하는 빛살 (선택, 스프라이트 비워두면 자동 생성)
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI tapText;        // 화면을 터치하세요 (선택)

    [Header("연출")]
    public string title = "새 슬라임 발견!";
    public float showDelay = 0.15f;        // 합치기 이펙트를 먼저 보여주고 뜨도록
    public float minShowTime = 0.5f;       // 실수로 바로 닫히지 않게
    public float raySpeed = 40f;
    public float bobHeight = 8f;

    private CanvasGroup root;
    private readonly Queue<int> queue = new Queue<int>();
    private Coroutine routine;
    private bool isOpen;
    private bool canClose;
    private Vector2 slimeBasePos;

    private void Awake()
    {
        instance = this;
        root = GetComponent<CanvasGroup>();

        if (slimeImage != null)
        {
            slimeImage.preserveAspect = true;
            slimeBasePos = slimeImage.rectTransform.anchoredPosition;
        }

        if (raysImage != null && raysImage.sprite == null)
        {
            raysImage.sprite = MakeRaySprite(64, 12);
        }

        Hide();
    }

    public void Show(int index)
    {
        queue.Enqueue(index);
        if (routine == null)
        {
            routine = StartCoroutine(ProcessQueue());
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (isOpen && canClose)
        {
            isOpen = false;
        }
    }

    private void Update()
    {
        if (root.alpha <= 0f) return;

        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        if (raysImage != null)
        {
            raysImage.rectTransform.Rotate(0f, 0f, -raySpeed * dt);
        }

        if (slimeImage != null)
        {
            float bob = Mathf.Sin(time * 3f) * bobHeight;
            slimeImage.rectTransform.anchoredPosition = slimeBasePos + new Vector2(0f, bob);
        }

        if (tapText != null)
        {
            tapText.alpha = canClose ? 0.5f + 0.5f * Mathf.Sin(time * 4f) : 0f;
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();

            yield return new WaitForSecondsRealtime(showDelay);

            Fill(index);

            isOpen = true;
            canClose = false;
            root.blocksRaycasts = true;
            root.interactable = true;

            float t = 0f;
            const float inDuration = 0.35f;
            while (t < inDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / inDuration);
                root.alpha = Mathf.Clamp01(t / 0.2f);
                if (panel != null) panel.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }

            root.alpha = 1f;
            if (panel != null) panel.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(minShowTime);
            canClose = true;

            while (isOpen)
            {
                yield return null;
            }

            t = 0f;
            const float outDuration = 0.15f;
            while (t < outDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / outDuration);
                root.alpha = 1f - k;
                if (panel != null) panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
                yield return null;
            }

            Hide();
        }

        routine = null;
    }

    private void Fill(int index)
    {
        GameObject[] slimes = GameManager.instance.Slimes;
        BookManager book = BookManager.instance;

        if (slimeImage != null && index >= 0 && index < slimes.Length)
        {
            SpriteRenderer sr = slimes[index].GetComponent<SpriteRenderer>();
            slimeImage.sprite = sr != null ? sr.sprite : null;
        }

        if (titleText != null) titleText.text = title;
        if (nameText != null) nameText.text = index < book.slimeNames.Length ? book.slimeNames[index] : "";
        if (descText != null) descText.text = index < book.slimeDescriptions.Length ? book.slimeDescriptions[index] : "";
    }

    private void Hide()
    {
        isOpen = false;
        canClose = false;
        root.alpha = 0f;
        root.blocksRaycasts = false;
        root.interactable = false;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    private static Sprite MakeRaySprite(int size, int rayCount)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) / 2f;
        float radius = size / 2f;
        float wedge = Mathf.PI * 2f / (rayCount * 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float angle = Mathf.Atan2(dy, dx) + Mathf.PI;
                bool onRay = Mathf.FloorToInt(angle / wedge) % 2 == 0;

                byte alpha = 0;
                if (onRay && dist < radius)
                {
                    float fade = 1f - dist / radius;
                    fade = Mathf.Ceil(fade * 4f) / 4f;
                    alpha = (byte)(fade * 255f);
                }

                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}