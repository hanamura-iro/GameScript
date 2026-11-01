using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class SlimeDrag : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
{
    [Header("가장자리 자동 스크롤")]
    [Range(0.05f, 0.3f)]
    public float edgeRatio = 0.12f;     //화면 폭 중 좌우 몇 %를 가장자리로 볼지

    [Header("드래그 연출")]
    public int dragSortingBoost = 100;  //드는 동안 다른 슬라임보다 위에 보이게
    public Color32 dragColor = new Color32(140, 140, 140, 255);

    [Header("말랑한 잡기")]
    public float liftScale = 1.12f;     //집었을 때 커지는 정도
    public float wobbleAmount = 0.15f;  //집는 순간 출렁임 세기
    public float stretchAmount = 0.015f; //끄는 속도에 따라 늘어나는 정도
    public float maxStretch = 0.25f;

    private Camera cam;
    private SlimeMove slimeMove;
    private SlimeBase slimeBase;
    private SpriteRenderer render;
    private Color originalColor;
    private int originalSortingOrder;

    private bool dragging;
    private int pointerId;
    private Vector2 pointerScreen;
    private Vector3 grabOffset;

    private Vector3 grabScale;
    private Vector3 lastPosition;
    private float smoothedSpeedX;
    private float holdTime;

    private void Awake()
    {
        cam = Camera.main;
        slimeMove = GetComponent<SlimeMove>();
        slimeBase = GetComponent<SlimeBase>();
        render = GetComponent<SpriteRenderer>();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (dragging) return;

        dragging = true;
        pointerId = e.pointerId;
        pointerScreen = e.position;

        grabOffset = transform.position - ScreenToWorld(pointerScreen);
        grabOffset.z = 0f;

        grabScale = transform.localScale;
        lastPosition = transform.position;
        smoothedSpeedX = 0f;
        holdTime = 0f;

        if (slimeMove != null) slimeMove.isHeld = true;

        if (render != null)
        {
            originalColor = render.color;
            originalSortingOrder = render.sortingOrder;
            render.color = dragColor;
            render.sortingOrder = originalSortingOrder + dragSortingBoost;
        }

        if (GameFeel.instance != null) GameFeel.instance.OnGrab();
    }

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        e.useDragThreshold = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging || e.pointerId != pointerId) return;
        pointerScreen = e.position;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!dragging || e.pointerId != pointerId) return;
        EndDrag();
    }

    private void Update()
    {
        if (!dragging) return;

        float edge = Screen.width * edgeRatio;
        int dir = 0;
        if (pointerScreen.x < edge) dir = -1;
        else if (pointerScreen.x > Screen.width - edge) dir = 1;

        if (CameraController.instance != null)
            CameraController.instance.SetEdgeInput(dir);
    }

    private void LateUpdate()
    {
        if (!dragging) return;

        Vector3 newPosition = ScreenToWorld(pointerScreen) + grabOffset;
        transform.position = newPosition;

        float dt = Time.deltaTime;
        if (dt <= 0f) return; 

        float speedX = (newPosition.x - lastPosition.x) / dt;
        lastPosition = newPosition;
        smoothedSpeedX = Mathf.Lerp(smoothedSpeedX, speedX, 1f - Mathf.Exp(-15f * dt));

        holdTime += dt;

        float lift = Mathf.Lerp(1f, liftScale, 1f - Mathf.Exp(-20f * holdTime));
        float wobble = Mathf.Sin(holdTime * 30f) * wobbleAmount * Mathf.Exp(-holdTime * 8f);

        float stretch = Mathf.Min(Mathf.Abs(smoothedSpeedX) * stretchAmount, maxStretch);

        float sx = lift * (1f + stretch + wobble);
        float sy = lift * (1f - stretch * 0.6f - wobble);

        transform.localScale = new Vector3(grabScale.x * sx, grabScale.y * sy, grabScale.z);
    }

    private void OnDisable()
    {
        if (dragging) EndDrag();
    }

    private void EndDrag()
    {
        dragging = false;

        if (CameraController.instance != null)
            CameraController.instance.SetEdgeInput(0);

        if (render != null)
        {
            render.color = originalColor;
            render.sortingOrder = originalSortingOrder;
        }

        bool merged = SlimeMerge.instance != null && SlimeMerge.instance.TryMerge(slimeBase);

        if (!merged)
        {
            if (slimeMove != null) slimeMove.Drop(transform.position);
            if (GameFeel.instance != null) GameFeel.instance.OnDrop();
        }
    }

    private Vector3 ScreenToWorld(Vector2 screen)
    {
        float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        w.z = transform.position.z;
        return w;
    }
}