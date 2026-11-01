using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    [Header("이동 제한 설정 (X 좌표)")]
    public float minX = -12f;
    public float maxX = 12f;

    [Header("이동 속도 설정")]
    public float moveSpeed = 3f;

    [Header("드래그 중 가장자리 자동 스크롤 속도")]
    public float edgeMoveSpeed = 8f;

    [Header("자동 합치기 카메라 이동")]
    public float focusSmoothTime = 0.25f;  // 작을수록 빠르게 도착
    public float focusMaxSpeed = 40f;

    private Camera cam;
    private float targetX;
    private float baseY;
    private float input = 0f;
    private float edgeInput = 0f;

    private float shakeTimer;
    private float shakeDuration;
    private float shakeStrength;

    private bool focusing;
    private float focusX;
    private float focusVelocity;

    // 흔들림이 섞이지 않은 현재 카메라 X
    public float CurrentX => targetX;

    // 화면 절반 너비 (월드 단위)
    public float HalfWidth => cam != null ? cam.orthographicSize * cam.aspect : 3f;

    public bool IsFocusing => focusing;
    public bool FocusArrived => !focusing || Mathf.Abs(targetX - focusX) < 0.05f;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        targetX = transform.position.x;
        baseY = transform.position.y;
    }

    public void MoveLeft()
    {
        input = -1f;
    }

    public void MoveRight()
    {
        input = 1f;
    }

    public void StopMoving()
    {
        input = 0f;
    }

    public void SetEdgeInput(int dir)
    {
        edgeInput = dir;
    }

    public void FocusOn(float x)
    {
        focusing = true;
        focusX = Mathf.Clamp(x, minX, maxX);
    }

    public void ClearFocus()
    {
        focusing = false;
        focusVelocity = 0f;
    }

    public bool IsVisible(float x, float margin)
    {
        return Mathf.Abs(x - targetX) <= HalfWidth - margin;
    }

    public void Shake(float strength, float duration)
    {
        float currentStrength = shakeTimer > 0f ? shakeStrength * (shakeTimer / shakeDuration) : 0f;
        if (strength < currentStrength) return;

        shakeStrength = strength;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    void Update()
    {
        if (focusing)
        {
            targetX = Mathf.SmoothDamp(targetX, focusX, ref focusVelocity,
                focusSmoothTime, focusMaxSpeed, Time.unscaledDeltaTime);
        }
        else
        {
            targetX += (input * moveSpeed + edgeInput * edgeMoveSpeed) * Time.deltaTime;
        }

        targetX = Mathf.Clamp(targetX, minX, maxX);

        Vector2 shakeOffset = Vector2.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float k = Mathf.Max(shakeTimer, 0f) / shakeDuration;
            shakeOffset = Random.insideUnitCircle * shakeStrength * k;
        }

        Vector3 currentPosition = transform.position;
        currentPosition.x = targetX + shakeOffset.x;
        currentPosition.y = baseY + shakeOffset.y;
        transform.position = currentPosition;
    }
}