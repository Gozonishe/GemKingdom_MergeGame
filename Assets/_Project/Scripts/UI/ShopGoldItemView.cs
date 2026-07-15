using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class ShopGoldItemView : MonoBehaviour
{
    [Serializable]
    public sealed class ShopGoldItemClickedEvent : UnityEvent<ShopGoldItemView>
    {
    }

    [Header("Data")]
    [SerializeField] private Sprite goldSprite;
    [SerializeField, Min(0)] private int goldAmount;
    [SerializeField] private string currencyText = "EUR";
    [SerializeField] private string amountText = "0.00";

    [Header("UI")]
    [SerializeField] private Image goldImage;
    [SerializeField] private TMP_Text goldAmountText;
    [SerializeField] private TMP_Text currencyTextLabel;
    [SerializeField] private TMP_Text amountTextLabel;
    [SerializeField] private Button buyButton;

    [Header("Events")]
    [SerializeField] private ShopGoldItemClickedEvent purchased = new ShopGoldItemClickedEvent();

    public Sprite GoldSprite => goldSprite;
    public int GoldAmount => Mathf.Max(0, goldAmount);
    public string CurrencyText => currencyText;
    public string AmountText => amountText;
    public ShopGoldItemClickedEvent Purchased => purchased;

    public event Action<ShopGoldItemView> PurchasedEvent;

    private void Awake()
    {
        ResolveReferences();
        ApplyDataToUi();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyDataToUi();

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(HandleBuyClicked);
        }
    }

    private void OnDisable()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }

    private void OnValidate()
    {
        goldAmount = Mathf.Max(0, goldAmount);
        ResolveReferences();
        ApplyDataToUi();
    }

    public void Configure(Sprite sprite, int amount, string currency, string price)
    {
        goldSprite = sprite;
        goldAmount = Mathf.Max(0, amount);
        currencyText = currency;
        amountText = price;
        ApplyDataToUi();
    }

    private void ApplyDataToUi()
    {
        if (goldImage != null)
        {
            goldImage.sprite = goldSprite;
            goldImage.enabled = goldSprite != null;
        }

        if (goldAmountText != null)
        {
            goldAmountText.text = GoldAmount.ToString();
        }

        if (currencyTextLabel != null)
        {
            currencyTextLabel.text = currencyText;
        }

        if (amountTextLabel != null)
        {
            amountTextLabel.text = amountText;
        }
    }

    private void HandleBuyClicked()
    {
        if (!PlayerGold.AddCoins(GoldAmount))
        {
            return;
        }

        PurchasedEvent?.Invoke(this);

        if (purchased != null)
        {
            purchased.Invoke(this);
        }

        KeepAvailableForPurchase();
    }

    private void KeepAvailableForPurchase()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (buyButton != null)
        {
            if (!buyButton.gameObject.activeSelf)
            {
                buyButton.gameObject.SetActive(true);
            }

            buyButton.interactable = true;
        }
    }

    private void ResolveReferences()
    {
        goldImage = goldImage != null ? goldImage : FindImage("GoldImage");
        goldAmountText = goldAmountText != null ? goldAmountText : FindText("GoldAmount");
        currencyTextLabel = currencyTextLabel != null ? currencyTextLabel : FindText("CurrancyText");
        currencyTextLabel = currencyTextLabel != null ? currencyTextLabel : FindText("CurrencyText");
        amountTextLabel = amountTextLabel != null ? amountTextLabel : FindText("AmoutText");
        amountTextLabel = amountTextLabel != null ? amountTextLabel : FindText("AmountText");
        buyButton = buyButton != null ? buyButton : GetComponent<Button>();
        buyButton = buyButton != null ? buyButton : GetComponentInChildren<Button>(true);
    }

    private Image FindImage(string objectName)
    {
        var images = GetComponentsInChildren<Image>(true);
        for (var i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image != null && image.name == objectName)
            {
                return image;
            }
        }

        return null;
    }

    private TMP_Text FindText(string objectName)
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text != null && text.name == objectName)
            {
                return text;
            }
        }

        return null;
    }
}
