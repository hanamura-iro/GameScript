using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanel : MonoBehaviour, IPointerClickHandler
{
    public static SettingsPanel instance;

    [Header("UI 연결")]
    public RectTransform panel;
    public Button vibrationButton;
    public TextMeshProUGUI vibrationLabel;
    public Button sfxButton;
    public TextMeshProUGUI sfxLabel;
    public Button bgmButton;
    public TextMeshProUGUI bgmLabel;
    public Button resetButton;
    public TextMeshProUGUI resetLabel;
    public Button closeButton;

    [Header("표시")]
    public Color onColor = new Color32(120, 210, 140, 255);
    public Color offColor = new Color32(170, 170, 170, 255);
    public float resetConfirmTime = 3f;

    private CanvasGroup root;
    private bool isOpen;
    private float resetConfirmTimer;

    private void Awake()
    {
        instance = this;
        root = GetComponent<CanvasGroup>();

        if (vibrationButton != null) vibrationButton.onClick.AddListener(ToggleVibration);
        if (sfxButton != null) sfxButton.onClick.AddListener(ToggleSfx);
        if (bgmButton != null) bgmButton.onClick.AddListener(ToggleBgm);
        if (resetButton != null) resetButton.onClick.AddListener(OnResetPressed);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        Hide();
        Refresh();
    }

    private void Update()
    {
        if (resetConfirmTimer > 0f)
        {
            resetConfirmTimer -= Time.unscaledDeltaTime;
            if (resetConfirmTimer <= 0f) RefreshReset();
        }
    }

    public void Open()
    {
        if (isOpen) return;
        Refresh();
        StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (!isOpen) return;
        resetConfirmTimer = 0f;
        RefreshReset();
        StartCoroutine(CloseRoutine());
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.pointerCurrentRaycast.gameObject == gameObject)
        {
            Close();
        }
    }

    private void ToggleVibration()
    {
        Haptics.Enabled = !Haptics.Enabled;
        PlayerPrefs.Save();

        if (Haptics.Enabled) Haptics.Vibrate(30, 150);

        Refresh();
    }

    private void ToggleSfx()
    {
        SoundSettings.SfxEnabled = !SoundSettings.SfxEnabled;
        Refresh();
    }

    private void ToggleBgm()
    {
        SoundSettings.BgmEnabled = !SoundSettings.BgmEnabled;
        Refresh();
    }

    private void OnResetPressed()
    {
        if (resetConfirmTimer > 0f)
        {
            if (SaveManager.instance != null) SaveManager.instance.ResetAll();
            return;
        }

        resetConfirmTimer = resetConfirmTime;
        RefreshReset();
    }

    private void Refresh()
    {
        SetState(vibrationButton, vibrationLabel, "진동", Haptics.Enabled);
        SetState(sfxButton, sfxLabel, "효과음", SoundSettings.SfxEnabled);
        SetState(bgmButton, bgmLabel, "배경음", SoundSettings.BgmEnabled);
        RefreshReset();
    }

    private void SetState(Button button, TextMeshProUGUI label, string name, bool on)
    {
        if (label != null) label.text = $"{name}: {(on ? "켜짐" : "꺼짐")}";
        if (button != null && button.image != null) button.image.color = on ? onColor : offColor;
    }

    private void RefreshReset()
    {
        if (resetLabel != null)
        {
            resetLabel.text = resetConfirmTimer > 0f ? "한 번 더 누르면 초기화" : "데이터 초기화";
        }
    }

    private IEnumerator OpenRoutine()
    {
        isOpen = true;
        root.blocksRaycasts = true;
        root.interactable = true;

        float t = 0f;
        const float duration = 0.3f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            root.alpha = Mathf.Clamp01(t / 0.15f);
            if (panel != null) panel.localScale = Vector3.one * EaseOutBack(k);
            yield return null;
        }

        root.alpha = 1f;
        if (panel != null) panel.localScale = Vector3.one;
    }

    private IEnumerator CloseRoutine()
    {
        isOpen = false;
        root.interactable = false;

        float t = 0f;
        const float duration = 0.15f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            root.alpha = 1f - k;
            if (panel != null) panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
            yield return null;
        }

        Hide();
    }

    private void Hide()
    {
        isOpen = false;
        root.alpha = 0f;
        root.blocksRaycasts = false;
        root.interactable = false;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}