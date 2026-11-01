using UnityEngine;

public class SpawnPop : MonoBehaviour
{
    public float duration = 0.35f;
    public float startScale = 0.3f;

    private float timer;
    private int spawnFrame;
    private SlimeMove move;

    private void Awake()
    {
        spawnFrame = Time.frameCount;
        move = GetComponent<SlimeMove>();
    }

    private void LateUpdate()
    {
        if (Time.frameCount == spawnFrame) return;

        if (move != null && move.isHeld)
        {
            Destroy(this);
            return;
        }

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        float ease = 1f + c3 * u * u * u + c1 * u * u;

        transform.localScale *= Mathf.LerpUnclamped(startScale, 1f, ease);

        if (t >= 1f) Destroy(this);
    }
}