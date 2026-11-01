using System;
using TMPro;
using UnityEngine;

public class SlimeEconomy : MonoBehaviour
{
    public static SlimeEconomy instance;

    [Header("구매 가격")]
    public long basePrice = 10;              //흰 슬라임 기본 가격 (맨 첫 구매는 무료)
    public float tierPriceMultiplier = 3.5f; //구매 단계가 하나 오를 때마다 가격에 곱해지는 배율
    public float priceGrowth = 1.15f;        //같은 단계를 살 때마다 가격에 곱해지는 배율

    [Header("구매 단계 자동 상승")]
    public int buyTierGap = 5;               //발견한 최고 단계보다 몇 단계 아래를 구매할지

    [Header("보유 제한")]
    public int maxSlimes = 50;

    [Header("터치 골드 (보유 슬라임 전체 합)")]
    public double touchBase = 1;             //흰 슬라임 터치 골드
    public double touchMultiplier = 2;       //단계가 오를 때마다 곱해지는 배율

    [Header("초당 수익")]
    public double incomeBase = 1;            //빨강 슬라임 초당 수익
    public double incomeMultiplier = 2.45;   //단계가 오를 때마다 곱해지는 배율 (흰 슬라임은 0)

    [Header("UI (선택)")]
    public TextMeshProUGUI incomeText;       //초당 N원
    public TextMeshProUGUI touchText;        //터치당 N원

    public int BuyCount { get; set; }        //현재 구매 단계에서 산 횟수
    public int BuyTier { get; private set; } //현재 구매되는 슬라임 단계
    public long TouchGold { get; private set; }          //골드 2배 적용된 값
    public double IncomePerSecond { get; private set; }  //골드 2배 적용된 값

    public long CurrentPrice => GetPrice();
    public bool IsFull => GameManager.instance.SlimeData.Count >= maxSlimes;
    public bool CanBuy => !IsFull && GameManager.instance.currentGold >= CurrentPrice;

    private const float RecalcInterval = 0.5f;

    private int typeCount;
    private double[] incomeTable;
    private long[] touchTable;
    private double pendingGold;

    private int cachedCount = -1;
    private float recalcTimer;
    private double baseIncome;
    private long baseTouch;

    private long shownIncome = long.MinValue;
    private long shownTouch = long.MinValue;

    private void Awake()
    {
        instance = this;
        typeCount = Enum.GetValues(typeof(SlimeBase.Types)).Length;
        BuildTables();
    }

    private void BuildTables()
    {
        incomeTable = new double[typeCount];
        touchTable = new long[typeCount];

        double income = incomeBase;
        double touch = touchBase;

        for (int i = 0; i < typeCount; i++)
        {
            touchTable[i] = (long)Math.Round(touch);
            touch *= touchMultiplier;

            if (i > 0)
            {
                incomeTable[i] = income;
                income *= incomeMultiplier;
            }
        }
    }
    public double GetIncome(int type)
    {
        if (type < 0 || type >= typeCount) return 0;
        return incomeTable[type];
    }

    public long GetTouch(int type)
    {
        if (type < 0 || type >= typeCount) return 0;
        return touchTable[type];
    }

    public void LoadState(int buyCount, int buyTier)
    {
        BuyCount = buyCount;
        BuyTier = Mathf.Clamp(buyTier, 0, typeCount - 1);
    }

    private long GetPrice()
    {
        if (BuyTier == 0 && BuyCount == 0) return 0;

        int growthCount = BuyTier == 0 ? BuyCount - 1 : BuyCount;
        double price = basePrice
            * Math.Pow(tierPriceMultiplier, BuyTier)
            * Math.Pow(priceGrowth, growthCount);

        if (price >= long.MaxValue) return long.MaxValue;
        return (long)Math.Ceiling(price);
    }

    public bool TryBuy()
    {
        if (IsFull) return false;

        long price = CurrentPrice;
        if (GameManager.instance.currentGold < price) return false;

        GameManager.instance.currentGold -= price;
        BuyCount++;
        return true;
    }

    private int HighestDiscovered()
    {
        for (int i = typeCount - 1; i >= 0; i--)
        {
            if (BookManager.instance.IsDiscovered(i)) return i;
        }
        return 0;
    }

    private void RecalcIfNeeded()
    {
        recalcTimer -= Time.unscaledDeltaTime;

        int count = GameManager.instance.SlimeData.Count;
        if (count == cachedCount && recalcTimer > 0f) return;

        cachedCount = count;
        recalcTimer = RecalcInterval;

        double income = 0;
        long touch = 0;

        foreach (GameObject go in GameManager.instance.SlimeData)
        {
            if (go == null) continue;

            SlimeBase slimeBase = go.GetComponent<SlimeBase>();
            if (slimeBase == null) continue;

            int type = (int)slimeBase.Slimetype;
            income += GetIncome(type);
            touch += GetTouch(type);
        }

        baseIncome = income;
        baseTouch = touch;

        int targetTier = Mathf.Max(0, HighestDiscovered() - buyTierGap);
        if (targetTier > BuyTier)
        {
            BuyTier = targetTier;
            BuyCount = 0;
        }
    }

    private void Update()
    {
        RecalcIfNeeded();

        int multiplier = BoostManager.instance != null ? BoostManager.instance.GoldMultiplier : 1;
        IncomePerSecond = baseIncome * multiplier;
        TouchGold = baseTouch * multiplier;

        pendingGold += IncomePerSecond * Time.deltaTime;
        if (pendingGold >= 1)
        {
            long add = (long)pendingGold;
            GameManager.instance.currentGold += add;
            pendingGold -= add;
        }

        long incomeShown = (long)IncomePerSecond;
        if (incomeText != null && incomeShown != shownIncome)
        {
            shownIncome = incomeShown;
            incomeText.text = $"초당 {GameManager.FormatKoreanNumber(incomeShown)}";
        }

        if (touchText != null && TouchGold != shownTouch)
        {
            shownTouch = TouchGold;
            touchText.text = $"터치당 {GameManager.FormatKoreanNumber(TouchGold)}";
        }
    }
}