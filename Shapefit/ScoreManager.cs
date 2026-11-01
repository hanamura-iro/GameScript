using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private static string HighScoreKeyFor(GameMode m) => $"ShapeFit_HighScore_{m}";
    private static string BestStageKeyFor(GameMode m) => $"ShapeFit_BestStage_{m}";

    public static int GetBestScore(GameMode mode) => PlayerPrefs.GetInt(HighScoreKeyFor(mode), 0);
    public static int GetBestStage(GameMode mode) => PlayerPrefs.GetInt(BestStageKeyFor(mode), 0);

    private GameMode mode = GameMode.TimeAttack;

    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int BestStage { get; private set; }

    public void SetMode(GameMode newMode)
    {
        mode = newMode;
        HighScore = GetBestScore(mode);
        BestStage = GetBestStage(mode);
        Score = 0;
    }

    public void AddStageScore(int stageNumber, float remainingTimeAtClear, float comboMultiplier = 1f)
    {
        int baseScore = 100 * stageNumber;
        int timeBonus = Mathf.RoundToInt(Mathf.Max(0f, remainingTimeAtClear) * 10f);
        int total = Mathf.RoundToInt((baseScore + timeBonus) * comboMultiplier);
        Score += total;

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKeyFor(mode), HighScore);
            PlayerPrefs.Save();
        }
    }

    public void RecordStage(int stageNumber)
    {
        if (stageNumber <= BestStage) return;
        BestStage = stageNumber;
        PlayerPrefs.SetInt(BestStageKeyFor(mode), BestStage);
        PlayerPrefs.Save();
    }

    public void ResetScore()
    {
        Score = 0;
    }

    public void SetCurrentScore(int score)
    {
        Score = Mathf.Max(0, score);
    }
}
