using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BoostButton : MonoBehaviour
{
    public BoostManager.BoostType type;
    public TextMeshProUGUI label;
    public string readyText = "";      // 비우면 자동 문구

    private Button button;
    private int shownKey = int.MinValue;   // -1 = 대기 상태, 0 이상 = 남은 초

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        BoostManager boost = BoostManager.instance;
        if (boost == null) return;

        float remain = boost.Remaining(type);
        bool active = remain > 0f;

        int key = active ? Mathf.CeilToInt(remain) : -1;
        if (label != null && key != shownKey)
        {
            shownKey = key;
            label.text = active ? FormatTime(key) : GetReadyText(boost);
        }

        button.interactable = !active;
    }

    private string GetReadyText(BoostManager boost)
    {
        if (!string.IsNullOrEmpty(readyText)) return readyText;

        int minutes = Mathf.RoundToInt(boost.DurationSeconds(type) / 60f);
        return type == BoostManager.BoostType.GoldDouble
            ? $"광고 보고 골드 2배 ({minutes}분)"
            : $"광고 보고 자동 터치 ({minutes}분)";
    }

    private void OnClick()
    {
        if (AdManager.instance == null) return;

        AdManager.instance.ShowRewarded(() =>
        {
            if (BoostManager.instance != null) BoostManager.instance.Activate(type);
        });
    }

    private static string FormatTime(int totalSeconds)
    {
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}