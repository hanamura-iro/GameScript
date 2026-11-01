using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GridManager grid;
    public ScoreManager scoreManager;
    public StatsManager statsManager;
    public Transform trayParent;

    [Header("카메라 (GameBootstrap이 자동 연결)")]
    public Camera mainCam;

    public int gridWidth = 8;
    public int gridHeight = 9;

    [Header("조각 배치 간격 (셀 크기 배수 기준, cellSize가 바뀌어도 자동으로 맞춰짐)")]
    public float trayColumnSpacingCells = 4.3f; // 최대 조각 크기(4칸)보다 살짝만 커서 안 겹치는 선에서 최대한 좁힘
    public float trayRowSpacingCells = 4.3f; // 절대값만 사용(위/아래 방향은 코드에서 결정)
    public int trayColumns = 3;
    public float traySeparation = 1.2f; // 보드와 조각 그룹 사이 간격(피스가 보드 쪽으로 안 삐져나오게만 최소한으로)

    private float TrayColumnSpacing => trayColumnSpacingCells * grid.cellSize;
    private float TrayRowSpacing => Mathf.Abs(trayRowSpacingCells) * grid.cellSize;

    [Header("제한시간 (타임어택 전용)")]
    public float startingTime = 60f;
    public float maxTime = 300f;
    public float flatBonusPerStage = 30f;
    public float speedBonusRatio = 0f;

    [Header("콤보")]
    public float comboMultiplierStep = 0.1f;
    public int comboMultiplierCap = 9;

    [Header("광고 (타임어택 전용 기능 포함)")]
    public int interstitialEveryNStages = 5;
    [Range(0f, 1f)] public float interstitialChance = 0.5f;
    public float rewardedTimeBonus = 20f;
    public float reviveTime = 15f;

    [Header("화면 확대")]
    public float visualZoom = 1f;
    public float minVisualZoom = 0.6f;
    public float maxVisualZoom = 1.6f;
    private const string VisualZoomKey = "ShapeFit_VisualZoom";

    public bool HasRevivedThisGame { get; private set; }
    public GameMode CurrentMode { get; private set; } = GameMode.TimeAttack;
    public bool IsPlaying { get; private set; }

    private PieceController selectedPiece;
    public PieceController SelectedPiece
    {
        get => selectedPiece;
        set
        {
            if (selectedPiece == value) return;
            if (selectedPiece != null) selectedPiece.SetSelected(false);
            selectedPiece = value;
            if (selectedPiece != null) selectedPiece.SetSelected(true);
        }
    }

    public int StageNumber { get; private set; } = 1;
    public float TimeRemaining { get; private set; }
    public bool IsGameOver { get; private set; }
    public int ComboCount { get; private set; }

    public event Action<int, int> OnStageCleared;
    public event Action<string> OnMessage;
    public event Action<GameMode> OnGameStarted;
    public event Action OnReturnedToMenu;

    private int lastTickSecond = -1;
    private int hintUsesThisStage;
    private float cachedGridCenterX;
    private int reachedPieceCountFloor = 3;

    // 이번 스테이지의 조각 배치 범위
    private float aboveExtentWorld;
    private float belowExtentWorld;

    void Start()
    {
        visualZoom = PlayerPrefs.GetFloat(VisualZoomKey, visualZoom);
        customBaseCount = PlayerPrefs.GetInt(CustomBaseKey, customBaseCount);
        customInterval = PlayerPrefs.GetInt(CustomIntervalKey, customInterval);
        cachedGridCenterX = (gridWidth - 1) * grid.cellSize / 2f;
        if (trayParent != null)
            trayParent.position = new Vector3(cachedGridCenterX, 0f, 0f);

        if (mainCam != null)
        {
            mainCam.rect = new Rect(0f, 0f, 1f, 1f);
            mainCam.orthographic = true;
        }

        FrameCamera();
    }

    void Update()
    {
        FrameCamera();

        if (!IsPlaying || IsGameOver) return;
        if (CurrentMode != GameMode.TimeAttack) return;

        TimeRemaining -= Time.deltaTime;
        int ceil = Mathf.CeilToInt(TimeRemaining);
        if (ceil <= 5 && ceil >= 0 && ceil != lastTickSecond)
        {
            lastTickSecond = ceil;
            if (ceil > 0) Sfx.Tick();
        }

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TriggerGameOver();
        }
    }

    [Header("모드별 보드 크기")]
    public int normalGridWidth = 8;
    public int normalGridHeight = 9;
    public int bigBoardGridWidth = 10;
    public int bigBoardGridHeight = 14; // 10x14=140칸 - 30개(120칸)가 여유 있게 들어가도록
    public int customGridWidth = 12;
    public int customGridHeight = 14; // 168칸 - 시작 블록수 최대치(30개=120칸)가 여유 있게 들어가도록

    [Header("모드별 조각 개수 범위")]
    public int normalBaseCount = 3;
    public int normalCapCount = 14;
    public int bigBoardBaseCount = 20;
    public int bigBoardCapCount = 30;
    public int defaultInterval = 5; // 몇 스테이지마다 조각이 1개씩 늘어나는지 (커스텀 모드 제외 공통)

    [Header("커스텀 모드 설정 (설정 패널에서 유저 직접 조절, PlayerPrefs 저장)")]
    public int customBaseCount = 3;
    public int customInterval = 5;
    public int customCapCount = 14;
    public const int CustomBaseMin = 3, CustomBaseMax = 30;
    public const int CustomIntervalMin = 1, CustomIntervalMax = 10;
    private const string CustomBaseKey = "ShapeFit_Custom_Base";
    private const string CustomIntervalKey = "ShapeFit_Custom_Interval";

    private int currentBaseCount = 3;
    private int currentCapCount = 14;
    private int currentInterval = 5;

    // ---- 모드별 진행상황, 저장/조회
    private static string ProgressStageKey(GameMode m) => $"ShapeFit_Progress_{m}_Stage";
    private static string ProgressScoreKey(GameMode m) => $"ShapeFit_Progress_{m}_Score";
    private static string ProgressFloorKey(GameMode m) => $"ShapeFit_Progress_{m}_Floor";

    public static bool HasSavedProgress(GameMode mode) => PlayerPrefs.HasKey(ProgressStageKey(mode));
    public static int GetSavedStage(GameMode mode) => PlayerPrefs.GetInt(ProgressStageKey(mode), 1);
    public static int GetSavedScore(GameMode mode) => PlayerPrefs.GetInt(ProgressScoreKey(mode), 0);
    public static int GetSavedFloor(GameMode mode) => PlayerPrefs.GetInt(ProgressFloorKey(mode), 3);

    private static string ProgressSnapshotKey(GameMode m) => $"ShapeFit_Progress_{m}_Snapshot";

    private static void SaveProgress(GameMode mode, int stage, int score, int pieceFloor)
    {
        PlayerPrefs.SetInt(ProgressStageKey(mode), stage);
        PlayerPrefs.SetInt(ProgressScoreKey(mode), score);
        PlayerPrefs.SetInt(ProgressFloorKey(mode), pieceFloor);
        PlayerPrefs.Save();
    }

    private static void ClearProgress(GameMode mode)
    {
        PlayerPrefs.DeleteKey(ProgressStageKey(mode));
        PlayerPrefs.DeleteKey(ProgressScoreKey(mode));
        PlayerPrefs.DeleteKey(ProgressFloorKey(mode));
        PlayerPrefs.DeleteKey(ProgressSnapshotKey(mode));
        PlayerPrefs.Save();
    }

    //스테이지 스냅샷, 직렬화
    [Serializable]
    private class PieceSnap
    {
        public int type;
        public int rotation;
        public bool placed;
        public int ox, oy;        
        public int solOx, solOy;   
        public int solRot;
    }

    [Serializable]
    private class StageSnap
    {
        public List<int> cx = new List<int>();
        public List<int> cy = new List<int>();
        public List<int> creq = new List<int>(); // -1 = 색 제약 없음
        public List<PieceSnap> pieces = new List<PieceSnap>();
        public int hintUses; // 이 스테이지에서 이미 쓴 힌트 횟수 - 이어하기 해도 그대로 유지되도록
    }

    private string CaptureSnapshotJson()
    {
        var snap = new StageSnap();
        snap.hintUses = hintUsesThisStage;
        var cells = new List<Vector2Int>();
        var reqTypes = new List<int>();
        grid.ExportTargetCells(cells, reqTypes);
        for (int i = 0; i < cells.Count; i++)
        {
            snap.cx.Add(cells[i].x);
            snap.cy.Add(cells[i].y);
            snap.creq.Add(reqTypes[i]);
        }

        foreach (Transform child in trayParent)
        {
            var pc = child.GetComponent<PieceController>();
            if (pc != null) snap.pieces.Add(BuildPieceSnap(pc));
        }
        foreach (Transform child in grid.transform)
        {
            var pc = child.GetComponent<PieceController>();
            if (pc != null) snap.pieces.Add(BuildPieceSnap(pc));
        }

        return JsonUtility.ToJson(snap);
    }

    private void SaveSnapshotIfPlaying()
    {
        if (!IsPlaying || IsGameOver) return;
        string json = CaptureSnapshotJson();
        PlayerPrefs.SetString(ProgressSnapshotKey(CurrentMode), json);
        PlayerPrefs.Save();
    }

    private static PieceSnap BuildPieceSnap(PieceController pc)
    {
        var origin = pc.PlacedOriginValue;
        var sol = pc.CorrectOriginValue;
        return new PieceSnap
        {
            type = (int)pc.type,
            rotation = pc.rotation,
            placed = origin.HasValue,
            ox = origin.HasValue ? origin.Value.x : 0,
            oy = origin.HasValue ? origin.Value.y : 0,
            solOx = sol.x,
            solOy = sol.y,
            solRot = pc.CorrectRotationValue,
        };
    }

    private bool TryRestoreSnapshot(GameMode mode)
    {
        string json = PlayerPrefs.GetString(ProgressSnapshotKey(mode), "");
        if (string.IsNullOrEmpty(json)) return false;

        StageSnap snap;
        try { snap = JsonUtility.FromJson<StageSnap>(json); }
        catch { return false; }
        if (snap == null || snap.cx == null || snap.cx.Count == 0) return false;

        for (int i = trayParent.childCount - 1; i >= 0; i--)
            Destroy(trayParent.GetChild(i).gameObject);
        DestroyPlacedPiecesOnGrid();
        SelectedPiece = null;
        hintUsesThisStage = Mathf.Max(0, snap.hintUses);

        var targetCells = new List<Vector2Int>();
        Dictionary<Vector2Int, TetrominoType> reqTypes = null;
        for (int i = 0; i < snap.cx.Count; i++)
        {
            var c = new Vector2Int(snap.cx[i], snap.cy[i]);
            targetCells.Add(c);
            if (snap.creq[i] >= 0)
            {
                reqTypes ??= new Dictionary<Vector2Int, TetrominoType>();
                reqTypes[c] = (TetrominoType)snap.creq[i];
            }
        }
        grid.SetTargetShape(targetCells, reqTypes);

        var unplaced = new List<PieceSpawnData>();
        var placedSnaps = new List<PieceSnap>();
        foreach (var p in snap.pieces)
        {
            if (p.placed) placedSnaps.Add(p);
            else unplaced.Add(new PieceSpawnData
            {
                type = (TetrominoType)p.type,
                rotation = p.rotation,
                solutionOrigin = new Vector2Int(p.solOx, p.solOy),
                solutionRotation = p.solRot,
            });
        }
        SpawnPieces(unplaced, Vector2Int.zero);

        foreach (var p in placedSnaps)
        {
            var type = (TetrominoType)p.type;
            var cells = TetrominoLibrary.GetCells(type, p.rotation);
            var origin = new Vector2Int(p.ox, p.oy);
            grid.PlacePiece(origin, cells, TetrominoLibrary.GetColor(type));

            var go = new GameObject($"Piece_{type}");
            var pc = go.AddComponent<PieceController>();
            pc.Init(grid, this, type, p.rotation);
            pc.SetSolution(new Vector2Int(p.solOx, p.solOy), p.solRot);
            pc.RestorePlaced(origin);
        }

        return true;
    }

    public int GetCapForMode(GameMode mode)
    {
        if (mode == GameMode.BigBoard) return bigBoardCapCount;
        if (mode == GameMode.Custom) return Mathf.Min(customBaseCount + 8, CustomBaseMax);
        return normalCapCount;
    }

    public void SetCustomBaseCount(int value)
    {
        customBaseCount = Mathf.Clamp(value, CustomBaseMin, CustomBaseMax);
        PlayerPrefs.SetInt(CustomBaseKey, customBaseCount);
        PlayerPrefs.Save();
    }

    public void SetCustomInterval(int value)
    {
        customInterval = Mathf.Clamp(value, CustomIntervalMin, CustomIntervalMax);
        PlayerPrefs.SetInt(CustomIntervalKey, customInterval);
        PlayerPrefs.Save();
    }

    public void BeginGame(GameMode mode, bool resume = false)
    {
        CancelInvoke();
        CurrentMode = mode;
        scoreManager.SetMode(mode);

        IsGameOver = false;
        HasRevivedThisGame = false;
        ComboCount = 0;
        hintUsesThisStage = 0;
        lastTickSecond = -1;
        SelectedPiece = null;

        grid.SetColorMatchMode(mode == GameMode.ColorMatch);

        if (mode == GameMode.BigBoard)
        {
            gridWidth = bigBoardGridWidth;
            gridHeight = bigBoardGridHeight;
            currentBaseCount = bigBoardBaseCount;
            currentCapCount = bigBoardCapCount;
            currentInterval = defaultInterval;
        }
        else if (mode == GameMode.Custom)
        {
            gridWidth = customGridWidth;
            gridHeight = customGridHeight;
            currentBaseCount = customBaseCount;
            currentCapCount = Mathf.Min(customBaseCount + 8, CustomBaseMax);
            currentInterval = customInterval;
        }
        else
        {
            gridWidth = normalGridWidth;
            gridHeight = normalGridHeight;
            currentBaseCount = normalBaseCount;
            currentCapCount = normalCapCount;
            currentInterval = defaultInterval;
        }
        grid.Resize(gridWidth, gridHeight);

        cachedGridCenterX = (gridWidth - 1) * grid.cellSize / 2f;
        if (trayParent != null)
            trayParent.position = new Vector3(cachedGridCenterX, 0f, 0f);

        bool hasSave = resume && HasSavedProgress(mode);
        if (hasSave)
        {
            StageNumber = Mathf.Max(1, GetSavedStage(mode));
            scoreManager.SetCurrentScore(GetSavedScore(mode));
            reachedPieceCountFloor = Mathf.Max(currentBaseCount, GetSavedFloor(mode));
        }
        else
        {
            StageNumber = 1;
            scoreManager.ResetScore();
            ClearProgress(mode);
            reachedPieceCountFloor = currentBaseCount;
        }

        TimeRemaining = mode == GameMode.TimeAttack ? startingTime : 0f;

        IsPlaying = true;

        bool restored = hasSave && TryRestoreSnapshot(mode);
        if (!restored)
            StartStage(); 

        OnGameStarted?.Invoke(mode);
    }

    public void ReturnToMenu()
    {
        CancelInvoke();
        scoreManager.RecordStage(StageNumber);
        if (IsPlaying && !IsGameOver)
        {
            SaveProgress(CurrentMode, StageNumber, scoreManager.Score, reachedPieceCountFloor);
            SaveSnapshotIfPlaying();
        }
        IsPlaying = false;
        IsGameOver = false;
        SelectedPiece = null;

        for (int i = trayParent.childCount - 1; i >= 0; i--)
            Destroy(trayParent.GetChild(i).gameObject);
        DestroyPlacedPiecesOnGrid();
        grid.ClearBoard();

        OnReturnedToMenu?.Invoke();
    }

    public void RotateSelectedPiece(bool clockwise = true)
    {
        if (IsGameOver) return;
        if (SelectedPiece != null) SelectedPiece.RotateOnce(clockwise);
    }

    public void NotifyMistake()
    {
        ComboCount = 0;
    }

    //카메라

    private void FrameCamera()
    {
        if (mainCam == null) return;
        float cellSize = grid != null ? grid.cellSize : 1f;

        float boardHalfWidth = gridWidth * cellSize / 2f;
        float trayHalfWidth = trayColumns * TrayColumnSpacing / 2f + cellSize; // 가장 넓은 조각(가로 I자 4칸)도 안 잘리게 여유분 추가
        float halfWidth = Mathf.Max(boardHalfWidth, trayHalfWidth);

        float boardHalfHeight = gridHeight * cellSize / 2f;
        // 보드를 항상 정중앙에 두기 위해, 위/아래 중 더 많이 뻗은 쪽 기준으로 대칭 계산
        float halfHeight = boardHalfHeight + Mathf.Max(aboveExtentWorld, belowExtentWorld) + traySeparation * cellSize * 0.3f;

        float aspect = mainCam.aspect;
        float orthoSize = Mathf.Max(halfHeight, halfWidth / aspect) * 1.12f;
        orthoSize /= Mathf.Max(0.1f, visualZoom);

        mainCam.orthographicSize = orthoSize;
        var pos = mainCam.transform.position;
        pos.x = cachedGridCenterX + GetShakeOffsetX(orthoSize);
        pos.y = boardHalfHeight + GetShakeOffsetY(orthoSize); // 보드의 세로 중심 = 화면 정중앙
        pos.z = pos.z != 0 ? pos.z : -10f;
        mainCam.transform.position = pos;
    }

    //카메라 흔들림
    private float shakeTimeLeft;
    private float shakeDuration = 1f;
    private float shakeMagnitude;

    public void ShakeCamera(float duration = 0.12f, float magnitude = 0.02f)
    {
        shakeTimeLeft = duration;
        shakeDuration = Mathf.Max(0.0001f, duration);
        shakeMagnitude = magnitude;
    }

    private float GetShakeOffsetX(float orthoSize)
    {
        if (shakeTimeLeft <= 0f) return 0f;
        float t = shakeTimeLeft / shakeDuration;
        return (Mathf.PerlinNoise(Time.time * 60f, 0f) - 0.5f) * 2f * orthoSize * shakeMagnitude * t;
    }

    private float GetShakeOffsetY(float orthoSize)
    {
        if (shakeTimeLeft <= 0f) return 0f;
        float t = shakeTimeLeft / shakeDuration;
        float offset = (Mathf.PerlinNoise(0f, Time.time * 60f) - 0.5f) * 2f * orthoSize * shakeMagnitude * t;
        shakeTimeLeft = Mathf.Max(0f, shakeTimeLeft - Time.deltaTime);
        return offset;
    }

    public void SetVisualZoom(float newZoom)
    {
        visualZoom = Mathf.Clamp(newZoom, minVisualZoom, maxVisualZoom);
        PlayerPrefs.SetFloat(VisualZoomKey, visualZoom);
        PlayerPrefs.Save();
        FrameCamera();
    }

    private void DestroyPlacedPiecesOnGrid()
    {
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
        {
            var child = grid.transform.GetChild(i);
            if (child.GetComponent<PieceController>() != null)
                Destroy(child.gameObject);
        }
    }

    private void StartStage()
    {
        if (!IsPlaying || IsGameOver) return;

        scoreManager.RecordStage(StageNumber);

        for (int i = trayParent.childCount - 1; i >= 0; i--)
            Destroy(trayParent.GetChild(i).gameObject);
        DestroyPlacedPiecesOnGrid();
        SelectedPiece = null;
        hintUsesThisStage = 0;

        var result = StageGenerator.Generate(StageNumber, gridWidth, gridHeight, reachedPieceCountFloor,
            currentBaseCount, currentCapCount, currentInterval);
        reachedPieceCountFloor = Mathf.Max(reachedPieceCountFloor, result.pieces.Count);
        var (centeredCells, offset) = CenterShape(result.targetCells);

        Dictionary<Vector2Int, TetrominoType> shiftedRequiredTypes = null;
        if (grid.ColorMatchMode && result.cellRequiredTypes != null)
        {
            shiftedRequiredTypes = new Dictionary<Vector2Int, TetrominoType>();
            foreach (var kvp in result.cellRequiredTypes)
                shiftedRequiredTypes[kvp.Key + offset] = kvp.Value;
        }

        grid.SetTargetShape(centeredCells, shiftedRequiredTypes);
        SpawnPieces(result.pieces, offset);
    }

    private (List<Vector2Int> cells, Vector2Int offset) CenterShape(List<Vector2Int> cells)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in cells)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        int offsetX = Mathf.Max(0, (gridWidth - w) / 2) - minX;
        int offsetY = Mathf.Max(0, (gridHeight - h) / 2) - minY;
        var offset = new Vector2Int(offsetX, offsetY);

        var centered = new List<Vector2Int>(cells.Count);
        foreach (var c in cells)
            centered.Add(c + offset);
        return (centered, offset);
    }
    private void SpawnPieces(List<PieceSpawnData> pieces, Vector2Int solutionOffset)
    {
        int total = pieces.Count;
        int aboveCount = total / 2;
        int belowCount = total - aboveCount;

        var above = pieces.GetRange(0, aboveCount);
        var below = pieces.GetRange(aboveCount, belowCount);

        float boardTopY = gridHeight * grid.cellSize;
        float aboveStartY = boardTopY + traySeparation * grid.cellSize;
        aboveExtentWorld = SpawnPieceGroup(above, aboveStartY, solutionOffset, upward: true);

        float belowStartY = -traySeparation * grid.cellSize;
        belowExtentWorld = SpawnPieceGroup(below, belowStartY, solutionOffset, upward: false);
    }
    private float SpawnPieceGroup(List<PieceSpawnData> pieces, float startY, Vector2Int solutionOffset, bool upward)
    {
        if (pieces.Count == 0) return 0f;

        int totalRows = Mathf.CeilToInt(pieces.Count / (float)trayColumns);
        int index = 0;
        float rowSpacing = TrayRowSpacing;
        float cellSize = grid.cellSize;

        for (int row = 0; row < totalRows; row++)
        {
            int itemsInRow = Mathf.Min(trayColumns, pieces.Count - row * trayColumns);
            float rowWidth = (itemsInRow - 1) * TrayColumnSpacing;
            float startX = -rowWidth / 2f;
            float rowY = upward ? startY + row * rowSpacing : startY - row * rowSpacing;

            for (int col = 0; col < itemsInRow; col++)
            {
                var spawnData = pieces[index];
                var cells = TetrominoLibrary.GetCells(spawnData.type, spawnData.rotation);
                int w = 0, h = 0;
                foreach (var c in cells) { w = Mathf.Max(w, c.x + 1); h = Mathf.Max(h, c.y + 1); }

                float slotX = startX + col * TrayColumnSpacing;
                float slotY = upward ? rowY : rowY - h * cellSize;

                var go = new GameObject($"Piece_{spawnData.type}");
                go.transform.SetParent(trayParent, false);
                go.transform.localPosition = new Vector3(slotX - w * cellSize / 2f, slotY, 0f);

                var pc = go.AddComponent<PieceController>();
                pc.Init(grid, this, spawnData.type, spawnData.rotation);
                pc.SetSolution(spawnData.solutionOrigin + solutionOffset, spawnData.solutionRotation);
                index++;
            }
        }

        float boardCenterY = gridHeight * cellSize / 2f;
        float lastRowBuffer = cellSize * 4.5f;
        float farthestRowY = upward ? startY + (totalRows - 1) * rowSpacing + lastRowBuffer
                                     : startY - (totalRows - 1) * rowSpacing - lastRowBuffer;
        return Mathf.Abs(farthestRowY - boardCenterY) - (gridHeight * cellSize / 2f);
    }

    public void OnPiecePlaced(PieceController piece)
    {
        if (!grid.IsStageComplete()) return;
        CompleteStage();
    }

    private void CompleteStage()
    {
        ComboCount++;
        float multiplier = 1f + Mathf.Min(ComboCount - 1, comboMultiplierCap) * comboMultiplierStep;

        float remainingAtClear = 0f;
        if (CurrentMode == GameMode.TimeAttack)
        {
            remainingAtClear = TimeRemaining;
            float speedBonus = Mathf.Max(0f, remainingAtClear) * speedBonusRatio;
            TimeRemaining = Mathf.Min(maxTime, remainingAtClear + flatBonusPerStage + speedBonus);
            lastTickSecond = -1;
        }

        scoreManager.AddStageScore(StageNumber, remainingAtClear, multiplier);
        statsManager?.RecordStageCleared();
        statsManager?.RecordCombo(ComboCount);

        int clearedStage = StageNumber;
        StageNumber++;
        SaveProgress(CurrentMode, StageNumber, scoreManager.Score, reachedPieceCountFloor);

        Sfx.StageClear(this);
        OnStageCleared?.Invoke(clearedStage, ComboCount);

        var burstCenter = new Vector3(cachedGridCenterX, gridHeight * grid.cellSize / 2f, 0f);
        Color[] burstColors =
        {
            TetrominoLibrary.GetColor(TetrominoType.I), TetrominoLibrary.GetColor(TetrominoType.O),
            TetrominoLibrary.GetColor(TetrominoType.L), TetrominoLibrary.GetColor(TetrominoType.S),
            TetrominoLibrary.GetColor(TetrominoType.T)
        };
        foreach (var c in burstColors)
            ParticleBurst.Spawn(this, burstCenter, c, count: 8, speed: 4.5f, duration: 0.7f);

        if (interstitialEveryNStages > 0
            && StageNumber % interstitialEveryNStages == 0 && UnityEngine.Random.value < interstitialChance)
            AdManager.Instance?.ShowInterstitial();

        Invoke(nameof(StartStage), 0.4f);
    }

    private void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Sfx.GameOver();
        ClearProgress(CurrentMode);
    }

    //힌트
    public bool HintAvailable => IsPlaying && !IsGameOver;
    public int HintUsesThisStage => hintUsesThisStage;

    public void RequestHintAd()
    {
        if (!IsPlaying || IsGameOver) return;

        if (hintUsesThisStage == 0)
        {
            bool found = ShowHint();
            if (found) hintUsesThisStage++;
        }
        else
        {
            AdManager.Instance?.ShowInterstitial(() =>
            {
                bool found = ShowHint();
                if (found) hintUsesThisStage++;
            });
        }
    }

    private bool ShowHint()
    {
        foreach (Transform child in trayParent)
        {
            var pc = child.GetComponent<PieceController>();
            if (pc != null && !pc.IsPlaced && pc.TryShowHint())
                return true;
        }
        OnMessage?.Invoke("hint_unavailable");
        return false;
    }

    //광고 (타임어택)

    public void RequestTimeBonusAd()
    {
        if (CurrentMode != GameMode.TimeAttack || IsGameOver) return;
        AdManager.Instance?.ShowRewarded(() =>
        {
            TimeRemaining = Mathf.Min(maxTime, TimeRemaining + rewardedTimeBonus);
        });
    }

    public void RequestReviveAd()
    {
        if (CurrentMode != GameMode.TimeAttack || !IsGameOver || HasRevivedThisGame) return;
        AdManager.Instance?.ShowRewarded(() =>
        {
            HasRevivedThisGame = true;
            IsGameOver = false;
            TimeRemaining = reviveTime;
        });
    }

    public void RestartGame()
    {
        BeginGame(GameMode.TimeAttack);
    }

    public void QuitGame()
    {
        if (IsPlaying)
        {
            scoreManager.RecordStage(StageNumber);
            if (!IsGameOver)
            {
                SaveProgress(CurrentMode, StageNumber, scoreManager.Score, reachedPieceCountFloor);
                SaveSnapshotIfPlaying();
            }
        }
        Application.Quit();
    }
}
