using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public TetrominoType type;
    public int rotation;

    private GridManager grid;
    private GameManager gameManager;
    private Transform blocksParent;

    private readonly List<SpriteRenderer> outlineRenderers = new List<SpriteRenderer>();

    private Vector3 dragOffset;
    private bool isDragging;

    private Vector2Int? placedOrigin;
    private Vector3 trayHomePosition;

    private Vector2Int correctOrigin;
    private int correctRotation;

    public bool IsPlaced => placedOrigin.HasValue;
    public Vector2Int? PlacedOriginValue => placedOrigin;
    public Vector2Int CorrectOriginValue => correctOrigin;
    public int CorrectRotationValue => correctRotation;

    public void RestorePlaced(Vector2Int origin)
    {
        placedOrigin = origin;
        transform.position = grid.CellToWorld(origin);
        transform.SetParent(grid.transform, true);
    }

    public void SetSolution(Vector2Int origin, int rot)
    {
        correctOrigin = origin;
        correctRotation = rot;
    }

    public void Init(GridManager grid, GameManager gameManager, TetrominoType type, int rotation)
    {
        this.grid = grid;
        this.gameManager = gameManager;
        this.type = type;
        this.rotation = rotation;

        var blocksObj = new GameObject("Blocks");
        blocksObj.transform.SetParent(transform, false);
        blocksParent = blocksObj.transform;

        trayHomePosition = transform.position;
        Rebuild();
    }

    private Vector2Int[] CurrentCells() => TetrominoLibrary.GetCells(type, rotation);

    public void Rebuild()
    {
        for (int i = blocksParent.childCount - 1; i >= 0; i--)
            Destroy(blocksParent.GetChild(i).gameObject);
        outlineRenderers.Clear();

        var cells = CurrentCells();
        var color = TetrominoLibrary.GetColor(type);
        float cellSize = grid.cellSize;

        int maxX = 0, maxY = 0;
        foreach (var c in cells)
        {
            var localPos = new Vector3(c.x * cellSize, c.y * cellSize, 0f);

            var outline = new GameObject($"Outline_{c.x}_{c.y}");
            outline.transform.SetParent(blocksParent, false);
            outline.transform.localPosition = localPos;
            outline.transform.localScale = new Vector3(cellSize * 1.12f, cellSize * 1.12f, 1f);
            var outlineSr = outline.AddComponent<SpriteRenderer>();
            outlineSr.sprite = RuntimeSprite.TetrisBlock;
            outlineSr.color = new Color(1f, 0.92f, 0.35f, 1f);
            outlineSr.sortingOrder = 0;
            outlineSr.enabled = false;
            outlineRenderers.Add(outlineSr);

            var block = new GameObject($"Block_{c.x}_{c.y}");
            block.transform.SetParent(blocksParent, false);
            block.transform.localPosition = localPos;
            block.transform.localScale = new Vector3(cellSize, cellSize, 1f);
            var sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprite.TetrisBlock;
            sr.color = color;
            sr.sortingOrder = 1;

            maxX = Mathf.Max(maxX, c.x);
            maxY = Mathf.Max(maxY, c.y);
        }

        var col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();

        float touchPadding = cellSize * 0.5f;
        col.size = new Vector2((maxX + 1) * cellSize + touchPadding, (maxY + 1) * cellSize + touchPadding);
        col.offset = new Vector2(col.size.x / 2f - cellSize / 2f, col.size.y / 2f - cellSize / 2f);
    }

    public void SetSelected(bool on)
    {
        foreach (var r in outlineRenderers) r.enabled = on;
    }

    public void RotateOnce(bool clockwise = true)
    {
        if (placedOrigin.HasValue) return;
        Sfx.Rotate();
        int count = TetrominoLibrary.RotationCount(type);
        rotation = clockwise ? (rotation + 1) % count : (rotation - 1 + count) % count;
        Rebuild();
        SetSelected(true);
        if (isDragging) UpdatePreview();
    }

    public bool TryShowHint()
    {
        if (placedOrigin.HasValue) return false;

        var solutionCells = TetrominoLibrary.GetCells(type, correctRotation);
        if (!grid.CanPlace(correctOrigin, solutionCells, type)) return false;

        rotation = correctRotation;
        Rebuild();
        grid.HighlightHint(correctOrigin, solutionCells);
        return true;
    }

    public void BeginDrag(Vector3 worldPos)
    {
        if (gameManager.IsGameOver) return;

        gameManager.SelectedPiece = this;
        Sfx.Pickup();

        if (placedOrigin.HasValue)
        {
            grid.RemovePiece(placedOrigin.Value, CurrentCells());
            placedOrigin = null;
            transform.SetParent(gameManager.trayParent, true);
        }

        isDragging = true;
        dragOffset = transform.position - worldPos;
        UpdatePreview();
    }

    public void UpdateDrag(Vector3 worldPos)
    {
        if (!isDragging) return;
        transform.position = worldPos + dragOffset;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var cells = CurrentCells();
        var origin = grid.WorldToCell(transform.position);
        bool valid = FindNearbyValidOrigin(origin, cells).HasValue;
        grid.ShowPlacementPreview(origin, cells, valid);
    }

    public void EndDrag()
    {
        if (!isDragging) return;
        isDragging = false;
        grid.ClearPlacementPreview();
        TryPlace();
    }

    private Vector2Int? FindNearbyValidOrigin(Vector2Int center, Vector2Int[] cells)
    {
        if (grid.CanPlace(center, cells, type)) return center;

        Vector2Int? best = null;
        float bestDist = float.MaxValue;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var candidate = center + new Vector2Int(dx, dy);
                if (!grid.CanPlace(candidate, cells, type)) continue;
                float dist = dx * dx + dy * dy;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }
        }
        return best;
    }

    private void TryPlace()
    {
        var cells = CurrentCells();
        var color = TetrominoLibrary.GetColor(type);
        Vector2Int roundedOrigin = grid.WorldToCell(transform.position);
        var validOrigin = FindNearbyValidOrigin(roundedOrigin, cells);

        if (validOrigin.HasValue)
        {
            var origin = validOrigin.Value;
            grid.PlacePiece(origin, cells, color);
            transform.position = grid.CellToWorld(origin);
            placedOrigin = origin;

            transform.SetParent(grid.transform, true);
            float fillRatio = grid.TargetCellCount > 0 ? (float)grid.FilledCellCount / grid.TargetCellCount : 0f;
            Sfx.Place(fillRatio);
            ParticleBurst.Spawn(gameManager, transform.position, color, count: 6, speed: 2.5f, duration: 0.35f);
            ImpactBurst.Spawn(gameManager, transform.position, color);
            gameManager.ShakeCamera();
            if (gameManager.SelectedPiece == this) gameManager.SelectedPiece = null;
            StartCoroutine(PlacePopAnimation());
            gameManager.OnPiecePlaced(this);
        }
        else
        {          
            Sfx.Invalid();
            gameManager.NotifyMistake();
            transform.position = trayHomePosition;
        }
    }

    private IEnumerator PlacePopAnimation()
    {
        Vector3 baseScale = blocksParent.localScale;
        Vector3 bigPop = baseScale * 1.45f;   
        Vector3 smallDip = baseScale * 0.9f;  
        float t = 0f;

        const float phase1 = 0.08f;
        const float phase2 = 0.08f;
        const float phase3 = 0.10f; 

        while (t < phase1)
        {
            t += Time.deltaTime;
            blocksParent.localScale = Vector3.Lerp(baseScale, bigPop, t / phase1);
            yield return null;
        }
        t = 0f;
        while (t < phase2)
        {
            t += Time.deltaTime;
            blocksParent.localScale = Vector3.Lerp(bigPop, smallDip, t / phase2);
            yield return null;
        }
        t = 0f;
        while (t < phase3)
        {
            t += Time.deltaTime;
            blocksParent.localScale = Vector3.Lerp(smallDip, baseScale, t / phase3);
            yield return null;
        }
        blocksParent.localScale = baseScale;
    }
}