using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Camera))]
public class CameraFitBackground : MonoBehaviour
{
    [Header("배경 (여러 개면 전부 넣기)")]
    public SpriteRenderer[] backgrounds;

    [Header("옵션")]
    public bool fitHorizontalLimits = true;   //좌우 이동 범위도 배경 안으로 자동 계산
    public float edgePadding = 0.01f;         //경계에 생기는 1픽셀 틈 방지

    private Camera cam;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    private void LateUpdate()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            Fit();
        }
    }

    private void Fit()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (!TryGetBounds(out Bounds bg)) return;

        float maxHalfHeight = bg.extents.y - edgePadding;

        if (cam.orthographicSize > maxHalfHeight)
        {
            cam.orthographicSize = maxHalfHeight;
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, bg.min.y + halfH, bg.max.y - halfH);
        transform.position = pos;

        if (fitHorizontalLimits)
        {
            CameraController controller = GetComponent<CameraController>();
            if (controller != null)
            {
                float minX = bg.min.x + halfW + edgePadding;
                float maxX = bg.max.x - halfW - edgePadding;

                if (minX > maxX)
                {
                    minX = maxX = bg.center.x;
                }

                controller.minX = minX;
                controller.maxX = maxX;
            }
        }
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        if (backgrounds == null) return false;

        foreach (SpriteRenderer sr in backgrounds)
        {
            if (sr == null) continue;

            if (!found)
            {
                bounds = sr.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        return found;
    }
}