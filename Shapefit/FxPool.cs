using System.Collections.Generic;
using UnityEngine;

public static class FxPool
{
    private const int PrewarmCount = 96;

    private static readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
    private static Transform root;

    private static void EnsureRoot()
    {
        if (root != null) return;
        var go = new GameObject("~FxPool");
        Object.DontDestroyOnLoad(go);
        root = go.transform;
        pool.Clear();
        for (int i = 0; i < PrewarmCount; i++)
            pool.Push(CreateNew());
    }

    private static SpriteRenderer CreateNew()
    {
        var go = new GameObject("Fx");
        go.transform.SetParent(root, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RuntimeSprite.Circle;
        go.SetActive(false);
        return sr;
    }

    public static SpriteRenderer Get(Vector3 position, Color color, int sortingOrder)
    {
        EnsureRoot();
        SpriteRenderer sr = null;
        while (pool.Count > 0 && sr == null) sr = pool.Pop();
        if (sr == null) sr = CreateNew();

        var t = sr.transform;
        t.position = position;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        sr.gameObject.SetActive(true);
        return sr;
    }

    public static void Release(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.gameObject.SetActive(false);
        pool.Push(sr);
    }
}
