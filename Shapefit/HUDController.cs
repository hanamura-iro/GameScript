using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public GameManager gameManager;
    public ScoreManager scoreManager;
    public StatsManager statsManager;

    [Header("메인 메뉴")]
    public GameObject mainMenuPanel;
    public Text menuBestTimeAttackText;
    public Text menuBestNormalText;
    public Text menuNormalTitleText;
    public Text menuNormalDescText;
    public Text menuTimeAttackTitleText;
    public Text menuTimeAttackDescText;
    public Text menuBestBigBoardText;
    public Text menuBigBoardTitleText;
    public Text menuBigBoardDescText;
    public Text menuBestColorMatchText;
    public Text menuColorMatchTitleText;
    public Text menuColorMatchDescText;
    public Text menuBestCustomText;
    public Text menuCustomTitleText;
    public Text menuCustomDescText;
    public System.Collections.Generic.List<GameBootstrap.ModeCardRefs> modeCardRefs;
    public GameObject gameplayHudRoot; 

    public Text scoreText;
    public Text highScoreText;
    public Text stageText;
    public Text timerText;
    public Text comboText;

    public GameObject gameOverPanel;
    public Text gameOverText;
    public Text restartButtonText;

    public Image flashOverlay;
    public GameObject stageClearPopup;
    public Text stageClearText;

    public GameObject toastPopup;
    public Text toastText;

    [Header("광고 버튼")]
    public GameObject adTimeButtonObj;
    public Text adTimeButtonText;
    public Button hintButton;
    public Text hintButtonText;
    public GameObject reviveButtonObj;
    public Text reviveButtonText;

    [Header("설정 패널")]
    public GameObject settingsPanel;
    public Text settingsTitleText;
    public Text soundToggleText;
    public Text langToggleText;
    public Text closeButtonText;
    public Text statsText;
    public Text quitButtonText;
    public Text mainMenuButtonText;
    public Text cellSizeText;
    public Text customBaseText;
    public Text customIntervalText;

    private const float TextRefreshInterval = 0.1f;
    private float nextTextRefresh;

    void Start()
    {
        if (gameManager != null)
        {
            gameManager.OnStageCleared += HandleStageCleared;
            gameManager.OnMessage += HandleMessage;
            gameManager.OnGameStarted += HandleGameStarted;
            gameManager.OnReturnedToMenu += HandleReturnedToMenu;
        }
        RefreshMenuVisibility();
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnStageCleared -= HandleStageCleared;
            gameManager.OnMessage -= HandleMessage;
            gameManager.OnGameStarted -= HandleGameStarted;
            gameManager.OnReturnedToMenu -= HandleReturnedToMenu;
        }
    }

    private void HandleGameStarted(GameMode mode)
    {
        RefreshMenuVisibility();
    }

    private void HandleReturnedToMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        RefreshMenuVisibility();
        UpdateMenuBestScores();
    }

    private void RefreshMenuVisibility()
    {
        bool playing = gameManager != null && gameManager.IsPlaying;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(!playing);
        if (gameplayHudRoot != null) gameplayHudRoot.SetActive(playing);
    }

    private void UpdateMenuBestScores()
    {
        SetBestText(menuBestNormalText, GameMode.Normal);
        SetBestText(menuBestTimeAttackText, GameMode.TimeAttack);
        SetBestText(menuBestBigBoardText, GameMode.BigBoard);
        SetBestText(menuBestColorMatchText, GameMode.ColorMatch);
        SetBestText(menuBestCustomText, GameMode.Custom);

        if (menuNormalTitleText != null) menuNormalTitleText.text = Localization.Get("normal_mode");
        if (menuNormalDescText != null) menuNormalDescText.text = Localization.Get("normal_desc");
        if (menuTimeAttackTitleText != null) menuTimeAttackTitleText.text = Localization.Get("time_attack");
        if (menuTimeAttackDescText != null) menuTimeAttackDescText.text = Localization.Get("time_attack_desc");
        if (menuBigBoardTitleText != null) menuBigBoardTitleText.text = Localization.Get("big_board");
        if (menuBigBoardDescText != null) menuBigBoardDescText.text = Localization.Get("big_board_desc");
        if (menuColorMatchTitleText != null) menuColorMatchTitleText.text = Localization.Get("color_match");
        if (menuColorMatchDescText != null) menuColorMatchDescText.text = Localization.Get("color_match_desc");
        if (menuCustomTitleText != null) menuCustomTitleText.text = Localization.Get("custom_mode");
        if (menuCustomDescText != null) menuCustomDescText.text = Localization.Get("custom_desc");

        if (modeCardRefs != null)
        {
            foreach (var refs in modeCardRefs)
            {
                if (refs == null) continue;
                bool hasSave = GameManager.HasSavedProgress(refs.mode);
                if (refs.continueButton != null)
                {
                    var btn = refs.continueButton.GetComponent<Button>();
                    if (btn != null) btn.interactable = hasSave; // 비활성화되면 Button 기본 트랜지션으로 흐리게 표시됨
                }
                if (refs.continueText != null)
                {
                    refs.continueText.text = hasSave
                        ? $"{Localization.Get("continue_run")} ({GameManager.GetSavedStage(refs.mode)})"
                        : Localization.Get("continue_run");
                }
            }
        }
    }

    private void SetBestText(Text text, GameMode mode)
    {
        if (text == null) return;
        int score = ScoreManager.GetBestScore(mode);
        int stage = ScoreManager.GetBestStage(mode);
        string maxBlocksLine = gameManager != null
            ? $"\n{Localization.Get("max_blocks")}: {gameManager.GetCapForMode(mode)}"
            : "";
        text.text = $"{Localization.Get("best")}: {score}\n{Localization.Get("stage")} {stage}{maxBlocksLine}";
    }

    void Update()
    {
        // 안드로이드 뒤로가기 버튼
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null)
        {
            if (settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }
            else
            {
                settingsPanel.transform.SetAsLastSibling();
                settingsPanel.SetActive(true);
            }
        }

        if (Time.unscaledTime < nextTextRefresh) return;
        nextTextRefresh = Time.unscaledTime + TextRefreshInterval;

        bool playing = gameManager != null && gameManager.IsPlaying;
        bool isTimeAttack = playing && gameManager.CurrentMode == GameMode.TimeAttack;

        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            UpdateMenuBestScores();

        if (settingsTitleText != null)
            settingsTitleText.text = Localization.Get("settings");

        if (soundToggleText != null)
            soundToggleText.text = $"{Localization.Get("sound")}: {(Sfx.Muted ? Localization.Get("off") : Localization.Get("on"))}";

        if (langToggleText != null)
            langToggleText.text = $"{Localization.Get("language")}: {(Localization.Current == Localization.Lang.KR ? "한국어" : "English")}";

        if (closeButtonText != null)
            closeButtonText.text = Localization.Get("close");

        if (mainMenuButtonText != null)
            mainMenuButtonText.text = Localization.Get("main_menu");

        if (cellSizeText != null && gameManager != null)
            cellSizeText.text = $"{Localization.Get("cell_size")}: {Mathf.RoundToInt(gameManager.visualZoom * 100f)}%";

        if (customBaseText != null && gameManager != null)
            customBaseText.text = $"{Localization.Get("custom_base")}: {gameManager.customBaseCount}";

        if (customIntervalText != null && gameManager != null)
            customIntervalText.text = $"{Localization.Get("custom_interval")}: {gameManager.customInterval}";

        if (quitButtonText != null)
            quitButtonText.text = Localization.Get("quit");

        if (statsText != null && statsManager != null)
        {
            statsText.text =
                $"{Localization.Get("total_cleared")}: {statsManager.TotalStagesCleared}\n" +
                $"{Localization.Get("best_combo")}: {statsManager.BestCombo}\n" +
                $"{Localization.Get("games_played")}: {statsManager.GamesPlayed}";
        }

        if (!playing) return; 

        if (scoreText != null && scoreManager != null)
            scoreText.text = $"{Localization.Get("score")}: {scoreManager.Score}";

        if (highScoreText != null && scoreManager != null)
            highScoreText.text = $"{Localization.Get("best")}: {scoreManager.HighScore}";

        if (stageText != null && gameManager != null)
            stageText.text = $"{Localization.Get("stage")} {gameManager.StageNumber}";

        if (timerText != null)
        {
            timerText.gameObject.SetActive(isTimeAttack);
            if (isTimeAttack)
            {
                int seconds = Mathf.CeilToInt(gameManager.TimeRemaining);
                timerText.text = $"{Localization.Get("time")}: {seconds}";
                timerText.color = seconds <= 5 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            }
        }

        if (adTimeButtonObj != null)
            adTimeButtonObj.SetActive(isTimeAttack);

        if (comboText != null && gameManager != null)
        {
            bool show = gameManager.ComboCount >= 2;
            comboText.gameObject.SetActive(show);
            if (show) comboText.text = $"{gameManager.ComboCount} {Localization.Get("combo")}";
        }

        if (hintButtonText != null && gameManager != null)
        {
            if (gameManager.HintUsesThisStage == 0)
                hintButtonText.text = Localization.Get("hint_free");
            else
                hintButtonText.text = $"{Localization.Get("watch_ad_hint")} ({gameManager.HintUsesThisStage})";
        }

        if (hintButton != null && gameManager != null)
        {
            bool available = gameManager.HintAvailable;
            hintButton.interactable = available;
            var img = hintButton.GetComponent<Image>();
            if (img != null) img.color = available ? new Color(0.85f, 0.65f, 0.25f) : new Color(0.4f, 0.4f, 0.42f);
        }

        if (adTimeButtonText != null)
            adTimeButtonText.text = Localization.Get("watch_ad_time");

        if (reviveButtonText != null)
            reviveButtonText.text = Localization.Get("watch_ad_revive");

        if (reviveButtonObj != null && gameManager != null)
            reviveButtonObj.SetActive(isTimeAttack && gameManager.IsGameOver && !gameManager.HasRevivedThisGame);

        if (restartButtonText != null)
            restartButtonText.text = Localization.Get("restart");

        if (gameOverPanel != null && gameManager != null)
        {
            bool shouldShow = isTimeAttack && gameManager.IsGameOver;
            if (gameOverPanel.activeSelf != shouldShow)
                gameOverPanel.SetActive(shouldShow);

            if (shouldShow && gameOverText != null)
            {
                gameOverText.text =
                    $"{Localization.Get("gameover_title")}\n\n" +
                    $"{Localization.Get("score")}: {scoreManager.Score}\n" +
                    $"{Localization.Get("best")}: {scoreManager.HighScore}\n" +
                    $"{Localization.Get("stage")}: {gameManager.StageNumber}";
            }
        }
    }

    private void HandleStageCleared(int clearedStage, int combo)
    {
        StartCoroutine(PlayStageClearEffect(combo));
    }

    private void HandleMessage(string localizationKey)
    {
        StartCoroutine(ShowToast(Localization.Get(localizationKey)));
    }

    private IEnumerator ShowToast(string message)
    {
        if (toastPopup == null || toastText == null) yield break;

        toastText.text = message;
        toastPopup.SetActive(true);
        var canvasGroup = toastPopup.GetComponent<CanvasGroup>();

        const float fadeIn = 0.2f, hold = 2.2f, fadeOut = 0.5f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = t / fadeIn;
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = 1f - t / fadeOut;
            yield return null;
        }
        toastPopup.SetActive(false);
    }

    private IEnumerator PlayStageClearEffect(int combo)
    {
        if (flashOverlay != null)
        {
            float t = 0f;
            const float flashDuration = 0.22f;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                float k = t / flashDuration;
                float alpha = k < 0.4f ? Mathf.Lerp(0f, 0.35f, k / 0.4f) : Mathf.Lerp(0.35f, 0f, (k - 0.4f) / 0.6f);
                var c = flashOverlay.color;
                c.a = alpha;
                flashOverlay.color = c;
                yield return null;
            }
            var reset = flashOverlay.color;
            reset.a = 0f;
            flashOverlay.color = reset;
        }

        if (stageClearPopup != null && stageClearText != null)
        {
            stageClearText.text = combo >= 2
                ? $"{Localization.Get("stageclear")}\n{combo} {Localization.Get("combo")}"
                : Localization.Get("stageclear");
            stageClearPopup.SetActive(true);
            var rt = stageClearPopup.transform;

            float t = 0f;
            const float popIn = 0.15f;
            const float hold = 0.35f;
            const float fadeOut = 0.35f;
            var canvasGroup = stageClearPopup.GetComponent<CanvasGroup>();

            while (t < popIn)
            {
                t += Time.deltaTime;
                float k = t / popIn;
                rt.localScale = Vector3.Lerp(Vector3.one * 0.6f, Vector3.one, k);
                if (canvasGroup != null) canvasGroup.alpha = k;
                yield return null;
            }
            rt.localScale = Vector3.one;

            yield return new WaitForSeconds(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float k = t / fadeOut;
                if (canvasGroup != null) canvasGroup.alpha = 1f - k;
                yield return null;
            }
            stageClearPopup.SetActive(false);
        }
    }
}
