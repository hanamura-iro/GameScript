using System.Collections;
using UnityEngine;

public static class ParticleBurst
{
    public static void Spawn(MonoBehaviour runner, Vector3 position, Color color, int count = 10, float speed = 3f, float duration = 0.5f)
    {
        runner.StartCoroutine(BurstRoutine(position, color, count, speed, duration));
    }

    private static IEnumerator BurstRoutine(Vector3 position, Color color, int count, float speed, float duration)
    {
        var parts = new SpriteRenderer[count];
        var vels = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            parts[i] = FxPool.Get(position, color, 6);
            parts[i].transform.localScale = Vector3.one * Random.Range(0.12f, 0.3f);

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float mag = speed * Random.Range(0.5f, 1.3f);
            vels[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * mag;
        }

        float t = 0f;
        while (t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < count; i++)
            {
                vels[i] += Vector3.down * dt * 5f; // 중력
                parts[i].transform.position += vels[i] * dt;
                var c = color; c.a = 1f - k; parts[i].color = c;
            }
            yield return null;
        }

        for (int i = 0; i < count; i++) FxPool.Release(parts[i]);
    }
}
