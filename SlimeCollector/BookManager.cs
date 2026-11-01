using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookManager : MonoBehaviour
{
    public static BookManager instance;

    public Image BookIMG;

    public Image SlimeIMG;
    public TextMeshProUGUI SlimeNameText;
    public TextMeshProUGUI SlimeExText;

    public string[] slimeNames = new string[27];
    public string[] slimeDescriptions = new string[27];

    public Image[] SlimeSlotIMG;

    private bool[] discovered;
    private static readonly string[] DefaultNames =
    {
        "흰 슬라임",
        "빨강 슬라임",
        "주황 슬라임",
        "노랑 슬라임",
        "초록 슬라임",
        "파랑 슬라임",
        "남색 슬라임",
        "보라 슬라임",
        "투명 슬라임",

        "왕 슬라임",
        "바위 슬라임",
        "유령 슬라임",
        "똥 슬라임",
        "피곤한 슬라임",
        "황금 슬라임",
        "슬픈 슬라임",
        "화난 슬라임",
        "아기 슬라임",

        "바보 슬라임",
        "강아지 슬라임",
        "고양이 슬라임",
        "토끼 슬라임",
        "햄스터 슬라임",
        "병아리 슬라임",
        "여우 슬라임",
        "돼지 슬라임",
        "드래곤 슬라임",
    };

    private static readonly string[] DefaultDescriptions =
    {
        "모든 슬라임의 시작. 쓰다듬으면 말랑말랑한 소리가 난다.",
        "흰 슬라임 둘이 만나 얼굴이 빨개졌다. 부끄러움을 많이 탄다.",
        "햇볕을 좋아해서 늘 따끈따끈하다. 귤 냄새가 난다는 소문이 있다.",
        "늘 신이 나서 한시도 가만히 있지 않는다. 밤에는 살짝 빛난다.",
        "풀밭에 누워 있으면 아무도 찾지 못한다. 숨바꼭질 챔피언.",
        "물처럼 차분한 성격. 몸이 시원해서 여름에 인기가 많다.",
        "밤하늘을 닮은 슬라임. 가만히 들여다보면 작은 별이 보인다.",
        "신비로운 분위기를 풍기지만 사실 아무 생각이 없다.",
        "너무 맑아서 자주 밟힌다. 존재감이 없는 게 고민이다.",

        "왕관을 쓰고 태어났다. 신하는 없지만 늘 당당하다.",
        "단단해 보이지만 속은 여전히 말랑하다. 잘 굴러다닌다.",
        "벽을 통과할 수 있다. 놀래키는 걸 좋아하지만 본인이 더 잘 놀란다.",
        "모양은 저래도 향기는 딸기 맛. 아무도 믿어주지 않는다.",
        "하루 스물세 시간을 잔다. 나머지 한 시간은 하품을 한다.",
        "온몸이 반짝반짝 빛난다. 지나간 자리에 금가루가 떨어진다는 소문이 있다.",
        "늘 눈물이 그렁그렁하다. 슬픈 이유는 본인도 모른다.",
        "항상 씩씩대지만 머리를 쓰다듬어 주면 금방 풀린다.",
        "합칠수록 커질 줄 알았는데 오히려 작아졌다. 세상 모든 게 신기하다.",

        "자기 이름을 자주 잊어버린다. 그래도 늘 행복하다.",
        "꼬리 대신 몸 전체를 흔든다. 부르면 어디서든 달려온다.",
        "부르면 안 오고, 안 부르면 옆에 와서 눕는다.",
        "귀가 길어서 멀리서 나는 소리도 다 듣는다. 당근을 정말 좋아한다.",
        "볼에 뭐든 넣어둔다. 가끔 다른 슬라임도 넣으려고 한다.",
        "삐약삐약 따라다니는 걸 좋아한다. 처음 본 상대를 엄마라고 믿는다.",
        "꼬리가 복슬복슬하다. 영리해서 장난을 잘 친다.",
        "먹는 걸 정말 좋아한다. 행복할 때 꿀꿀 소리를 낸다.",
        "모든 슬라임의 꿈. 작은 날개로 열심히 날갯짓하지만 아직 날지는 못한다.",
    };

    private void Awake()
    {
        instance = this;
        discovered = new bool[SlimeSlotIMG.Length];
        FillEmptyWithDefaults();
    }

    private void FillEmptyWithDefaults()
    {
        if (slimeNames == null || slimeNames.Length < DefaultNames.Length)
            System.Array.Resize(ref slimeNames, DefaultNames.Length);
        if (slimeDescriptions == null || slimeDescriptions.Length < DefaultDescriptions.Length)
            System.Array.Resize(ref slimeDescriptions, DefaultDescriptions.Length);

        for (int i = 0; i < DefaultNames.Length; i++)
        {
            if (string.IsNullOrEmpty(slimeNames[i])) slimeNames[i] = DefaultNames[i];
            if (string.IsNullOrEmpty(slimeDescriptions[i])) slimeDescriptions[i] = DefaultDescriptions[i];
        }
    }
    [ContextMenu("기본 이름과 설명 채우기")]
    private void FillDefaultsInInspector()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "슬라임 이름과 설명 채우기");
#endif
        slimeNames = (string[])DefaultNames.Clone();
        slimeDescriptions = (string[])DefaultDescriptions.Clone();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public void BookBtn()
    {
        BookIMG.gameObject.SetActive(!BookIMG.gameObject.activeSelf);
    }

    // 슬라임 종류를 발견 처리. 처음 발견했으면 true
    public bool Discover(int index)
    {
        if (index < 0 || index >= discovered.Length) return false;

        bool isNew = !discovered[index];
        discovered[index] = true;
        SlimeSlotIMG[index].color = new Color32(255, 255, 255, 255);

        return isNew;
    }

    public bool IsDiscovered(int index)
    {
        return index >= 0 && index < discovered.Length && discovered[index];
    }

    // 저장
    public bool[] GetDiscovered()
    {
        return (bool[])discovered.Clone();
    }

    // 불러오기
    public void LoadDiscovered(bool[] data)
    {
        int count = Mathf.Min(data.Length, discovered.Length);
        for (int i = 0; i < count; i++)
        {
            discovered[i] = data[i];
            if (data[i])
            {
                SlimeSlotIMG[i].color = new Color32(255, 255, 255, 255);
            }
        }
    }

    public void SlimeSlotBtn(int index)
    {
        GameObject[] slimes = GameManager.instance.Slimes;
        if (index < 0 || index >= slimes.Length) return;

        SpriteRenderer sr = slimes[index].GetComponent<SpriteRenderer>();
        SlimeIMG.sprite = sr != null ? sr.sprite : null;

        if (IsDiscovered(index))
        {
            SlimeIMG.color = new Color32(255, 255, 255, 255);
            SlimeNameText.text = index < slimeNames.Length ? slimeNames[index] : "";
            SlimeExText.text = index < slimeDescriptions.Length ? slimeDescriptions[index] : "";
        }
        else
        {
            SlimeIMG.color = new Color32(0, 0, 0, 255);
            SlimeNameText.text = "???";
            SlimeExText.text = "???";
        }
    }
}