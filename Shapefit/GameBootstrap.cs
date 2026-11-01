using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class GameBootstrap
{
    private static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color AccentColor = new Color(0.36f, 0.55f, 0.98f);

    //애드몹 실제 광고 단위 ID
    private const string RealInterstitialAdUnitId = "ca-app-pub-5339301765152973/7570925291";
    private const string RealRewardedAdUnitId = "ca-app-pub-5339301765152973/5575317340";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSetup()
    {
        Debug.Log("GameBootstrap.AutoSetup() 실행됨");

        if (Object.FindFirstObjectByType<GameManager>() != null)
        {
            Debug.Log("GameManager가 씬에 있어서 건너뜀");
            return;
        }

        try
        {
            BuildScene();
            Debug.Log("씬 자동 생성 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError("자동 생성 중 오류 : " + e);
        }
    }

    private static void BuildScene()
    {
        //프레임레이트
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        //카메라
        var camObj = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
        if (camObj.GetComponent<Camera>() == null) camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";
        var cam = camObj.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);

        //이벤트 시스템
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var routerObj = new GameObject("TouchInputRouter");
        var router = routerObj.AddComponent<TouchInputRouter>();

        //그리드
        var gridObj = new GameObject("Grid");
        var grid = gridObj.AddComponent<GridManager>();

        //점수 / 통계
        var scoreObj = new GameObject("ScoreManager");
        var scoreManager = scoreObj.AddComponent<ScoreManager>();
        var statsObj = new GameObject("StatsManager");
        var statsManager = statsObj.AddComponent<StatsManager>();

        var trayObj = new GameObject("Tray");

        //게임 매니저
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();
        gm.grid = grid;
        gm.scoreManager = scoreManager;
        gm.statsManager = statsManager;
        gm.trayParent = trayObj.transform;
        gm.gridWidth = grid.width;
        gm.gridHeight = grid.height;
        gm.mainCam = cam;

        router.gameManager = gm;

        //한글 지원 폰트
        Font koreanFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "AppleGothic", "Noto Sans CJK KR", "NanumGothic", "Arial" }, 48);

        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        //애드몹 매니저
        var adObj = new GameObject("AdManager");
        var adManager = adObj.AddComponent<AdManager>();
        adManager.interstitialAdUnitId = RealInterstitialAdUnitId;
        adManager.rewardedAdUnitId = RealRewardedAdUnitId;

        //게임플레이 HUD
        var gameplayHudRoot = new GameObject("GameplayHUD", typeof(RectTransform));
        gameplayHudRoot.transform.SetParent(canvasObj.transform, false);
        var hudRootRt = gameplayHudRoot.GetComponent<RectTransform>();
        hudRootRt.anchorMin = Vector2.zero;
        hudRootRt.anchorMax = Vector2.one;
        hudRootRt.offsetMin = Vector2.zero;
        hudRootRt.offsetMax = Vector2.zero;

        var infoPanel = new GameObject("InfoPanel", typeof(RectTransform));
        infoPanel.transform.SetParent(gameplayHudRoot.transform, false);
        var infoRt = infoPanel.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0, 1);
        infoRt.anchorMax = new Vector2(0, 1);
        infoRt.pivot = new Vector2(0, 1);
        infoRt.anchoredPosition = new Vector2(24, -24);
        infoRt.sizeDelta = new Vector2(360, 300);
        var infoImg = infoPanel.AddComponent<Image>();
        infoImg.sprite = RuntimeSprite.RoundedPanel;
        infoImg.type = Image.Type.Sliced;
        infoImg.color = PanelColor;

        var scoreText = CreateText(infoPanel.transform, "ScoreText", koreanFont, new Vector2(24, -20), 40);
        var highScoreText = CreateText(infoPanel.transform, "HighScoreText", koreanFont, new Vector2(24, -70), 28);
        var stageText = CreateText(infoPanel.transform, "StageText", koreanFont, new Vector2(24, -120), 34);
        var timerText = CreateText(infoPanel.transform, "TimerText", koreanFont, new Vector2(24, -180), 34);
        var comboText = CreateText(infoPanel.transform, "ComboText", koreanFont, new Vector2(24, -240), 32);
        comboText.color = new Color(1f, 0.85f, 0.3f);
        comboText.gameObject.SetActive(false);

        //회전 버튼 2개
        var rotateLeftObj = CreateButton(gameplayHudRoot.transform, "\u21BA", koreanFont); // ↺
        var rotateLeftRt = rotateLeftObj.GetComponent<RectTransform>();
        rotateLeftRt.anchorMin = new Vector2(0f, 0.5f);
        rotateLeftRt.anchorMax = new Vector2(0f, 0.5f);
        rotateLeftRt.pivot = new Vector2(0f, 0.5f);
        rotateLeftRt.sizeDelta = new Vector2(140, 140);
        rotateLeftRt.anchoredPosition = new Vector2(20, 0);
        rotateLeftObj.GetComponentInChildren<Text>().fontSize = 64;
        rotateLeftObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        rotateLeftObj.GetComponent<Button>().onClick.AddListener(() => gm.RotateSelectedPiece(false));

        var rotateRightObj = CreateButton(gameplayHudRoot.transform, "\u21BB", koreanFont); // ↻
        var rotateRightRt = rotateRightObj.GetComponent<RectTransform>();
        rotateRightRt.anchorMin = new Vector2(1f, 0.5f);
        rotateRightRt.anchorMax = new Vector2(1f, 0.5f);
        rotateRightRt.pivot = new Vector2(1f, 0.5f);
        rotateRightRt.sizeDelta = new Vector2(140, 140);
        rotateRightRt.anchoredPosition = new Vector2(-20, 0);
        rotateRightObj.GetComponentInChildren<Text>().fontSize = 64;
        rotateRightObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        rotateRightObj.GetComponent<Button>().onClick.AddListener(() => gm.RotateSelectedPiece(true));

        //시간 추가 광고 버튼(타임어택 전용)
        var adTimeButtonObj = CreateButton(gameplayHudRoot.transform, "광고보고 +시간", koreanFont);
        adTimeButtonObj.name = "AdTimeButton";
        var adTimeRt = adTimeButtonObj.GetComponent<RectTransform>();
        adTimeRt.anchorMin = new Vector2(0, 0);
        adTimeRt.anchorMax = new Vector2(0, 0);
        adTimeRt.pivot = new Vector2(0, 0);
        adTimeRt.sizeDelta = new Vector2(260, 90);
        adTimeRt.anchoredPosition = new Vector2(28, 28);
        adTimeButtonObj.GetComponent<Image>().color = new Color(0.3f, 0.75f, 0.45f);
        adTimeButtonObj.GetComponent<Button>().onClick.AddListener(gm.RequestTimeBonusAd);
        var adTimeButtonText = adTimeButtonObj.GetComponentInChildren<Text>();
        adTimeButtonText.resizeTextForBestFit = true;
        adTimeButtonText.resizeTextMinSize = 16;
        adTimeButtonText.resizeTextMaxSize = 38;

        //힌트 광고 버튼(두 모드 공통)
        var hintButtonObj = CreateButton(gameplayHudRoot.transform, "광고보고 힌트", koreanFont);
        hintButtonObj.name = "HintButton";
        var hintRt = hintButtonObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0, 0);
        hintRt.anchorMax = new Vector2(0, 0);
        hintRt.pivot = new Vector2(0, 0);
        hintRt.sizeDelta = new Vector2(260, 90);
        hintRt.anchoredPosition = new Vector2(28, 150);
        hintButtonObj.GetComponent<Image>().color = new Color(0.85f, 0.65f, 0.25f);
        hintButtonObj.GetComponent<Button>().onClick.AddListener(gm.RequestHintAd);
        var hintButtonText = hintButtonObj.GetComponentInChildren<Text>();
        hintButtonText.resizeTextForBestFit = true;
        hintButtonText.resizeTextMinSize = 16;
        hintButtonText.resizeTextMaxSize = 38;

        //팝업, 오버레이
        var (panel, panelText, reviveObj, reviveText) = CreateGameOverPanel(canvasObj.transform, koreanFont, gm);
        var restartButtonText = panel.transform.Find("Card/RestartButton")?.GetComponentInChildren<Text>();
        var flashOverlay = CreateFlashOverlay(canvasObj.transform);
        var (stageClearPopup, stageClearText) = CreateStageClearPopup(canvasObj.transform, koreanFont);
        var (toastPopup, toastText) = CreateToastPopup(canvasObj.transform, koreanFont);
        var settings = CreateSettingsPanel(canvasObj.transform, koreanFont, statsManager, gm);
        var (customConfigPanel, customConfigBaseText, customConfigIntervalText) = CreateCustomConfigPanel(canvasObj.transform, koreanFont, gm);

        //메인 메뉴
        var (menuPanel, bestTimeAttackText, bestNormalText, normalTitleText, normalDescText, timeAttackTitleText, timeAttackDescText,
            bestBigBoardText, bigBoardTitleText, bigBoardDescText, bestColorMatchText, colorMatchTitleText, colorMatchDescText,
            bestCustomText, customTitleText, customDescText,
            normalCardRefs, timeAttackCardRefs, bigBoardCardRefs, colorMatchCardRefs, customCardRefs)
            = CreateMainMenu(canvasObj.transform, koreanFont, gm, () =>
            {
                customConfigPanel.transform.SetAsLastSibling();
                customConfigPanel.SetActive(true);
            });

        //설정 버튼
        var settingsButtonObj = CreateSmallButton(canvasObj.transform, "\u2699", koreanFont,
            new Vector2(1, 1), new Vector2(-28, -28));
        settingsButtonObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            settings.panel.transform.SetAsLastSibling();
            settings.panel.SetActive(true);
        });

        //HUD 연결
        var hudObj = new GameObject("HUD");
        var hud = hudObj.AddComponent<HUDController>();
        hud.gameManager = gm;
        hud.scoreManager = scoreManager;
        hud.statsManager = statsManager;
        hud.mainMenuPanel = menuPanel;
        hud.menuBestTimeAttackText = bestTimeAttackText;
        hud.menuBestNormalText = bestNormalText;
        hud.menuNormalTitleText = normalTitleText;
        hud.menuNormalDescText = normalDescText;
        hud.menuTimeAttackTitleText = timeAttackTitleText;
        hud.menuTimeAttackDescText = timeAttackDescText;
        hud.menuBestBigBoardText = bestBigBoardText;
        hud.menuBigBoardTitleText = bigBoardTitleText;
        hud.menuBigBoardDescText = bigBoardDescText;
        hud.menuBestColorMatchText = bestColorMatchText;
        hud.menuColorMatchTitleText = colorMatchTitleText;
        hud.menuColorMatchDescText = colorMatchDescText;
        hud.menuBestCustomText = bestCustomText;
        hud.menuCustomTitleText = customTitleText;
        hud.menuCustomDescText = customDescText;
        hud.modeCardRefs = new System.Collections.Generic.List<GameBootstrap.ModeCardRefs>
        {
            normalCardRefs, timeAttackCardRefs, bigBoardCardRefs, colorMatchCardRefs, customCardRefs
        };
        hud.gameplayHudRoot = gameplayHudRoot;
        hud.scoreText = scoreText;
        hud.highScoreText = highScoreText;
        hud.stageText = stageText;
        hud.timerText = timerText;
        hud.comboText = comboText;
        hud.adTimeButtonObj = adTimeButtonObj;
        hud.adTimeButtonText = adTimeButtonText;
        hud.hintButton = hintButtonObj.GetComponent<Button>();
        hud.hintButtonText = hintButtonText;
        hud.gameOverPanel = panel;
        hud.gameOverText = panelText;
        hud.restartButtonText = restartButtonText;
        hud.reviveButtonObj = reviveObj;
        hud.reviveButtonText = reviveText;
        hud.flashOverlay = flashOverlay;
        hud.stageClearPopup = stageClearPopup;
        hud.stageClearText = stageClearText;
        hud.toastPopup = toastPopup;
        hud.toastText = toastText;
        hud.settingsPanel = settings.panel;
        hud.settingsTitleText = settings.title;
        hud.soundToggleText = settings.soundText;
        hud.langToggleText = settings.langText;
        hud.closeButtonText = settings.closeText;
        hud.statsText = settings.statsText;
        hud.quitButtonText = settings.quitText;
        hud.mainMenuButtonText = settings.mainMenuText;
        hud.cellSizeText = settings.zoomText;
        hud.customBaseText = customConfigBaseText;
        hud.customIntervalText = customConfigIntervalText;
    }

    private static Text CreateText(Transform parent, string name, Font font, Vector2 topLeftOffset, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(320, 60);
        rt.anchoredPosition = topLeftOffset;

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.text = name;
        return text;
    }

    private static GameObject CreateButton(Transform parent, string label, Font font)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(220, 110);
        rt.anchoredPosition = new Vector2(-28, 28);

        var img = go.AddComponent<Image>();
        img.sprite = RuntimeSprite.RoundedPanel;
        img.type = Image.Type.Sliced;
        img.color = AccentColor;
        go.AddComponent<Button>();

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 38;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        return go;
    }

    private static GameObject CreateSmallButton(Transform parent, string label, Font font, Vector2 anchor, Vector2 anchoredPos)
    {
        var go = new GameObject("SettingsButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(120, 120);
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.sprite = RuntimeSprite.RoundedPanel;
        img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.12f);
        go.AddComponent<Button>();

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 58;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        return go;
    }

    private static Image CreateFlashOverlay(Transform parent)
    {
        var go = new GameObject("FlashOverlay", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        return img;
    }

    private static (GameObject popup, Text text) CreateStageClearPopup(Transform parent, Font font)
    {
        var go = new GameObject("StageClearPopup", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(700, 220);

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 56;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.85f, 0.3f);
        text.text = "Stage Clear";

        go.SetActive(false);
        return (go, text);
    }

    private static (GameObject popup, Text text) CreateToastPopup(Transform parent, Font font)
    {
        var go = new GameObject("ToastPopup", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -220);
        rt.sizeDelta = new Vector2(820, 200);

        var bg = go.AddComponent<Image>();
        bg.sprite = RuntimeSprite.RoundedPanel;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(32, 20);
        trt.offsetMax = new Vector2(-32, -20);
        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = "";

        go.SetActive(false);
        return (go, text);
    }

    private static (GameObject panel, Text text, GameObject reviveBtn, Text reviveText) CreateGameOverPanel(Transform parent, Font font, GameManager gm)
    {
        var panel = new GameObject("GameOverPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(panel.transform, false);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(680, 640);
        crt.anchoredPosition = Vector2.zero;
        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = RuntimeSprite.RoundedPanel;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.13f, 0.14f, 0.19f, 0.98f);

        var textObj = new GameObject("ResultText", typeof(RectTransform));
        textObj.transform.SetParent(card.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0, 120);
        trt.sizeDelta = new Vector2(600, 340);
        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 50;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "Game Over";

        var reviveObj = CreateButton(card.transform, "광고보고 부활 함", font);
        reviveObj.name = "ReviveButton";
        var reviveRt = reviveObj.GetComponent<RectTransform>();
        reviveRt.anchorMin = new Vector2(0.5f, 0f);
        reviveRt.anchorMax = new Vector2(0.5f, 0f);
        reviveRt.pivot = new Vector2(0.5f, 0f);
        reviveRt.anchoredPosition = new Vector2(0, 170);
        reviveRt.sizeDelta = new Vector2(400, 100);
        reviveObj.GetComponent<Image>().color = new Color(0.3f, 0.75f, 0.45f);
        reviveObj.GetComponent<Button>().onClick.AddListener(gm.RequestReviveAd);
        var reviveText = reviveObj.GetComponentInChildren<Text>();

        var restartObj = CreateButton(card.transform, "다시 시작", font);
        restartObj.name = "RestartButton";
        var restartRt = restartObj.GetComponent<RectTransform>();
        restartRt.anchorMin = new Vector2(0.5f, 0f);
        restartRt.anchorMax = new Vector2(0.5f, 0f);
        restartRt.pivot = new Vector2(0.5f, 0f);
        restartRt.anchoredPosition = new Vector2(0, 40);
        restartRt.sizeDelta = new Vector2(280, 110);
        restartObj.GetComponent<Image>().color = AccentColor;
        restartObj.GetComponent<Button>().onClick.AddListener(gm.RestartGame);

        panel.SetActive(false);
        return (panel, text, reviveObj, reviveText);
    }

    private static (GameObject panel, Text title, Text soundText, Text langText, Text closeText, Text statsText, Text quitText, Text mainMenuText, Text zoomText)
        CreateSettingsPanel(Transform parent, Font font, StatsManager stats, GameManager gm)
    {
        var panel = new GameObject("SettingsPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(panel.transform, false);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(760, 1150);
        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = RuntimeSprite.RoundedPanel;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.13f, 0.14f, 0.19f, 0.98f);

        var title = CreateText(card.transform, "SettingsTitle", font, new Vector2(30, -30), 52);
        title.alignment = TextAnchor.UpperLeft;

        var soundBtn = CreateWideButton(card.transform, font, new Vector2(0, -140));
        soundBtn.GetComponent<Button>().onClick.AddListener(() => Sfx.Muted = !Sfx.Muted);
        var soundText = soundBtn.GetComponentInChildren<Text>();

        var langBtn = CreateWideButton(card.transform, font, new Vector2(0, -260));
        langBtn.GetComponent<Button>().onClick.AddListener(Localization.ToggleLanguage);
        var langText = langBtn.GetComponentInChildren<Text>();

        var statsText = CreateText(card.transform, "StatsText", font, new Vector2(30, -360), 36);
        statsText.alignment = TextAnchor.UpperLeft;
        statsText.color = new Color(0.8f, 0.82f, 0.9f);

        var zoomText = CreateStepperRow(card.transform, font, new Vector2(0, -500),
            () => gm.SetVisualZoom(gm.visualZoom - 0.1f),
            () => gm.SetVisualZoom(gm.visualZoom + 0.1f));

        var mainMenuBtn = CreateWideButton(card.transform, font, new Vector2(0, -650));
        mainMenuBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            panel.SetActive(false);
            gm.ReturnToMenu();
        });
        mainMenuBtn.GetComponent<Image>().color = new Color(0.4f, 0.42f, 0.5f);
        var mainMenuText = mainMenuBtn.GetComponentInChildren<Text>();

        var quitBtn = CreateWideButton(card.transform, font, new Vector2(0, -770));
        quitBtn.GetComponent<Button>().onClick.AddListener(gm.QuitGame);
        quitBtn.GetComponent<Image>().color = new Color(0.75f, 0.28f, 0.28f);
        var quitText = quitBtn.GetComponentInChildren<Text>();

        var closeBtn = CreateWideButton(card.transform, font, new Vector2(0, -890));
        closeBtn.GetComponent<Button>().onClick.AddListener(() => panel.SetActive(false));
        closeBtn.GetComponent<Image>().color = AccentColor;
        var closeText = closeBtn.GetComponentInChildren<Text>();

        panel.SetActive(false);
        return (panel, title, soundText, langText, closeText, statsText, quitText, mainMenuText, zoomText);
    }

    private static (GameObject panel, Text baseText, Text intervalText) CreateCustomConfigPanel(Transform parent, Font font, GameManager gm)
    {
        var panel = new GameObject("CustomConfigPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(panel.transform, false);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(680, 620);
        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = RuntimeSprite.RoundedPanel;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.13f, 0.16f, 0.19f, 0.98f);

        var title = CreateText(card.transform, "CustomConfigTitle", font, new Vector2(30, -30), 42);
        title.alignment = TextAnchor.UpperLeft;
        title.text = Localization.Get("custom_settings_label");

        var baseText = CreateStepperRow(card.transform, font, new Vector2(0, -140),
            () => gm.SetCustomBaseCount(gm.customBaseCount - 1),
            () => gm.SetCustomBaseCount(gm.customBaseCount + 1));

        var intervalText = CreateStepperRow(card.transform, font, new Vector2(0, -270),
            () => gm.SetCustomInterval(gm.customInterval - 1),
            () => gm.SetCustomInterval(gm.customInterval + 1));

        var startBtn = CreateWideButton(card.transform, font, new Vector2(0, -410));
        startBtn.GetComponent<Image>().color = new Color(0.35f, 0.75f, 0.75f);
        var startText = startBtn.GetComponentInChildren<Text>();
        startText.text = Localization.Get("start_run");
        startBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            panel.SetActive(false);
            gm.BeginGame(GameMode.Custom, false);
        });

        var cancelBtn = CreateWideButton(card.transform, font, new Vector2(0, -530));
        cancelBtn.GetComponent<Image>().color = new Color(0.4f, 0.42f, 0.48f);
        var cancelText = cancelBtn.GetComponentInChildren<Text>();
        cancelText.text = Localization.Get("close");
        cancelBtn.GetComponent<Button>().onClick.AddListener(() => panel.SetActive(false));

        panel.SetActive(false);
        return (panel, baseText, intervalText);
    }

    private static Text CreateStepperRow(Transform parent, Font font, Vector2 anchoredPosFromTop, System.Action onMinus, System.Action onPlus)
    {
        var container = new GameObject("StepperRow", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(620, 108);
        rt.anchoredPosition = anchoredPosFromTop;

        var bg = container.AddComponent<Image>();
        bg.sprite = RuntimeSprite.RoundedPanel;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.1f);

        var valueTextObj = new GameObject("Text", typeof(RectTransform));
        valueTextObj.transform.SetParent(container.transform, false);
        var vtrt = valueTextObj.GetComponent<RectTransform>();
        vtrt.anchorMin = Vector2.zero;
        vtrt.anchorMax = Vector2.one;
        vtrt.offsetMin = new Vector2(130, 0);
        vtrt.offsetMax = new Vector2(-130, 0);
        var valueText = valueTextObj.AddComponent<Text>();
        valueText.font = font;
        valueText.fontSize = 34;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.color = Color.white;
        valueText.text = "-";

        var minusObj = CreateStepperButton(container.transform, font, "\u2212", new Vector2(0, 0.5f));
        minusObj.GetComponent<Button>().onClick.AddListener(() => onMinus?.Invoke());

        var plusObj = CreateStepperButton(container.transform, font, "+", new Vector2(1, 0.5f));
        plusObj.GetComponent<Button>().onClick.AddListener(() => onPlus?.Invoke());

        return valueText;
    }

    private static GameObject CreateStepperButton(Transform parent, Font font, string label, Vector2 anchor)
    {
        var go = new GameObject("StepperButton_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(108, 108);
        rt.anchoredPosition = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite = RuntimeSprite.RoundedPanel;
        img.type = Image.Type.Sliced;
        img.color = AccentColor;
        go.AddComponent<Button>();

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 54;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;

        return go;
    }

    private static GameObject CreateWideButton(Transform parent, Font font, Vector2 anchoredPosFromTop)
    {
        var go = new GameObject("WideButton", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(620, 108);
        rt.anchoredPosition = anchoredPosFromTop;

        var img = go.AddComponent<Image>();
        img.sprite = RuntimeSprite.RoundedPanel;
        img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.1f);
        go.AddComponent<Button>();

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 40;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "-";

        return go;
    }

    private static (GameObject panel, Text bestTimeAttack, Text bestNormal, Text normalTitle, Text normalDesc, Text timeAttackTitle, Text timeAttackDesc,
        Text bestBigBoard, Text bigBoardTitle, Text bigBoardDesc, Text bestColorMatch, Text colorMatchTitle, Text colorMatchDesc,
        Text bestCustom, Text customTitle, Text customDesc,
        ModeCardRefs normalRefs, ModeCardRefs timeAttackRefs, ModeCardRefs bigBoardRefs, ModeCardRefs colorMatchRefs, ModeCardRefs customRefs)
        CreateMainMenu(Transform parent, Font font, GameManager gm, System.Action openCustomConfig)
    {
        var panel = new GameObject("MainMenuPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 1f);

        //타이틀
        var titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(panel.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0, -80);
        titleRt.sizeDelta = new Vector2(800, 100);
        var titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 66;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = "ShapeFit";

        //부제
        var subtitleObj = new GameObject("Subtitle", typeof(RectTransform));
        subtitleObj.transform.SetParent(panel.transform, false);
        var subtitleRt = subtitleObj.GetComponent<RectTransform>();
        subtitleRt.anchorMin = new Vector2(0.5f, 1f);
        subtitleRt.anchorMax = new Vector2(0.5f, 1f);
        subtitleRt.pivot = new Vector2(0.5f, 1f);
        subtitleRt.anchoredPosition = new Vector2(0, -160);
        subtitleRt.sizeDelta = new Vector2(800, 46);
        var subtitleText = subtitleObj.AddComponent<Text>();
        subtitleText.font = font;
        subtitleText.fontSize = 24;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.6f, 0.63f, 0.72f);
        subtitleText.text = "Fit the pieces";

        //모드 카드 5개
        float colOffset = 190f;
        float rowTopY = 260f;
        float rowBottomY = -110f;
        float rowCustomY = -480f;

        var normalCard = CreateModeCard(panel.transform, font, new Vector2(-colOffset, rowTopY),
            new Color(0.30f, 0.55f, 0.90f), "normal_mode", "normal_desc");
        normalCard.continueBtn.onClick.AddListener(() => gm.BeginGame(GameMode.Normal, true));
        normalCard.restartBtn.onClick.AddListener(() => gm.BeginGame(GameMode.Normal, false));

        var timeAttackCard = CreateModeCard(panel.transform, font, new Vector2(colOffset, rowTopY),
            new Color(0.90f, 0.45f, 0.30f), "time_attack", "time_attack_desc");
        timeAttackCard.continueBtn.onClick.AddListener(() => gm.BeginGame(GameMode.TimeAttack, true));
        timeAttackCard.restartBtn.onClick.AddListener(() => gm.BeginGame(GameMode.TimeAttack, false));

        var bigBoardCard = CreateModeCard(panel.transform, font, new Vector2(-colOffset, rowBottomY),
            new Color(0.35f, 0.75f, 0.55f), "big_board", "big_board_desc");
        bigBoardCard.continueBtn.onClick.AddListener(() => gm.BeginGame(GameMode.BigBoard, true));
        bigBoardCard.restartBtn.onClick.AddListener(() => gm.BeginGame(GameMode.BigBoard, false));

        var colorMatchCard = CreateModeCard(panel.transform, font, new Vector2(colOffset, rowBottomY),
            new Color(0.75f, 0.4f, 0.85f), "color_match", "color_match_desc");
        colorMatchCard.continueBtn.onClick.AddListener(() => gm.BeginGame(GameMode.ColorMatch, true));
        colorMatchCard.restartBtn.onClick.AddListener(() => gm.BeginGame(GameMode.ColorMatch, false));

        var customCard = CreateModeCard(panel.transform, font, new Vector2(0, rowCustomY),
            new Color(0.35f, 0.75f, 0.75f), "custom_mode", "custom_desc");
        customCard.continueBtn.onClick.AddListener(() => gm.BeginGame(GameMode.Custom, true));
        customCard.restartBtn.onClick.AddListener(() => openCustomConfig?.Invoke());

        var normalRefs = new ModeCardRefs { mode = GameMode.Normal, continueButton = normalCard.continueBtn.gameObject, continueText = normalCard.continueText };
        var timeAttackRefs = new ModeCardRefs { mode = GameMode.TimeAttack, continueButton = timeAttackCard.continueBtn.gameObject, continueText = timeAttackCard.continueText };
        var bigBoardRefs = new ModeCardRefs { mode = GameMode.BigBoard, continueButton = bigBoardCard.continueBtn.gameObject, continueText = bigBoardCard.continueText };
        var colorMatchRefs = new ModeCardRefs { mode = GameMode.ColorMatch, continueButton = colorMatchCard.continueBtn.gameObject, continueText = colorMatchCard.continueText };
        var customRefs = new ModeCardRefs { mode = GameMode.Custom, continueButton = customCard.continueBtn.gameObject, continueText = customCard.continueText };

        return (panel, timeAttackCard.best, normalCard.best, normalCard.title, normalCard.desc, timeAttackCard.title, timeAttackCard.desc,
            bigBoardCard.best, bigBoardCard.title, bigBoardCard.desc, colorMatchCard.best, colorMatchCard.title, colorMatchCard.desc,
            customCard.best, customCard.title, customCard.desc,
            normalRefs, timeAttackRefs, bigBoardRefs, colorMatchRefs, customRefs);
    }

    private static (GameObject card, Button continueBtn, Button restartBtn, Text title, Text desc, Text best, Text continueText)
        CreateModeCard(Transform parent, Font font, Vector2 anchoredPosFromCenter, Color accent, string titleKey, string descKey)
    {
        var go = new GameObject("ModeCard_" + titleKey, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360, 340);
        rt.anchoredPosition = anchoredPosFromCenter;

        var img = go.AddComponent<Image>();
        img.sprite = RuntimeSprite.RoundedPanel;
        img.type = Image.Type.Sliced;
        img.color = new Color(accent.r, accent.g, accent.b, 0.22f);

        var titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(go.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(0, 1);
        titleRt.pivot = new Vector2(0, 1);
        titleRt.anchoredPosition = new Vector2(22, -20);
        titleRt.sizeDelta = new Vector2(320, 52);
        var titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.color = accent;
        titleText.text = Localization.Get(titleKey);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 18;
        titleText.resizeTextMaxSize = 34;

        var descObj = new GameObject("Desc", typeof(RectTransform));
        descObj.transform.SetParent(go.transform, false);
        var descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0, 1);
        descRt.anchorMax = new Vector2(0, 1);
        descRt.pivot = new Vector2(0, 1);
        descRt.anchoredPosition = new Vector2(22, -78);
        descRt.sizeDelta = new Vector2(316, 96);
        var descText = descObj.AddComponent<Text>();
        descText.font = font;
        descText.fontSize = 20;
        descText.alignment = TextAnchor.UpperLeft;
        descText.color = new Color(0.85f, 0.87f, 0.92f);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.text = Localization.Get(descKey);

        var bestObj = new GameObject("Best", typeof(RectTransform));
        bestObj.transform.SetParent(go.transform, false);
        var bestRt = bestObj.GetComponent<RectTransform>();
        bestRt.anchorMin = new Vector2(0, 1);
        bestRt.anchorMax = new Vector2(0, 1);
        bestRt.pivot = new Vector2(0, 1);
        bestRt.anchoredPosition = new Vector2(22, -178);
        bestRt.sizeDelta = new Vector2(320, 76);
        var bestText = bestObj.AddComponent<Text>();
        bestText.font = font;
        bestText.fontSize = 16;
        bestText.alignment = TextAnchor.UpperLeft;
        bestText.color = new Color(0.85f, 0.9f, 1f);
        bestText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bestText.text = "-";

        var continueObj = CreateCardActionButton(go.transform, font, isRight: false, accent);
        var continueBtn = continueObj.GetComponent<Button>();
        var continueText = continueObj.GetComponentInChildren<Text>();
        continueText.text = Localization.Get("continue_run");
        continueText.fontSize = 20;

        var restartObj = CreateCardActionButton(go.transform, font, isRight: true, new Color(0.4f, 0.42f, 0.48f));
        var restartBtn = restartObj.GetComponent<Button>();
        var restartText = restartObj.GetComponentInChildren<Text>();
        restartText.text = Localization.Get("restart_run");
        restartText.fontSize = 20;

        return (go, continueBtn, restartBtn, titleText, descText, bestText, continueText);
    }

    private static GameObject CreateCardActionButton(Transform parent, Font font, bool isRight, Color color)
    {
        var go = new GameObject("CardAction", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(isRight ? 0.52f : 0f, 0f);
        rt.anchorMax = new Vector2(isRight ? 1f : 0.48f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 16);
        rt.sizeDelta = new Vector2(0, 64);

        var img = go.AddComponent<Image>();
        img.sprite = RuntimeSprite.RoundedPanel;
        img.type = Image.Type.Sliced;
        img.color = color;
        go.AddComponent<Button>();

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4, 0);
        trt.offsetMax = new Vector2(-4, 0);
        var text = textObj.AddComponent<Text>();
        text.font = font;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = 22;

        return go;
    }

    public class ModeCardRefs
    {
        public GameMode mode;
        public GameObject continueButton;
        public Text continueText;
    }
}
