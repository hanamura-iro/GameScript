using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuySlime : MonoBehaviour
{
    [Header("UI (선택)")]
    public Button buyButton;
    public TextMeshProUGUI priceText;
    public Image buyIcon;                  // 지금 구매되는 슬라임 아이콘
    public string freeMessage = "무료";
    public string fullMessage = "가득 참";

    private int shownTier = -1;
    private long shownPrice = long.MinValue;  // -1 = 가득 참

    private void Update()
    {
        SlimeEconomy eco = SlimeEconomy.instance;
        if (eco == null) return;

        long priceKey = eco.IsFull ? -1 : eco.CurrentPrice;
        if (priceText != null && priceKey != shownPrice)
        {
            shownPrice = priceKey;

            if (priceKey < 0) priceText.text = fullMessage;
            else if (priceKey == 0) priceText.text = freeMessage;
            else priceText.text = GameManager.FormatKoreanNumber(priceKey);
        }

        if (buyButton != null)
        {
            buyButton.interactable = eco.CanBuy;
        }

        if (buyIcon != null && shownTier != eco.BuyTier)
        {
            shownTier = eco.BuyTier;
            SpriteRenderer sr = GameManager.instance.Slimes[shownTier].GetComponent<SpriteRenderer>();
            buyIcon.sprite = sr != null ? sr.sprite : null;
            buyIcon.preserveAspect = true;
        }
    }

    public void SlimeBuy()
    {
        SlimeEconomy eco = SlimeEconomy.instance;
        if (eco == null) return;

        int tier = eco.BuyTier;
        if (!eco.TryBuy()) return;

        GameObject spawned = Instantiate(
            GameManager.instance.Slimes[tier],
            GameManager.instance.SlimeSpwanPos[Random.Range(0, GameManager.instance.SlimeSpwanPos.Length)].position,
            Quaternion.identity);

        GameManager.instance.SlimeData.Add(spawned);

        bool isNew = BookManager.instance.Discover(tier);
        if (isNew && DiscoveryPopup.instance != null)
        {
            DiscoveryPopup.instance.Show(tier);
        }
    }
}