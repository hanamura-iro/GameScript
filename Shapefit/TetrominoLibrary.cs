using System.Collections.Generic;
using UnityEngine;

public enum TetrominoType { I, O, T, S, Z, J, L }

public static class TetrominoLibrary
{
    private static readonly Dictionary<TetrominoType, Vector2Int[]> BaseShapes =
        new Dictionary<TetrominoType, Vector2Int[]>
    {
        { TetrominoType.I, new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) } },
        { TetrominoType.O, new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) } },
        { TetrominoType.T, new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) } },
        { TetrominoType.S, new[] { new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) } },
        { TetrominoType.Z, new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) } },
        { TetrominoType.J, new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) } },
        { TetrominoType.L, new[] { new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) } },
    };

    public static readonly TetrominoType[] PlayableTypes =
        { TetrominoType.I, TetrominoType.O, TetrominoType.L, TetrominoType.S, TetrominoType.T };

    public static int RotationCount(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.O: return 1;
            case TetrominoType.I:
            case TetrominoType.S:
            case TetrominoType.Z: return 2;
            default: return 4;
        }
    }

    public static Vector2Int[] GetCells(TetrominoType type, int rotation)
    {
        rotation = ((rotation % 4) + 4) % 4;
        Vector2Int[] cells = (Vector2Int[])BaseShapes[type].Clone();
        for (int i = 0; i < rotation; i++)
            cells = RotateCW(cells);
        return Normalize(cells);
    }

    private static Vector2Int[] RotateCW(Vector2Int[] cells)
    {
        var result = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            result[i] = new Vector2Int(cells[i].y, -cells[i].x);
        return result;
    }

    private static Vector2Int[] Normalize(Vector2Int[] cells)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in cells)
        {
            minX = Mathf.Min(minX, c.x);
            minY = Mathf.Min(minY, c.y);
        }
        var result = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
            result[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
        return result;
    }

    public static Color GetColor(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I: return new Color(0.20f, 0.80f, 0.90f);
            case TetrominoType.O: return new Color(0.95f, 0.85f, 0.20f);
            case TetrominoType.T: return new Color(0.90f, 0.30f, 0.85f); // 마젠타
            case TetrominoType.S: return new Color(0.30f, 0.90f, 0.40f);
            case TetrominoType.Z: return new Color(0.90f, 0.30f, 0.30f);
            case TetrominoType.J: return new Color(0.30f, 0.40f, 0.90f);
            case TetrominoType.L: return new Color(0.95f, 0.60f, 0.20f);
            default: return Color.white;
        }
    }
}
