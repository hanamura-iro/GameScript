using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 8;
    public int height = 9;
    public float cellSize = 2.6f;
    public Transform cellParent;

    private static readonly Color EmptyColor = new Color(0.33f, 0.36f, 0.44f, 1f);

    private bool[,] isTarget;
    private bool[,] isFilled;
    private TetrominoType?[,] requiredType;
    private GameObject[,] targetVisuals;
    private SpriteRenderer[,] targetRenderers;

    public bool ColorMatchMode { get; private set; }

    public int TargetCellCount { get; private set; }
    public int FilledCellCount { get; private set; }

    void Awake()
    {
        if (cellParent == null)
        {
            var go = new GameObject("CellParent");
            go.transform.SetParent(transform, false);
            cellParent = go.transform;
        }
        AllocateArrays();
    }

    private void AllocateArrays()
    {
        isTarget = new bool[width, height];
        isFilled = new bool[width, height];
        requiredType = new TetrominoType?[width, height];
        targetVisuals = new GameObject[width, height];
        targetRenderers = new SpriteRenderer[width, height];
    }
    public void Resize(int newWidth, int newHeight)
    {
        ClearBoard();
        width = newWidth;
        height = newHeight;
        AllocateArrays();
    }

    public void SetColorMatchMode(bool on)
    {
        ColorMatchMode = on;
    }

    public void ClearBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                isTarget[x, y] = false;
                isFilled[x, y] = false;
                requiredType[x, y] = null;
                if (targetVisuals[x, y] != null)
                {
                    Destroy(targetVisuals[x, y]);
                    targetVisuals[x, y] = null;
                    targetRenderers[x, y] = null;
                }
            }
        }
        TargetCellCount = 0;
        FilledCellCount = 0;
        previewCells.Clear();
    }
    public void SetTargetShape(List<Vector2Int> cells, Dictionary<Vector2Int, TetrominoType> cellRequiredTypes = null)
    {
        ClearBoard();
        foreach (var c in cells)
        {
            if (!InBounds(c)) continue;
            isTarget[c.x, c.y] = true;
            TargetCellCount++;

            TetrominoType? req = null;
            if (cellRequiredTypes != null && cellRequiredTypes.TryGetValue(c, out var t))
                req = t;
            requiredType[c.x, c.y] = req;

            targetVisuals[c.x, c.y] = CreateTargetCellVisual(c, req);
        }
    }

    private GameObject CreateTargetCellVisual(Vector2Int c, TetrominoType? requiredColorType)
    {
        var go = new GameObject($"TargetCell_{c.x}_{c.y}");
        go.transform.SetParent(cellParent, false);
        go.transform.localPosition = CellToWorld(c);
        go.transform.localScale = new Vector3(cellSize, cellSize, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.TetrisBlock;
        sr.color = EmptyColorForCell(requiredColorType);
        sr.sortingOrder = -1;
        targetRenderers[c.x, c.y] = sr;
        return go;
    }
    private static Color EmptyColorForCell(TetrominoType? requiredColorType)
    {
        if (!requiredColorType.HasValue) return EmptyColor;
        var baseColor = TetrominoLibrary.GetColor(requiredColorType.Value);
        return Color.Lerp(baseColor, Color.black, 0.6f);
    }

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;
    public void ExportTargetCells(List<Vector2Int> outCells, List<int> outRequiredTypes)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!isTarget[x, y]) continue;
                outCells.Add(new Vector2Int(x, y));
                outRequiredTypes.Add(requiredType[x, y].HasValue ? (int)requiredType[x, y].Value : -1);
            }
        }
    }

    public bool CanPlace(Vector2Int origin, Vector2Int[] shape, TetrominoType? pieceType = null)
    {
        foreach (var offset in shape)
        {
            Vector2Int c = origin + offset;
            if (!InBounds(c)) return false;
            if (!isTarget[c.x, c.y]) return false;
            if (isFilled[c.x, c.y]) return false;
            if (ColorMatchMode && requiredType[c.x, c.y].HasValue && requiredType[c.x, c.y].Value != pieceType)
                return false;
        }
        return true;
    }
    public void PlacePiece(Vector2Int origin, Vector2Int[] shape, Color pieceColor)
    {
        foreach (var offset in shape)
        {
            Vector2Int c = origin + offset;
            isFilled[c.x, c.y] = true;
            FilledCellCount++;

            var sr = targetRenderers[c.x, c.y];
            if (sr != null) sr.color = pieceColor;
        }
    }

    public void RemovePiece(Vector2Int origin, Vector2Int[] shape)
    {
        foreach (var offset in shape)
        {
            Vector2Int c = origin + offset;
            if (!InBounds(c)) continue;
            isFilled[c.x, c.y] = false;
            FilledCellCount = Mathf.Max(0, FilledCellCount - 1);

            var sr = targetRenderers[c.x, c.y];
            if (sr != null) sr.color = EmptyColorForCell(requiredType[c.x, c.y]);
        }
    }

    public bool IsStageComplete() => TargetCellCount > 0 && FilledCellCount >= TargetCellCount;

    public void HighlightHint(Vector2Int origin, Vector2Int[] shape)
    {
        StartCoroutine(HintRoutine(origin, shape));
    }

    private System.Collections.IEnumerator HintRoutine(Vector2Int origin, Vector2Int[] shape)
    {
        var renderers = new List<SpriteRenderer>();
        var baseColors = new List<Color>();
        foreach (var offset in shape)
        {
            var c = origin + offset;
            if (!InBounds(c)) continue;
            var sr = targetRenderers[c.x, c.y];
            if (sr != null)
            {
                renderers.Add(sr);
                baseColors.Add(EmptyColorForCell(requiredType[c.x, c.y]));
            }
        }

        var hintColor = new Color(1f, 0.85f, 0.3f, 1f);
        float duration = 2.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pulse = (Mathf.Sin(t * 6f) + 1f) / 2f;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = Color.Lerp(baseColors[i], hintColor, pulse);
            }
            yield return null;
        }

        for (int i = 0; i < renderers.Count; i++)
            if (renderers[i] != null) renderers[i].color = baseColors[i];
    }

    private readonly List<Vector2Int> previewCells = new List<Vector2Int>();
    private static readonly Color PreviewValidColor = new Color(0.45f, 1f, 0.55f, 0.75f);
    private static readonly Color PreviewInvalidColor = new Color(1f, 0.35f, 0.35f, 0.6f);
    public void ShowPlacementPreview(Vector2Int origin, Vector2Int[] shape, bool valid)
    {
        ClearPlacementPreview();
        var color = valid ? PreviewValidColor : PreviewInvalidColor;

        foreach (var offset in shape)
        {
            var c = origin + offset;
            if (!InBounds(c)) continue;
            if (!isTarget[c.x, c.y]) continue;
            if (isFilled[c.x, c.y]) continue;

            var sr = targetRenderers[c.x, c.y];
            if (sr != null)
            {
                sr.color = color;
                previewCells.Add(c);
            }
        }
    }

    public void ClearPlacementPreview()
    {
        foreach (var c in previewCells)
        {
            if (!InBounds(c)) continue;
            if (isFilled[c.x, c.y]) continue; // 채워진 칸은 그 색을 유지해야 하므로 건드리지 않음
            var sr = targetRenderers[c.x, c.y];
            if (sr != null) sr.color = EmptyColorForCell(requiredType[c.x, c.y]);
        }
        previewCells.Clear();
    }

    public Vector3 CellToWorld(Vector2Int c) => new Vector3(c.x * cellSize, c.y * cellSize, 0f);

    public Vector2Int WorldToCell(Vector3 worldPos) =>
        new Vector2Int(Mathf.RoundToInt(worldPos.x / cellSize), Mathf.RoundToInt(worldPos.y / cellSize));
}