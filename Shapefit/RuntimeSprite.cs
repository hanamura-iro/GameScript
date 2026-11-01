using UnityEngine;

public static class RuntimeSprite
{
    private static Sprite cachedBlock;
    private static Sprite cachedPanel;
    private static Sprite cachedCircle;

    public static Sprite Circle
    {
        get
        {
            if (cachedCircle == null)
            {
                var tex = GenerateCircleTexture(24);
                cachedCircle = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 24);
            }
            return cachedCircle;
        }
    }

    private static Texture2D GenerateCircleTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float radius = size / 2f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Sprite TetrisBlock
    {
        get
        {
            if (cachedBlock == null)
            {
                var tex = GenerateBevelTexture(32);
                cachedBlock = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            }
            return cachedBlock;
        }
    }

    public static Sprite RoundedPanel
    {
        get
        {
            if (cachedPanel == null)
            {
                var tex = GenerateRoundedTexture(64, 18);
                cachedPanel = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64,
                    0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
            }
            return cachedPanel;
        }
    }

    private static Texture2D GenerateBevelTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        int border = Mathf.Max(1, size / 16);   
        int bevel = Mathf.Max(2, size / 8);     

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || y < border || x >= size - border || y >= size - border;
                float shade;

                if (isBorder)
                {
                    shade = 0.22f; 
                }
                else
                {
                    bool nearTopLeft =
                        (x < border + bevel) || (y < border + bevel);
                    bool nearBottomRight =
                        (x >= size - border - bevel) || (y >= size - border - bevel);

                    if (nearTopLeft && !nearBottomRight) shade = 1.0f;  
                    else if (nearBottomRight) shade = 0.62f;             
                    else shade = 0.86f;                                   
                }

                pixels[y * size + x] = new Color(shade, shade, shade, 1f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateRoundedTexture(int size, int cornerRadius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = RoundedAlpha(x, y, size, cornerRadius);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static float RoundedAlpha(int x, int y, int size, int r)
    {
        float px = x + 0.5f, py = y + 0.5f;
        float left = r, right = size - r, top = r, bottom = size - r;

        float dx = 0f, dy = 0f;
        if (px < left) dx = left - px;
        else if (px > right) dx = px - right;
        if (py < top) dy = top - py;
        else if (py > bottom) dy = py - bottom;

        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        return Mathf.Clamp01(r - dist + 0.5f);
    }
}
