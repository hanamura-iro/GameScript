using System.Collections;
using UnityEngine;

public static class ImpactBurst
{
    private const int SparkCount = 8;
    private const int FastCount = 10;
    private const int SlowCount = 6;
    private const float TotalDuration = 0.75f;

    public static void Spawn(MonoBehaviour runner, Vector3 position, Color color)
    {
        runner.StartCoroutine(Routine(position, color));
    }

    private static IEnumerator Routine(Vector3 position, Color color)
    {
        var flash = FxPool.Get(position, Color.Lerp(Color.white, color, 0.25f), 8);

        var ring1 = FxPool.Get(position, new Color(color.r, color.g, color.b, 0f), 6);
        var ring2 = FxPool.Get(position, new Color(1f, 1f, 1f, 0f), 6);

        var sparks = new SpriteRenderer[SparkCount];
        var sparkDirs = new Vector2[SparkCount];
        var sparkLens = new float[SparkCount];
        var sparkColor = Color.Lerp(color, Color.white, 0.5f);
        for (int i = 0; i < SparkCount; i++)
        {
            float angle = (Mathf.PI * 2f / SparkCount) * i + Random.Range(-0.15f, 0.15f);
            sparkDirs[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            sparkLens[i] = Random.Range(1.8f, 3.2f);
            sparks[i] = FxPool.Get(position, sparkColor, 7);
            sparks[i].transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
        }

        int partCount = FastCount + SlowCount;
        var parts = new SpriteRenderer[partCount];
        var vels = new Vector3[partCount];
        var sizes = new float[partCount];
        var spins = new float[partCount];
        var baseColors = new Color[partCount];
        for (int i = 0; i < partCount; i++)
        {
            bool fast = i < FastCount;
            int n = fast ? FastCount : SlowCount;
            int k = fast ? i : i - FastCount;
            float angle = (Mathf.PI * 2f / n) * k + Random.Range(-0.3f, 0.3f);
            float speed = fast ? Random.Range(6f, 10f) : Random.Range(2f, 4.5f);
            vels[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;
            sizes[i] = fast ? Random.Range(0.14f, 0.34f) : Random.Range(0.25f, 0.55f);
            spins[i] = Random.Range(-540f, 540f);
            baseColors[i] = (i % 3 == 0) ? Color.Lerp(color, Color.white, 0.6f) : color;
            parts[i] = FxPool.Get(position, baseColors[i], fast ? 6 : 5);
            parts[i].transform.localScale = Vector3.one * sizes[i];
        }

        float t = 0f;
        while (t < TotalDuration)
        {
            float dt = Time.deltaTime;
            t += dt;

            //플래시 (0.14초)
            if (flash != null)
            {
                float k = t / 0.14f;
                if (k >= 1f) { FxPool.Release(flash); flash = null; }
                else
                {
                    var c = flash.color; c.a = Mathf.Lerp(0.95f, 0f, k); flash.color = c;
                    flash.transform.localScale = Vector3.one * Mathf.Lerp(1.4f, 3.0f, k);
                }
            }

            ring1 = UpdateRing(ring1, color, t, 0f, 1.6f);
            ring2 = UpdateRing(ring2, Color.white, t, 0.06f, 2.4f);

            //스파크 (0.32초)
            if (sparks[0] != null)
            {
                float k = Mathf.Clamp01(t / 0.32f);
                if (k >= 1f)
                {
                    for (int i = 0; i < SparkCount; i++) { FxPool.Release(sparks[i]); sparks[i] = null; }
                }
                else
                {
                    float stretch = Mathf.Sin(k * Mathf.PI);
                    float alpha = Mathf.Lerp(0.9f, 0f, k);
                    for (int i = 0; i < SparkCount; i++)
                    {
                        float dist = Mathf.Lerp(0.1f, sparkLens[i], Mathf.Min(1f, k * 2.2f));
                        var tr = sparks[i].transform;
                        tr.position = position + (Vector3)(sparkDirs[i] * dist * 0.5f);
                        tr.localScale = new Vector3(0.18f * stretch + 0.05f, dist, 1f);
                        var c = sparks[i].color; c.a = alpha; sparks[i].color = c;
                    }
                }
            }

            //파티클 (빠름 0.45초, 느림 0.75초)
            for (int i = 0; i < partCount; i++)
            {
                if (parts[i] == null) continue;
                bool fast = i < FastCount;
                float life = fast ? 0.45f : 0.75f;
                float k = t / life;
                if (k >= 1f) { FxPool.Release(parts[i]); parts[i] = null; continue; }

                vels[i] *= fast ? 0.90f : 0.96f;
                vels[i] += Vector3.down * dt * (fast ? 6f : 3f);
                var tr = parts[i].transform;
                tr.position += vels[i] * dt;
                tr.Rotate(0f, 0f, spins[i] * dt);
                tr.localScale = Vector3.one * sizes[i] * Mathf.Lerp(1.2f, 0.3f, k);
                var c = baseColors[i]; c.a = 1f - k; parts[i].color = c;
            }

            yield return null;
        }

        //정리
        if (flash != null) FxPool.Release(flash);
        if (ring1 != null) FxPool.Release(ring1);
        if (ring2 != null) FxPool.Release(ring2);
        for (int i = 0; i < SparkCount; i++) if (sparks[i] != null) FxPool.Release(sparks[i]);
        for (int i = 0; i < partCount; i++) if (parts[i] != null) FxPool.Release(parts[i]);
    }

    private static SpriteRenderer UpdateRing(SpriteRenderer ring, Color color, float t, float delay, float maxRadius)
    {
        if (ring == null) return null;
        float local = t - delay;
        if (local < 0f) return ring;

        float k = Mathf.Clamp01(local / 0.45f);
        if (k >= 1f) { FxPool.Release(ring); return null; }

        float eased = 1f - (1f - k) * (1f - k);
        ring.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, maxRadius, eased);
        float alpha = k < 0.12f ? Mathf.Lerp(0f, 0.8f, k / 0.12f) : Mathf.Lerp(0.8f, 0f, (k - 0.12f) / 0.88f);
        ring.color = new Color(color.r, color.g, color.b, alpha);
        return ring;
    }
}
