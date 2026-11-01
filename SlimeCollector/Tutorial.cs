using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    private const string DoneKey = "TutorialDone";

    [Header("연결")]
    public RectTransform canvasRect;      //Canvas
    public RectTransform buyButton;       //구매 버튼
    public TMP_FontAsset font;            //한글 폰트
    public Sprite handSprite;             //비우면 도트 손가락 자동 생성 (손끝이 피벗이어야 함)

    [Header("표시")]
    public float handSize = 130f;
    public float messageFontSize = 44f;
    public float messageY = -360f;        //화면 위쪽 기준 메시지 위치

    private enum Step { None, Buy, Touch, BuyMore, Merge }

    private Camera cam;
    private Camera uiCam;
    private CanvasGroup group;
    private RectTransform root;
    private RectTransform hand;
    private RectTransform ripple;
    private Image handImage;
    private Image rippleImage;
    private TextMeshProUGUI messageText;

    private Step step = Step.None;
    private float stepTime;

    private static Sprite generatedHand;
    private static Sprite generatedRing;

    private void Start()
    {
        if (PlayerPrefs.GetInt(DoneKey, 0) == 1)
        {
            enabled = false;
            return;
        }

        cam = Camera.main;
        Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
        uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        BuildUI();
    }

    private void Update()
    {
        if (BookManager.instance.IsDiscovered(1))
        {
            Finish();
            return;
        }

        Step next = DecideStep(out Vector2 from, out Vector2 to);
        if (next != step)
        {
            step = next;
            stepTime = 0f;
            messageText.text = GetMessage(step);
        }

        stepTime += Time.unscaledDeltaTime;

        bool hidden = step == Step.None || DiscoveryPopup.IsOpen || OfflineReward.IsOpen || AnySlimeHeld();
        group.alpha = Mathf.MoveTowards(group.alpha, hidden ? 0f : 1f, Time.unscaledDeltaTime * 4f);

        if (step == Step.Merge) AnimateDrag(from, to);
        else AnimateTap(from);
    }

    private Step DecideStep(out Vector2 from, out Vector2 to)
    {
        from = Vector2.zero;
        to = Vector2.zero;

        int count = 0;
        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go != null) count++;
        }

        if (count == 0)
        {
            from = ButtonScreenPos();
            return Step.Buy;
        }

        if (FindPair(out SlimeBase a, out SlimeBase b))
        {
            from = SlimeScreenPos(a);
            to = SlimeScreenPos(b);
            to.x = Mathf.Clamp(to.x, Screen.width * 0.1f, Screen.width * 0.9f);
            return Step.Merge;
        }

        if (SlimeEconomy.instance != null && SlimeEconomy.instance.CanBuy)
        {
            from = ButtonScreenPos();
            return Step.BuyMore;
        }

        from = new Vector2(Screen.width * 0.5f, Screen.height * 0.6f);
        return Step.Touch;
    }

    private static string GetMessage(Step s)
    {
        switch (s)
        {
            case Step.Buy: return "구매 버튼을 눌러 슬라임을 데려와요";
            case Step.Touch: return "화면을 터치해서 골드를 모아요";
            case Step.BuyMore: return "골드가 모였어요! 한 마리 더 데려와요";
            case Step.Merge: return "같은 슬라임을 끌어서 겹쳐보세요";
            default: return "";
        }
    }

    private void AnimateTap(Vector2 screenPos)
    {
        float t = stepTime % 1f;
        float press = t < 0.12f ? t / 0.12f : (t < 0.3f ? 1f - (t - 0.12f) / 0.18f : 0f);

        Vector2 local = ToLocal(screenPos);
        hand.anchoredPosition = local + new Vector2(0f, -12f * press);
        hand.localScale = Vector3.one * (1f - 0.1f * press);
        SetAlpha(handImage, 1f);

        float r = Mathf.Clamp01((t - 0.1f) / 0.5f);
        ripple.gameObject.SetActive(t > 0.1f && t < 0.6f);
        ripple.anchoredPosition = local;
        ripple.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, r);
        SetAlpha(rippleImage, 1f - r);
    }

    private void AnimateDrag(Vector2 fromScreen, Vector2 toScreen)
    {
        ripple.gameObject.SetActive(false);

        Vector2 from = ToLocal(fromScreen);
        Vector2 to = ToLocal(toScreen);

        const float cycle = 1.8f;
        float t = stepTime % cycle;

        Vector2 pos;
        float scale;
        float alpha = 1f;

        if (t < 0.25f)
        {
            pos = from;
            scale = Mathf.Lerp(1f, 0.9f, t / 0.25f);
        }
        else if (t < 1.15f)
        {
            float k = (t - 0.25f) / 0.9f;
            float ease = k * k * (3f - 2f * k);
            pos = Vector2.Lerp(from, to, ease);
            scale = 0.9f;
        }
        else if (t < 1.45f)
        {
            pos = to;
            scale = Mathf.Lerp(0.9f, 1f, (t - 1.15f) / 0.3f);
        }
        else
        {
            pos = to;
            scale = 1f;
            alpha = 1f - (t - 1.45f) / 0.35f;
        }

        hand.anchoredPosition = pos;
        hand.localScale = Vector3.one * scale;
        SetAlpha(handImage, alpha);
    }

    private bool FindPair(out SlimeBase a, out SlimeBase b)
    {
        a = null;
        b = null;

        var slimes = GameManager.instance.SlimeData;
        float camX = cam.transform.position.x;
        int bestType = int.MaxValue;
        float bestDist = float.MaxValue;

        foreach (GameObject go in slimes)
        {
            if (go == null) continue;
            SlimeBase s = go.GetComponent<SlimeBase>();
            if (s == null) continue;

            int type = (int)s.Slimetype;
            if (type > bestType) continue;

            SlimeBase partner = FindNearestSame(s);
            if (partner == null) continue;

            float dist = Mathf.Abs(go.transform.position.x - camX);
            if (type < bestType || dist < bestDist)
            {
                bestType = type;
                bestDist = dist;
                a = s;
                b = partner;
            }
        }

        return a != null;
    }

    private SlimeBase FindNearestSame(SlimeBase from)
    {
        SlimeBase best = null;
        float bestDist = float.MaxValue;

        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go == null || go == from.gameObject) continue;
            SlimeBase s = go.GetComponent<SlimeBase>();
            if (s == null || s.Slimetype != from.Slimetype) continue;

            float dist = Mathf.Abs(go.transform.position.x - from.transform.position.x);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = s;
            }
        }

        return best;
    }

    private static bool AnySlimeHeld()
    {
        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go == null) continue;
            SlimeMove move = go.GetComponent<SlimeMove>();
            if (move != null && move.isHeld) return true;
        }
        return false;
    }

    private Vector2 ButtonScreenPos()
    {
        if (buyButton == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);
        return RectTransformUtility.WorldToScreenPoint(uiCam, buyButton.position);
    }

    private Vector2 SlimeScreenPos(SlimeBase slime)
    {
        SpriteRenderer sr = slime.GetComponent<SpriteRenderer>();
        Vector3 world = sr != null ? sr.bounds.center : slime.transform.position;
        return cam.WorldToScreenPoint(world);
    }

    private Vector2 ToLocal(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCam, out Vector2 local);
        return local;
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void Finish()
    {
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();

        if (root != null) Destroy(root.gameObject);
        enabled = false;
    }

    [ContextMenu("튜토리얼 다시 보기")]
    private void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(DoneKey);
        PlayerPrefs.Save();
        Debug.Log("튜토리얼을 초기화");
    }

    private void BuildUI()
    {
        CreateSprites();

        GameObject rootGo = new GameObject("Tutorial", typeof(RectTransform), typeof(CanvasGroup));
        rootGo.transform.SetParent(canvasRect, false);
        root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        group = rootGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // 물결
        ripple = CreateImage("Ripple", generatedRing, new Vector2(handSize, handSize), out rippleImage);
        rippleImage.color = new Color32(255, 255, 255, 255);

        // 손가락
        Sprite sprite = handSprite != null ? handSprite : generatedHand;
        float ratio = sprite.rect.height / sprite.rect.width;
        hand = CreateImage("Hand", sprite, new Vector2(handSize, handSize * ratio), out handImage);
        handImage.preserveAspect = true;

        hand.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);

        GameObject msgGo = new GameObject("Message", typeof(RectTransform), typeof(Image));
        msgGo.transform.SetParent(root, false);
        RectTransform msgRect = msgGo.GetComponent<RectTransform>();
        msgRect.anchorMin = msgRect.anchorMax = new Vector2(0.5f, 1f);
        msgRect.pivot = new Vector2(0.5f, 0.5f);
        msgRect.anchoredPosition = new Vector2(0f, messageY);
        msgRect.sizeDelta = new Vector2(900f, 130f);

        Image msgBg = msgGo.GetComponent<Image>();
        msgBg.color = new Color32(0, 0, 0, 150);
        msgBg.raycastTarget = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(msgRect, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        messageText = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) messageText.font = font;
        messageText.fontSize = messageFontSize;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
    }

    private RectTransform CreateImage(string name, Sprite sprite, Vector2 size, out Image image)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return rt;
    }

    private static void CreateSprites()
    {
        if (generatedHand == null)
        {
            string[] map =
            {
                "....##.......",
                "...#oo#......",
                "...#oo#......",
                "...#oo#......",
                "...#oo###....",
                "...#oo#oo##..",
                ".###oo#oo#oo#",
                "#oo#oooooooo#",
                "#ooooooooooo#",
                ".#oooooooooo#",
                ".#oooooooooo#",
                "..#ooooooooo#",
                "..#oooooooo#.",
                "...#oooooo#..",
                "...########..",
            };

            int w = map[0].Length;
            int h = map.Length;
            Texture2D tex = NewTexture(w, h);
            Color32[] pixels = new Color32[w * h];

            for (int row = 0; row < h; row++)
            {
                for (int x = 0; x < w; x++)
                {
                    char c = map[row][x];
                    Color32 col = c == '#' ? new Color32(20, 20, 30, 255)
                                : c == 'o' ? new Color32(255, 255, 255, 255)
                                : new Color32(0, 0, 0, 0);
                    pixels[(h - 1 - row) * w + x] = col;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            generatedHand = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(4.5f / w, 1f), w);
        }

        if (generatedRing == null)
        {
            int size = 24;
            Texture2D tex = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            float c = (size - 1) / 2f;
            float r = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = d < r && d >= r - 2f
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            generatedRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    private static Texture2D NewTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }
}