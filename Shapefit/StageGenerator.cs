using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PieceSpawnData
{
    public TetrominoType type;
    public int rotation;
    public Vector2Int solutionOrigin;
    public int solutionRotation;
}

public class StageResult
{
    public List<Vector2Int> targetCells;
    public List<PieceSpawnData> pieces;
    public Dictionary<Vector2Int, TetrominoType> cellRequiredTypes;
}

public static class StageGenerator
{
    public static StageResult Generate(int stageNumber, int maxWidth, int maxHeight,
        int minPieceCount, int baseCount = 3, int capCount = 14, int interval = 5)
    {
        interval = Mathf.Max(1, interval);
        int formulaCount = Mathf.Clamp(baseCount + stageNumber / interval, baseCount, capCount);
        int targetCount = Mathf.Max(formulaCount, minPieceCount);
        int floor = Mathf.Clamp(minPieceCount, baseCount, targetCount);

        for (int count = targetCount; count >= floor; count--)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                var result = TryGenerate(count, maxWidth, maxHeight);
                if (result != null) return result;
            }
        }

        for (int attempt = 0; attempt < 150; attempt++)
        {
            var result = TryGenerate(floor, maxWidth, maxHeight);
            if (result != null) return result;
        }

        return TryGenerate(1, maxWidth, maxHeight);
    }

    private static StageResult TryGenerate(int pieceCount, int maxWidth, int maxHeight)
    {
        var filled = new HashSet<Vector2Int>();
        var cellType = new Dictionary<Vector2Int, TetrominoType>();
        var placedTypes = new List<TetrominoType>();
        var placedOrigins = new List<Vector2Int>();
        var placedRotations = new List<int>();
        var pool = TetrominoLibrary.PlayableTypes;

        var firstType = pool[Random.Range(0, pool.Length)];
        var firstRot = Random.Range(0, TetrominoLibrary.RotationCount(firstType));
        var firstCells = TetrominoLibrary.GetCells(firstType, firstRot);
        foreach (var c in firstCells) { filled.Add(c); cellType[c] = firstType; }
        placedTypes.Add(firstType);
        placedOrigins.Add(Vector2Int.zero);
        placedRotations.Add(firstRot);

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in firstCells)
        {
            minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
            minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
        }
        if (maxX - minX + 1 > maxWidth || maxY - minY + 1 > maxHeight) return null;

        int safety = 0;
        while (placedTypes.Count < pieceCount && safety < 400)
        {
            safety++;
            var bounds = new BoundsInt2(minX, minY, maxX, maxY);
            if (TryAddOnePiece(filled, cellType, placedTypes, placedOrigins, placedRotations, pool,
                    maxWidth, maxHeight, ref bounds))
            {
                minX = bounds.minX; minY = bounds.minY;
                maxX = bounds.maxX; maxY = bounds.maxY;
            }
        }

        if (placedTypes.Count < pieceCount) return null;

        var targetCells = new List<Vector2Int>();
        foreach (var c in filled) targetCells.Add(new Vector2Int(c.x - minX, c.y - minY));

        var normOffset = new Vector2Int(-minX, -minY);
        var normalizedCellTypes = new Dictionary<Vector2Int, TetrominoType>();
        foreach (var kvp in cellType)
            normalizedCellTypes[kvp.Key + normOffset] = kvp.Value;

        var pieces = new List<PieceSpawnData>();
        for (int i = 0; i < placedTypes.Count; i++)
        {
            pieces.Add(new PieceSpawnData
            {
                type = placedTypes[i],
                rotation = Random.Range(0, TetrominoLibrary.RotationCount(placedTypes[i])),
                solutionOrigin = placedOrigins[i] + normOffset,
                solutionRotation = placedRotations[i],
            });
        }
        Shuffle(pieces);

        return new StageResult { targetCells = targetCells, pieces = pieces, cellRequiredTypes = normalizedCellTypes };
    }

    private struct BoundsInt2
    {
        public int minX, minY, maxX, maxY;
        public BoundsInt2(int minX, int minY, int maxX, int maxY)
        {
            this.minX = minX; this.minY = minY; this.maxX = maxX; this.maxY = maxY;
        }
    }

    private static bool TryAddOnePiece(HashSet<Vector2Int> filled, Dictionary<Vector2Int, TetrominoType> cellType,
        List<TetrominoType> placedTypes, List<Vector2Int> placedOrigins, List<int> placedRotations,
        TetrominoType[] pool, int maxWidth, int maxHeight, ref BoundsInt2 bounds)
    {
        var filledList = new List<Vector2Int>(filled);
        var anchor = filledList[Random.Range(0, filledList.Count)];
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        var target = anchor + dirs[Random.Range(0, dirs.Length)];
        if (filled.Contains(target)) return false;

        var typesShuffled = new List<TetrominoType>(pool);
        Shuffle(typesShuffled);

        foreach (var type in typesShuffled)
        {
            int rotCount = TetrominoLibrary.RotationCount(type);
            var rotOrder = new List<int>();
            for (int r = 0; r < rotCount; r++) rotOrder.Add(r);
            Shuffle(rotOrder);

            foreach (var rot in rotOrder)
            {
                var shape = TetrominoLibrary.GetCells(type, rot);
                var cellOrder = new List<int>();
                for (int i = 0; i < shape.Length; i++) cellOrder.Add(i);
                Shuffle(cellOrder);

                foreach (var idx in cellOrder)
                {
                    var origin = target - shape[idx];

                    bool ok = true;
                    int newMinX = bounds.minX, newMinY = bounds.minY, newMaxX = bounds.maxX, newMaxY = bounds.maxY;
                    foreach (var offset in shape)
                    {
                        var c = origin + offset;
                        if (filled.Contains(c)) { ok = false; break; }
                        newMinX = Mathf.Min(newMinX, c.x); newMaxX = Mathf.Max(newMaxX, c.x);
                        newMinY = Mathf.Min(newMinY, c.y); newMaxY = Mathf.Max(newMaxY, c.y);
                    }
                    if (ok && (newMaxX - newMinX + 1 > maxWidth || newMaxY - newMinY + 1 > maxHeight))
                        ok = false;

                    if (ok)
                    {
                        foreach (var offset in shape)
                        {
                            var c = origin + offset;
                            filled.Add(c);
                            cellType[c] = type;
                        }
                        placedTypes.Add(type);
                        placedOrigins.Add(origin);
                        placedRotations.Add(rot);
                        bounds.minX = newMinX; bounds.minY = newMinY;
                        bounds.maxX = newMaxX; bounds.maxY = newMaxY;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}