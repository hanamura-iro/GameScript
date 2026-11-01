using System.Collections;
using UnityEngine;

public static class PlacementRing
{
    public static void Spawn(MonoBehaviour runner, Vector3 position, Color color, float maxRadius = 2.4f, float duration = 0.4f)
    {
        runner.StartCoroutine(RingRoutine(position, color, maxRadius, duration));
    }

    private static IEnumerator RingRoutine(Vector3 position, Color color, float maxRadius, float duration)
    {
        var go = new GameObject("PlacementRing");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Circle;
        sr.color = new Color(color.r, color.g, color.b, 0f);
        sr.sortingOrder = 5;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - k) * (1f - k);

            float radius = Mathf.Lerp(0.3f, maxRadius, eased);
            go.transform.localScale = Vector3.one * radius;

            float alpha = k < 0.15f ? Mathf.Lerp(0f, 0.85f, k / 0.15f) : Mathf.Lerp(0.85f, 0f, (k - 0.15f) / 0.85f);
            sr.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        Object.Destroy(go);
    }
}
