using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(RectTransform))]
public class AutoAnchorTool : MonoBehaviour
{
    [Tooltip("화면 가장자리에서 이 비율 안쪽에 있으면 그 가장자리에 붙임 (0.35 = 35%)")]
    [Range(0.2f, 0.45f)]
    public float edgeRatio = 0.35f;

    [ContextMenu("자식 UI 앵커 자동 설정")]
    private void ApplyFromComponent()
    {
#if UNITY_EDITOR
        Apply((RectTransform)transform, edgeRatio);
#endif
    }

#if UNITY_EDITOR
    [MenuItem("Tools/UI 앵커 자동 설정")]
    private static void ApplyFromMenu()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("Hierarchy에서 Canvas를 먼저 선택");
            return;
        }

        RectTransform rt = selected.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogWarning("선택한 오브젝트가 UI가 아닙니다. Canvas를 선택");
            return;
        }

        AutoAnchorTool tool = selected.GetComponent<AutoAnchorTool>();
        Apply(rt, tool != null ? tool.edgeRatio : 0.35f);
    }

    private static void Apply(RectTransform parent, float edgeRatio)
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("플레이 중에는 실행할 수 없음, 플레이를 멈추고 실행");
            return;
        }

        Rect area = parent.rect;
        Vector3[] corners = new Vector3[4];
        int changed = 0;

        foreach (Transform child in parent)
        {
            RectTransform rt = child as RectTransform;
            if (rt == null) continue;

            if (rt.anchorMin != rt.anchorMax) continue;

            rt.GetWorldCorners(corners);
            Vector3 centerWorld = (corners[0] + corners[2]) * 0.5f;
            Vector2 local = parent.InverseTransformPoint(centerWorld);

            float nx = (local.x - area.xMin) / area.width;
            float ny = (local.y - area.yMin) / area.height;

            float ax = nx < edgeRatio ? 0f : (nx > 1f - edgeRatio ? 1f : 0.5f);
            float ay = ny < edgeRatio ? 0f : (ny > 1f - edgeRatio ? 1f : 0.5f);
            Vector2 anchor = new Vector2(ax, ay);

            if (rt.anchorMin == anchor) continue;

            Undo.RecordObject(rt, "앵커 자동 설정");

            Vector3 worldPos = rt.position;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.position = worldPos;

            EditorUtility.SetDirty(rt);
            changed++;
            Debug.Log($"{rt.name}: {AnchorName(anchor)}에 고정");
        }

        Debug.Log($"앵커 자동 설정 완료: {changed}개 변경");
    }

    private static string AnchorName(Vector2 a)
    {
        string v = a.y == 1f ? "위" : (a.y == 0f ? "아래" : "가운데");
        string h = a.x == 0f ? "왼쪽" : (a.x == 1f ? "오른쪽" : "가운데");
        if (v == "가운데" && h == "가운데") return "정중앙";
        return $"{h} {v}";
    }
#endif
}