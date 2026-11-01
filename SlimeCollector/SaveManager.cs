using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    [Header("자동 저장 간격 (초)")]
    public float autoSaveInterval = 5f;

    [Serializable]
    public class SlimeSave
    {
        public int type;
        public float x;
        public float y;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long gold;
        public int buyCount;
        public int buyTier;
        public bool[] discovered;
        public List<SlimeSave> slimes = new List<SlimeSave>();
        public long lastSaveTicks;   //마지막 저장 시각 (오프라인 보상 계산용)
    }

    //지난번 접속 때 마지막으로 저장된 시각 (UTC Ticks, 첫 실행이면 0)
    public long LastSaveTicks { get; private set; }

    private const string TutorialDoneKey = "TutorialDone";

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    private float timer;
    private bool loaded;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Load();
        loaded = true;
    }

    private void Update()
    {
        if (!loaded) return;

        timer += Time.unscaledDeltaTime;
        if (timer >= autoSaveInterval)
        {
            timer = 0f;
            Save();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && loaded) Save();
    }

    private void OnApplicationQuit()
    {
        if (loaded) Save();
    }

    public void Save()
    {
        try
        {
            SaveData data = new SaveData();
            data.gold = GameManager.instance.currentGold;
            data.discovered = BookManager.instance.GetDiscovered();

            if (SlimeEconomy.instance != null)
            {
                data.buyCount = SlimeEconomy.instance.BuyCount;
                data.buyTier = SlimeEconomy.instance.BuyTier;
            }

            foreach (GameObject go in GameManager.instance.SlimeData)
            {
                if (go == null) continue;

                SlimeBase slimeBase = go.GetComponent<SlimeBase>();
                if (slimeBase == null) continue;

                SlimeMove move = go.GetComponent<SlimeMove>();
                Vector3 pos = move != null ? move.LogicalPosition : go.transform.position;

                data.slimes.Add(new SlimeSave
                {
                    type = (int)slimeBase.Slimetype,
                    x = pos.x,
                    y = pos.y
                });
            }

            data.lastSaveTicks = DateTime.UtcNow.Ticks;

            string json = JsonUtility.ToJson(data);
            string tempPath = SavePath + ".tmp";

            File.WriteAllText(tempPath, json);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tempPath, SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");
        }
    }

    private void Load()
    {
        if (!File.Exists(SavePath)) return;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"불러오기 실패: {e.Message}");
            File.Copy(SavePath, SavePath + ".broken", true);
            return;
        }

        if (data == null) return;

        GameManager.instance.currentGold = data.gold;
        LastSaveTicks = data.lastSaveTicks;

        if (SlimeEconomy.instance != null)
        {
            SlimeEconomy.instance.LoadState(data.buyCount, data.buyTier);
        }

        if (data.discovered != null)
        {
            BookManager.instance.LoadDiscovered(data.discovered);
        }

        GameObject[] prefabs = GameManager.instance.Slimes;

        foreach (SlimeSave s in data.slimes)
        {
            if (s.type < 0 || s.type >= prefabs.Length) continue;

            GameObject prefab = prefabs[s.type];
            Vector3 pos = new Vector3(s.x, s.y, prefab.transform.position.z);

            SlimeMove prefabMove = prefab.GetComponent<SlimeMove>();
            if (prefabMove != null)
            {
                pos.x = Mathf.Clamp(pos.x, prefabMove.minX, prefabMove.maxX);
                pos.y = Mathf.Clamp(pos.y, prefabMove.minY, prefabMove.maxY);
            }

            GameObject slime = Instantiate(prefab, pos, Quaternion.identity);
            GameManager.instance.SlimeData.Add(slime);
        }
    }

    public void ResetAll()
    {
        loaded = false;

        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"초기화 실패: {e.Message}");
        }

        PlayerPrefs.DeleteKey(TutorialDoneKey);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    [ContextMenu("저장 데이터 삭제")]
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("저장 데이터를 삭제");
        }
        else
        {
            Debug.Log("삭제할 저장 데이터가 없음");
        }

        PlayerPrefs.DeleteKey(TutorialDoneKey);
        PlayerPrefs.Save();
    }
}