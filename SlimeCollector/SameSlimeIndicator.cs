using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SameSlimeIndicator : MonoBehaviour
{
    [Header("연결")]
    public RectTransform canvasRect;       //화살표가 생성될 Canvas
    public TMP_FontAsset font;             //숫자 폰트 (비우면 TMP 기본 폰트)

    [Header("화면 밖 화살표")]
    public Color arrowColor = new Color32(255, 230, 90, 255);
    public float arrowSize = 90f;
    public float edgeMargin = 70f;
    public float countFontSize = 44f;
    public float bobAmount = 10f;

    [Header("화면 안 같은 슬라임 강조 링")]
    public Color ringColor = new Color32(255, 230, 90, 255);
    public float ringScale = 1.5f;

    private class Indicator
    {
        public RectTransform root;
        public RectTransform arrow;
        public TextMeshProUGUI count;
    }

    private static Sprite arrowSprite;
    private static Sprite ringSprite;

    private Camera cam;
    private Camera uiCam;
    private Canvas canvas;
    private Indicator left;
    private Indicator right;
    private readonly List<SpriteRenderer> rings = new List<SpriteRenderer>();

    private void Start()
    {
        cam = Camera.main;
        canvas = canvasRect.GetComponentInParent<Canvas>();
        uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        CreateSprites();
        left = CreateIndicator("SameSlimeLeft", -1);
        right = CreateIndicator("SameSlimeRight", 1);
    }

    private void Update()
    {
        SlimeBase held = FindHeldSlime();
        if (held == null)
        {
            left.root.gameObject.SetActive(false);
            right.root.gameObject.SetActive(false);
            HideRings(0);
            return;
        }

        float camX = cam.transform.position.x;

        int leftCount = 0;
        int rightCount = 0;
        float leftNearest = float.MaxValue;
        float rightNearest = float.MaxValue;
        float leftY = 0f;
        float rightY = 0f;
        int ringIndex = 0;

        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go == null || go == held.gameObject) continue;

            SlimeBase other = go.GetComponent<SlimeBase>();
            if (other == null || other.Slimetype != held.Slimetype) continue;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            Vector3 center = sr != null ? sr.bounds.center : go.transform.position;
            Vector3 screen = cam.WorldToScreenPoint(center);
            float dist = Mathf.Abs(center.x - camX);

            if (screen.x < 0f)
            {
                leftCount++;
                if (dist < leftNearest)
                {
                    leftNearest = dist;
                    leftY = screen.y;
                }
            }
            else if (screen.x > Screen.width)
            {
                rightCount++;
                if (dist < rightNearest)
                {
                    rightNearest = dist;
                    rightY = screen.y;
                }
            }
            else
            {
                ShowRing(ringIndex++, sr, center);
            }
        }

        HideRings(ringIndex);
        UpdateIndicator(left, leftCount, leftY, -1);
        UpdateIndicator(right, rightCount, rightY, 1);
    }

    private SlimeBase FindHeldSlime()
    {
        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go == null) continue;

            SlimeMove move = go.GetComponent<SlimeMove>();
            if (move != null && move.isHeld)
            {
                return go.GetComponent<SlimeBase>();
            }
        }
        return null;
    }

    private void UpdateIndicator(Indicator ind, int count, float screenY, int dir)
    {
        if (count == 0)
        {
            ind.root.gameObject.SetActive(false);
            return;
        }

        ind.root.gameObject.SetActive(true);

        float marginPx = edgeMargin * canvas.scaleFactor;
        float x = dir < 0 ? marginPx : Screen.width - marginPx;
        float y = Mathf.Clamp(screenY, Screen.height * 0.15f, Screen.height * 0.85f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(x, y), uiCam, out Vector2 local);
        ind.root.anchoredPosition = local;

        float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) * bobAmount;
        ind.arrow.anchoredPosition = new Vector2(dir * bob, 0f);

        ind.count.text = count.ToString();
    }

    private void ShowRing(int index, SpriteRenderer target, Vector3 center)
    {
        while (rings.Count <= index)
        {
            GameObject go = new GameObject("SameSlimeRing");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite;
            rings.Add(sr);
        }

        SpriteRenderer ring = rings[index];
        ring.gameObject.SetActive(true);

        float size = 1f;
        if (target != null)
        {
            size = Mathf.Max(target.bounds.size.x, target.bounds.size.y);
            ring.sortingLayerName = target.sortingLayerName;
            ring.sortingOrder = target.sortingOrder - 1;   // 슬라임 뒤에 깔림
        }

        float t = Time.unscaledTime;
        float pulse = 1f + Mathf.Sin(t * 6f) * 0.08f;
        ring.transform.position = center;
        ring.transform.localScale = Vector3.one * size * ringScale * pulse;

        Color c = ringColor;
        c.a = 0.6f + 0.4f * Mathf.Sin(t * 6f);
        ring.color = c;
    }

    private void HideRings(int fromIndex)
    {
        for (int i = fromIndex; i < rings.Count; i++)
        {
            if (rings[i].gameObject.activeSelf) rings[i].gameObject.SetActive(false);
        }
    }

    private Indicator CreateIndicator(string name, int dir)
    {
        GameObject rootGo = new GameObject(name, typeof(RectTransform));
        rootGo.transform.SetParent(canvasRect, false);

        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(arrowSize, arrowSize);

        GameObject arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGo.transform.SetParent(root, false);

        RectTransform arrow = arrowGo.GetComponent<RectTransform>();
        arrow.sizeDelta = new Vector2(arrowSize, arrowSize);
        arrow.localScale = new Vector3(dir, 1f, 1f);

        Image arrowImage = arrowGo.GetComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.color = arrowColor;
        arrowImage.raycastTarget = false;

        GameObject countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGo.transform.SetParent(root, false);

        RectTransform countRect = countGo.GetComponent<RectTransform>();
        countRect.sizeDelta = new Vector2(arrowSize, arrowSize);
        countRect.anchoredPosition = new Vector2(-dir * arrowSize * 0.85f, 0f);

        TextMeshProUGUI count = countGo.GetComponent<TextMeshProUGUI>();
        if (font != null) count.font = font;
        count.fontSize = countFontSize;
        count.fontStyle = FontStyles.Bold;
        count.alignment = TextAlignmentOptions.Center;
        count.color = arrowColor;
        count.raycastTarget = false;

        rootGo.SetActive(false);

        return new Indicator { root = root, arrow = arrow, count = count };
    }

    private static void CreateSprites()
    {
        if (arrowSprite != null) return;

        arrowSprite = MakeSprite(9, (x, y, size) =>
        {
            float half = (size - 1 - x) / 2f;
            return Mathf.Abs(y - (size - 1) / 2f) <= half;
        });

        ringSprite = MakeSprite(24, (x, y, size) =>
        {
            float c = (size - 1) / 2f;
            float dx = x - c;
            float dy = y - c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float r = size / 2f;
            return d < r && d >= r - 2f;
        });
    }

    private static Sprite MakeSprite(int size, System.Func<int, int, int, bool> fill)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = fill(x, y, size)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}