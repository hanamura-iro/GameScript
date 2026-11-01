using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuitHandler : MonoBehaviour
{
    [Header("뒤로 가기 안내 문구")]
    public RectTransform canvasRect;      //Canvas
    public TMP_FontAsset font;            //한글 폰트
    public string toastMessage = "뒤로 버튼을 한 번 더 누르면 종료돼요";
    public float confirmTime = 2f;        //이 시간 안에 한 번 더 누르면 종료

    [Header("종료 버튼")]
    public Button quitButton;             //비워두면 이름이 QuitButton인 버튼을 자동으로 찾음
    public TextMeshProUGUI quitLabel;     //비워두면 버튼 안의 글자를 자동으로 찾음
    public bool confirmQuitButton = true; //끄면 한 번만 눌러도 바로 종료
    public string quitText = "게임 종료";
    public string quitConfirmText = "한 번 더 누르면 종료";

    private float backTimer;
    private float buttonTimer;
    private CanvasGroup toast;
    private bool quitting;

    private void Awake()
    {
        if (quitButton == null)
        {
            quitButton = FindButtonByName("QuitButton");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButton);

            if (quitLabel == null)
            {
                quitLabel = quitButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
        else
        {
            Debug.LogWarning("[QuitHandler] 종료 버튼을 찾지 못함 Quit Button 칸에 버튼을 연결 바람");
        }

        RefreshLabel();
    }

    private void Start()
    {
        if (canvasRect != null) BuildToast();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (backTimer > 0f)
            {
                Quit();
                return;
            }

            backTimer = confirmTime;
        }

        if (backTimer > 0f) backTimer -= Time.unscaledDeltaTime;

        if (toast != null)
        {
            toast.alpha = backTimer > 0f ? Mathf.Clamp01(backTimer * 3f) : 0f;
        }

        if (buttonTimer > 0f)
        {
            buttonTimer -= Time.unscaledDeltaTime;
            if (buttonTimer <= 0f) RefreshLabel();
        }
    }
    public void OnQuitButton()
    {
        if (!confirmQuitButton || buttonTimer > 0f)
        {
            Quit();
            return;
        }

        buttonTimer = confirmTime;
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (quitLabel != null)
        {
            quitLabel.text = buttonTimer > 0f ? quitConfirmText : quitText;
        }
    }

    public void Quit()
    {
        if (quitting) return;
        quitting = true;

        if (SaveManager.instance != null) SaveManager.instance.Save();
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_ANDROID
        // 일부 기기에서 Application.Quit만으로는 앱이 백그라운드에 남는 경우가 있어 직접 종료
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call("finishAndRemoveTask");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[QuitHandler] 안드로이드 종료 실패: {e.Message}");
        }

        Application.Quit();
#else
        Application.Quit();
#endif
    }

    private static Button FindButtonByName(string name)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in buttons)
        {
            if (b.gameObject.name == name) return b;
        }
        return null;
    }
    private void BuildToast()
    {
        GameObject rootGo = new GameObject("QuitToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        rootGo.transform.SetParent(canvasRect, false);

        RectTransform rt = rootGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 260f);
        rt.sizeDelta = new Vector2(820f, 110f);

        Image bg = rootGo.GetComponent<Image>();
        bg.color = new Color32(0, 0, 0, 170);
        bg.raycastTarget = false;

        toast = rootGo.GetComponent<CanvasGroup>();
        toast.alpha = 0f;
        toast.blocksRaycasts = false;
        toast.interactable = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rt, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = toastMessage;
        text.fontSize = 40f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        rootGo.transform.SetAsLastSibling();
    }
}