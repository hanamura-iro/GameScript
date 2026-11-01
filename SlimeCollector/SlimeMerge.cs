using System.Collections.Generic;
using UnityEngine;

public class SlimeMerge : MonoBehaviour
{
    public static SlimeMerge instance;

    [Header("합치기 연출")]
    public Color effectColor = new Color32(180, 240, 255, 255);
    public Color newDiscoveryColor = new Color32(255, 220, 80, 255);
    public Vector3 effectOffset = Vector3.zero; 
    public string effectSortingLayer = "Default";
    public int effectSortingOrder = 500;

    private readonly List<Collider2D> overlapResults = new List<Collider2D>();

    private void Awake()
    {
        instance = this;
    }
    public bool TryMerge(SlimeBase dragged)
    {
        if (dragged == null) return false;

        int nextType = (int)dragged.Slimetype + 1;
        if (nextType >= GameManager.instance.Slimes.Length) return false;

        Collider2D col = dragged.GetComponent<Collider2D>();
        if (col == null) return false;

        Physics2D.SyncTransforms();

        overlapResults.Clear();
        col.Overlap(new ContactFilter2D().NoFilter(), overlapResults);

        SlimeBase target = null;
        float bestDist = float.MaxValue;

        foreach (Collider2D hit in overlapResults)
        {
            SlimeBase other = hit.GetComponent<SlimeBase>();
            if (other == null || other == dragged) continue;
            if (other.Slimetype != dragged.Slimetype) continue;

            float dist = (other.transform.position - dragged.transform.position).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                target = other;
            }
        }

        if (target == null) return false;

        return MergePair(dragged, target) != null;
    }

    public GameObject MergePair(SlimeBase from, SlimeBase to)
    {
        if (from == null || to == null || from == to) return null;
        if (from.Slimetype != to.Slimetype) return null;

        GameObject[] slimes = GameManager.instance.Slimes;
        int nextType = (int)to.Slimetype + 1;
        if (nextType >= slimes.Length) return null;

        Vector3 spawnPos = to.transform.position;

        SlimeMove toMove = to.GetComponent<SlimeMove>();
        if (toMove != null)
        {
            spawnPos.y = Mathf.Clamp(spawnPos.y, toMove.minY, toMove.maxY);
        }

        GameManager.instance.SlimeData.Remove(from.gameObject);
        GameManager.instance.SlimeData.Remove(to.gameObject);
        Destroy(from.gameObject);
        Destroy(to.gameObject);

        GameObject merged = Instantiate(slimes[nextType], spawnPos, Quaternion.identity);
        GameManager.instance.SlimeData.Add(merged);
        merged.AddComponent<SpawnPop>();

        bool isNew = BookManager.instance.Discover(nextType);

        MergeEffect.Play(
            spawnPos + effectOffset,
            isNew ? newDiscoveryColor : effectColor,
            isNew,
            effectSortingLayer,
            effectSortingOrder);

        if (GameFeel.instance != null) GameFeel.instance.OnMerge(nextType, isNew);

        if (isNew && DiscoveryPopup.instance != null)
        {
            DiscoveryPopup.instance.Show(nextType);
        }

        return merged;
    }
}