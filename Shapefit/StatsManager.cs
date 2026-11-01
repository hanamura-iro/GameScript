using UnityEngine;
public class StatsManager : MonoBehaviour
{
    private const string TotalStagesKey = "ShapeFit_TotalStagesCleared";
    private const string GamesPlayedKey = "ShapeFit_GamesPlayed";
    private const string BestComboKey = "ShapeFit_BestCombo";

    public int TotalStagesCleared { get; private set; }
    public int GamesPlayed { get; private set; }
    public int BestCombo { get; private set; }

    void Awake()
    {
        TotalStagesCleared = PlayerPrefs.GetInt(TotalStagesKey, 0);
        GamesPlayed = PlayerPrefs.GetInt(GamesPlayedKey, 0);
        BestCombo = PlayerPrefs.GetInt(BestComboKey, 0);

        GamesPlayed++;
        PlayerPrefs.SetInt(GamesPlayedKey, GamesPlayed);
        PlayerPrefs.Save();
    }

    public void RecordStageCleared()
    {
        TotalStagesCleared++;
        PlayerPrefs.SetInt(TotalStagesKey, TotalStagesCleared);
        PlayerPrefs.Save();
    }

    public void RecordCombo(int combo)
    {
        if (combo <= BestCombo) return;
        BestCombo = combo;
        PlayerPrefs.SetInt(BestComboKey, BestCombo);
        PlayerPrefs.Save();
    }
}
