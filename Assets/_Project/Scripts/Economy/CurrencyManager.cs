using UnityEngine;

public sealed class CurrencyManager : MonoBehaviour
{
    [Header("Currencies")]
    [SerializeField] private int coins;
    [SerializeField] private int stars;

    [Header("UI")]
    [SerializeField] private UICurrencyView currencyView;

    public int Coins => coins;
    public int Stars => stars;

    private void Awake()
    {
        ValidateReferences();
        RefreshView();
    }

    public void AddReward(int coinAmount, int starAmount)
    {
        AddCoins(coinAmount);
        AddStars(starAmount);
    }

    public void AddCoins(int amount)
    {
        if (amount > 0)
        {
            coins += amount;
            RefreshView();
        }
    }

    public void AddStars(int amount)
    {
        if (amount > 0)
        {
            stars += amount;
            RefreshView();
        }
    }

    public void ResetCurrencies()
    {
        coins = 0;
        stars = 0;
        RefreshView();
    }

    public bool SpendCoins(int amount)
    {
        if (amount < 0 || coins < amount)
        {
            return false;
        }

        coins -= amount;
        RefreshView();
        return true;
    }

    public bool SpendStars(int amount)
    {
        if (amount < 0 || stars < amount)
        {
            return false;
        }

        stars -= amount;
        RefreshView();
        return true;
    }

    private void RefreshView()
    {
        if (currencyView != null)
        {
            currencyView.Refresh(coins, stars);
        }
    }

    private void ValidateReferences()
    {
        if (currencyView == null)
        {
            Debug.LogError($"{nameof(CurrencyManager)} on '{name}' is missing {nameof(currencyView)}. Currency UI will not update.", this);
        }
    }
}
