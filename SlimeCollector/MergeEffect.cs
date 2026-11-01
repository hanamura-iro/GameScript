using System.Collections.Generic;
using UnityEngine;

public class MergeEffect : MonoBehaviour
{
    // 전체 크기 조절 (슬라임 크기에 맞춰 이 값만 바꾸면 됨)
    private const float BaseScale = 1f;

    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite squareSprite;
    private static Sprite sparkleSprite;
    private static Sprite raySprite;

    private static readonly Color[] Palette =
    {
        new Color32(255, 140, 190, 255), // 분홍
        new Color32(255, 210, 90, 255),  // 노랑
        new Color32(140, 235, 170, 255), // 민트
        new Color32(120, 205, 255, 255), // 하늘
        new Color32(195, 155, 255, 255), // 라벤더
        new Color32(255, 255, 255, 255), // 흰색
    };

    private enum Kind { Burst, Sparkle, Riser }

    private class Piece
    {
        public Kind kind;
        public Transform t;
        public SpriteRenderer sr;
        public Color color;
        public Vector3 origin;
        public Vector2 velocity;
        public float delay;
        public float life;
        public float maxLife;
        public float size;
        public float phase;
    }

    private class Ray
    {
        public Transform t;
        public SpriteRenderer sr;
        public Color color;
        public float length;
    }

    private readonly List<Piece> pieces = new List<Piece>();
    private readonly List<Ray> rays = new List<Ray>();

    private SpriteRenderer flash;
    private SpriteRenderer glow;
    private SpriteRenderer ring1;
    private SpriteRenderer ring2;
    private Transform rayHolder;

    private Color mainColor;
    private bool big;
    private string layer;
    private int order;
    private float scale;
    private float timer;
    private float totalDuration;

    private const float RayDuration = 0.55f;
    private const float Gravity = -12f;
    private const float Damping = 0.92f;
    public static void Play(Vector3 position, Color color, bool big, string sortingLayer, int sortingOrder)
    {
        CreateSprites();

        GameObject go = new GameObject("MergeEffect");
        go.transform.position = position;
        go.AddComponent<MergeEffect>().Init(color, big, sortingLayer, sortingOrder);
    }

    private void Init(Color color, bool isBig, string sortingLayer, int sortingOrder)
    {
        mainColor = color;
        big = isBig;
        layer = sortingLayer;
        order = sortingOrder;
        scale = BaseScale * (big ? 1.4f : 1f);

        glow = CreateRenderer("Glow", circleSprite, mainColor, order, transform);
        CreateRays();
        ring1 = CreateRenderer("Ring1", ringSprite, mainColor, order + 2, transform);
        ring2 = CreateRenderer("Ring2", ringSprite, Color.white, order + 2, transform);
        flash = CreateRenderer("Flash", circleSprite, Color.white, order + 5, transform);

        CreateBurst();
        CreateSparkles();
        CreateRisers();

        totalDuration = RayDuration;
        foreach (Piece p in pieces)
        {
            totalDuration = Mathf.Max(totalDuration, p.delay + p.maxLife);
        }

        Update();
    }

