using UnityEngine;

public class SlimeMove : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 1f;            //이동 속도
    public float minX = -11.5f;          //왼쪽 이동 제한 (최소 X 좌푯값)
    public float maxX = 11.5f;           //오른쪽 이동 제한 (최대 X 좌푯값)

    [Header("바닥 범위 설정 (Y)")]
    public float minY = -3.7f;           //놓았을 때 허용되는 최소 Y
    public float maxY = -1.55f;          //놓았을 때 허용되는 최대 Y
    public float fallGravity = 30f;      //범위 밖에서 놓았을 때 범위 안으로 들어가는 가속도

    [Header("점프 설정")]
    public float jumpHeight = 1f;
    public float jumpDuration = 0.6f;
    public float minJumpCooldown = 1.5f;
    public float maxJumpCooldown = 3f;
    public float jumpChance = 0.5f;

    [Header("Idle 바운스 설정")]
    public float idleBounceHeight = 0.08f;
    public float idleCycleDuration = 1.1f;

    [HideInInspector] public bool isHeld; //SlimeDrag가 들고 있는 동안 true

    private Vector3 logicalPosition;    //바운스/점프 오프셋이 섞이지 않은 순수 이동 위치
    private Vector3 targetPosition;     //현재 이동 중인 목표 위치
    private Vector3 baseScale;          //원래 로컬 스케일 (좌우 반전 전 절댓값 기준)
    private bool started;

    private float jumpCooldownTimer;
    private bool isJumping;
    private float jumpTimer;
    private float idleTimer;

    private bool isSettling;            //범위 안으로 떨어지는(들어가는) 중
    private float settleTargetY;
    private float settleVelocity;
    public Vector3 LogicalPosition
    {
        get
        {
            if (!started) return transform.position;
            Vector3 pos = logicalPosition;
            if (isSettling) pos.y = settleTargetY;
            return pos;
        }
    }


    void Start()
    {
        logicalPosition = transform.position;
        baseScale = transform.localScale;
        started = true;

        SetNewRandomTarget();
        SetNextJumpCooldown();
    }

    void Update()
    {
        if (isHeld) return;

        if (isSettling)
        {
            HandleSettle();
            ApplyFinalTransform();
            return;
        }

        HandleMovement();

        if (isJumping)
        {
            HandleJump();
        }
        else
        {
            HandleIdleBounce();
            CheckJumpTrigger();
        }

        ApplyFinalTransform();
    }
    public void Drop(Vector3 droppedPosition)
    {
        droppedPosition.x = Mathf.Clamp(droppedPosition.x, minX, maxX);
        logicalPosition = droppedPosition;

        isJumping = false;
        jumpTimer = 0f;
        idleTimer = 0f;
        visualYOffset = 0f;
        visualScaleXMultiplier = 1f;
        visualScaleYMultiplier = 1f;

        settleTargetY = Mathf.Clamp(droppedPosition.y, minY, maxY);
        isSettling = !Mathf.Approximately(settleTargetY, droppedPosition.y);
        settleVelocity = 0f;

        if (!isSettling)
        {
            SetNewRandomTarget();
        }

        SetNextJumpCooldown();

        isHeld = false;
        ApplyFinalTransform();
    }

    private void HandleSettle()
    {
        settleVelocity += fallGravity * Time.deltaTime;
        logicalPosition.y = Mathf.MoveTowards(logicalPosition.y, settleTargetY, settleVelocity * Time.deltaTime);

        if (Mathf.Approximately(logicalPosition.y, settleTargetY))
        {
            isSettling = false;
            settleVelocity = 0f;
            idleTimer = 0f;
            SetNewRandomTarget();
        }
    }

    private void HandleMovement()
    {
        Vector3 nextLogical = Vector3.MoveTowards(logicalPosition, targetPosition, speed * Time.deltaTime);
        if (nextLogical.x > logicalPosition.x)
        {
            baseScale.x = Mathf.Abs(baseScale.x);
        }
        else if (nextLogical.x < logicalPosition.x)
        {
            baseScale.x = -Mathf.Abs(baseScale.x);
        }

        logicalPosition = nextLogical;

        if (Vector3.Distance(logicalPosition, targetPosition) < 0.01f)
        {
            SetNewRandomTarget();
        }
    }

    private void SetNewRandomTarget()
    {
        float randomX = Random.Range(minX, maxX);
        targetPosition = new Vector3(randomX, logicalPosition.y, logicalPosition.z);
    }

    private float visualYOffset;
    private float visualScaleXMultiplier = 1f;
    private float visualScaleYMultiplier = 1f;

    private void HandleIdleBounce()
    {
        idleTimer += Time.deltaTime;
        float t = (idleTimer % idleCycleDuration) / idleCycleDuration;

        float squashPhase = Mathf.Sin(t * Mathf.PI * 2f);
        visualScaleXMultiplier = 1f + Mathf.Max(0f, -squashPhase) * 0.12f;
        visualScaleYMultiplier = 1f - Mathf.Max(0f, -squashPhase) * 0.15f + Mathf.Max(0f, squashPhase) * 0.05f;

        float heightPhase = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI * 0.3f));
        visualYOffset = heightPhase * idleBounceHeight;
    }

    private void CheckJumpTrigger()
    {
        jumpCooldownTimer -= Time.deltaTime;
        if (jumpCooldownTimer <= 0f)
        {
            if (Random.value < jumpChance)
            {
                isJumping = true;
                jumpTimer = 0f;
            }
            else
            {
                SetNextJumpCooldown();
            }
        }
    }

    private void HandleJump()
    {
        jumpTimer += Time.deltaTime;
        float t = jumpTimer / jumpDuration;

        if (t >= 1f)
        {
            isJumping = false;
            jumpTimer = 0f;
            idleTimer = 0f;
            visualYOffset = 0f;
            visualScaleXMultiplier = 1f;
            visualScaleYMultiplier = 1f;
            SetNextJumpCooldown();
            return;
        }

        float arc = Mathf.Sin(t * Mathf.PI);
        visualYOffset = arc * jumpHeight;

        float squashAmount;
        if (t < 0.15f)
        {
            float launchT = t / 0.15f;
            squashAmount = Mathf.Lerp(-0.3f, 0.2f, launchT);
        }
        else if (t > 0.85f)
        {
            float landT = (t - 0.85f) / 0.15f;
            squashAmount = Mathf.Lerp(0.2f, -0.3f, landT);
        }
        else
        {
            squashAmount = 0.2f;
        }

        visualScaleXMultiplier = 1f - squashAmount * 0.5f;
        visualScaleYMultiplier = 1f + squashAmount * 0.5f;
    }

    private void SetNextJumpCooldown()
    {
        jumpCooldownTimer = Random.Range(minJumpCooldown, maxJumpCooldown);
    }

    private void ApplyFinalTransform()
    {
        transform.position = logicalPosition + new Vector3(0f, visualYOffset, 0f);
        transform.localScale = new Vector3(
            baseScale.x * visualScaleXMultiplier,
            baseScale.y * visualScaleYMultiplier,
            baseScale.z);
    }
}