using UnityEngine;

public sealed class CurrencyManager : MonoBehaviour
{
    [Header("Currencies")]
    [SerializeField] private int stars;

    [Header("UI")]
    [SerializeField] private UICurrencyView currencyView;

    public int Coins => PlayerGold.CurrentCoins;
    public int Stars => stars;

    private void Awake()
    {
        PlayerGold.RefreshState();
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
        if (PlayerGold.AddCoins(amount))
        {
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
        PlayerGold.ResetToInitial();
        stars = 0;
        RefreshView();
    }

    public bool SpendCoins(int amount)
    {
        if (!PlayerGold.SpendCoins(amount))
        {
            return false;
        }

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
            currencyView.Refresh(PlayerGold.CurrentCoins, stars);
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

public static class PlayerGold
{
    public const int InitialCoins = 1000;

    private const string CoinsKey = "PlayerGold.Current";

    public static int CurrentCoins
    {
        get
        {
            RefreshState();
            return GetStoredCoins();
        }
    }

    public static void RefreshState()
    {
        if (!PlayerPrefs.HasKey(CoinsKey))
        {
            SaveCoins(InitialCoins);
            return;
        }

        var storedCoins = PlayerPrefs.GetInt(CoinsKey, InitialCoins);
        if (storedCoins < 0)
        {
            SaveCoins(0);
        }
    }

    public static bool AddCoins(int amount)
    {
        RefreshState();

        if (amount <= 0)
        {
            return false;
        }

        var totalCoins = (long)GetStoredCoins() + amount;
        SaveCoins(totalCoins > int.MaxValue ? int.MaxValue : (int)totalCoins);
        return true;
    }

    public static bool SpendCoins(int amount)
    {
        RefreshState();

        var coins = GetStoredCoins();
        if (amount < 0 || coins < amount)
        {
            return false;
        }

        SaveCoins(coins - amount);
        return true;
    }

    public static void ResetToInitial()
    {
        SaveCoins(InitialCoins);
    }

    private static int GetStoredCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, InitialCoins));
    }

    private static void SaveCoins(int coins)
    {
        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, coins));
        PlayerPrefs.Save();
    }
}