    private void CreateRays()
    {
        rayHolder = new GameObject("Rays").transform;
        rayHolder.SetParent(transform, false);

        int count = big ? 12 : 8;
        for (int i = 0; i < count; i++)
        {
            Color c = (i % 2 == 0) ? mainColor : Color.white;
            SpriteRenderer sr = CreateRenderer("Ray", raySprite, c, order + 1, rayHolder);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, 360f / count * i);

            rays.Add(new Ray
            {
                t = sr.transform,
                sr = sr,
                color = c,
                length = (i % 2 == 0 ? 1.7f : 1.0f) * scale
            });
        }
    }

    private void CreateBurst()
    {
        int count = big ? 28 : 16;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-10f, 10f);
            float rad = angle * Mathf.Deg2Rad;
            float speed = Random.Range(3.5f, 7f) * scale;

            Color c = (big && i % 3 == 0) ? mainColor : Palette[Random.Range(0, Palette.Length)];

            AddPiece(Kind.Burst, squareSprite, c, order + 3, Vector3.zero,
                new Vector2(Mathf.Cos(rad), Mathf.Sin(rad) + 0.4f) * speed,
                0f, Random.Range(0.5f, 0.85f), Random.Range(0.12f, 0.24f) * scale,
                Random.Range(-720f, 720f));
        }
    }

    private void CreateSparkles()
    {
        int count = big ? 12 : 7;
        float radius = 1.5f * scale;
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = Random.insideUnitCircle.normalized * Random.Range(0.4f, radius);
            Color c = (i % 2 == 0) ? Color.white : Palette[Random.Range(0, Palette.Length)];

            AddPiece(Kind.Sparkle, sparkleSprite, c, order + 4, pos, Vector2.zero,
                Random.Range(0.05f, 0.45f), Random.Range(0.35f, 0.55f), Random.Range(0.35f, 0.6f) * scale,
                Random.Range(-180f, 180f));
        }
    }

    private void CreateRisers()
    {
        int count = big ? 14 : 8;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-0.8f, 0.8f) * scale, Random.Range(-0.3f, 0.3f), 0f);
            Color c = Palette[Random.Range(0, Palette.Length)];

            AddPiece(Kind.Riser, squareSprite, c, order + 3, pos,
                new Vector2(0f, Random.Range(1f, 2f) * scale),
                Random.Range(0.1f, 0.5f), Random.Range(0.8f, 1.2f), Random.Range(0.08f, 0.14f) * scale,
                Random.Range(0f, Mathf.PI * 2f));
        }
    }

    private void AddPiece(Kind kind, Sprite sprite, Color c, int sortingOrder, Vector3 origin,
        Vector2 velocity, float delay, float maxLife, float size, float phase)
    {
        SpriteRenderer sr = CreateRenderer(kind.ToString(), sprite, c, sortingOrder, transform);
        sr.transform.localPosition = origin;
        sr.transform.localScale = Vector3.one * size;
        sr.enabled = delay <= 0f;

        pieces.Add(new Piece
        {
            kind = kind,
            t = sr.transform,
            sr = sr,
            color = c,
            origin = origin,
            velocity = velocity,
            delay = delay,
            maxLife = maxLife,
            size = size,
            phase = phase
        });
    }

    private void Update()
    {
        timer += Time.deltaTime;

        UpdateCircle(ref flash, Color.white, 0f, 0.12f, 0.5f * 1.6f * scale, 1.3f * 1.6f * scale, 1f);
        UpdateCircle(ref glow, mainColor, 0f, 0.45f, 0.8f * 2.4f * scale, 1.5f * 2.4f * scale, 0.5f);
        UpdateCircle(ref ring1, mainColor, 0f, 0.4f, 0.3f * 2.4f * scale, 2.4f * scale, 1f);
        UpdateCircle(ref ring2, Color.white, 0.1f, 0.45f, 0.3f * 2.8f * scale, 2.8f * scale, 1f);

        UpdateRays();
        UpdatePieces();

        if (timer >= totalDuration)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateCircle(ref SpriteRenderer r, Color c, float delay, float duration, float from, float to, float maxAlpha)
    {
        if (r == null) return;

        float local = timer - delay;
        if (local < 0f)
        {
            r.enabled = false;
            return;
        }

        r.enabled = true;
        float t = Mathf.Clamp01(local / duration);
        float ease = 1f - (1f - t) * (1f - t);

        r.transform.localScale = Vector3.one * Mathf.Lerp(from, to, ease);
        c.a = (1f - t) * maxAlpha;
        r.color = c;

        if (t >= 1f)
        {
            Destroy(r.gameObject);
            r = null;
        }
    }

    private void UpdateRays()
    {
        if (rayHolder == null) return;

        float t = Mathf.Clamp01(timer / RayDuration);
        if (t >= 1f)
        {
            Destroy(rayHolder.gameObject);
            rayHolder = null;
            return;
        }

        rayHolder.Rotate(0f, 0f, 70f * Time.deltaTime);

        float ease = 1f - (1f - t) * (1f - t) * (1f - t);
        float thickness = (big ? 0.14f : 0.1f) * scale * (1f - t * 0.6f);

        foreach (Ray ray in rays)
        {
            ray.t.localScale = new Vector3(ray.length * ease, thickness, 1f);
            Color c = ray.color;
            c.a = 1f - t;
            ray.sr.color = c;
        }
    }

    private void UpdatePieces()
    {
        float damp = Mathf.Pow(Damping, Time.deltaTime * 60f);

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            Piece p = pieces[i];
            float local = timer - p.delay;
            if (local < 0f) continue;

            p.sr.enabled = true;
            float t = local / p.maxLife;

            if (t >= 1f)
            {
                Destroy(p.t.gameObject);
                pieces.RemoveAt(i);
                continue;
            }

            Color c = p.color;

            switch (p.kind)
            {
                case Kind.Burst:
                    p.velocity.x *= damp;
                    p.velocity.y = p.velocity.y * damp + Gravity * Time.deltaTime;
                    p.t.localPosition += (Vector3)(p.velocity * Time.deltaTime);
                    p.t.Rotate(0f, 0f, p.phase * Time.deltaTime);
                    p.t.localScale = Vector3.one * p.size * (1f - t * 0.6f);
                    c.a = 1f - t * t;
                    break;

                case Kind.Sparkle:
                    p.t.localScale = Vector3.one * p.size * Mathf.Sin(t * Mathf.PI);
                    p.t.localRotation = Quaternion.Euler(0f, 0f, p.phase * t);
                    c.a = 1f;
                    break;

                case Kind.Riser:
                    float sway = Mathf.Sin(local * 6f + p.phase) * 0.12f * scale;
                    p.t.localPosition = p.origin + new Vector3(sway, p.velocity.y * local, 0f);
                    float fade = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
                    float twinkle = 0.6f + 0.4f * Mathf.Sin(local * 20f + p.phase);
                    c.a = fade * twinkle;
                    break;
            }

            p.sr.color = c;
        }
    }

    private SpriteRenderer CreateRenderer(string name, Sprite sprite, Color c, int sortingOrder, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = c;
        sr.sortingLayerName = layer;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    private static void CreateSprites()
    {
        if (circleSprite != null) return;

        Vector2 center = new Vector2(0.5f, 0.5f);

        circleSprite = MakeSprite(16, (dx, dy, r) => Mathf.Sqrt(dx * dx + dy * dy) < r, center);
        ringSprite = MakeSprite(24, (dx, dy, r) =>
        {
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return d < r && d >= r - 2f;
        }, center);
        squareSprite = MakeSprite(2, (dx, dy, r) => true, center);

        sparkleSprite = MakeSprite(7, (dx, dy, r) =>
        {
            float ax = Mathf.Abs(dx);
            float ay = Mathf.Abs(dy);
            bool cross = (ax < 0.5f || ay < 0.5f) && ax + ay <= 3f;
            bool core = ax <= 1f && ay <= 1f && ax + ay <= 1f;
            return cross || core;
        }, center);

        raySprite = MakeSprite(1, (dx, dy, r) => true, new Vector2(0f, 0.5f));
    }

    private static Sprite MakeSprite(int size, System.Func<float, float, float, bool> fill, Vector2 pivot)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = fill(x - center, y - center, radius)
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), pivot, size);
    }
}