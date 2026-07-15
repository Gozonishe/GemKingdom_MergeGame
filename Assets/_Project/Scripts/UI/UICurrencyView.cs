using TMPro;
using UnityEngine;

public sealed class UICurrencyView : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text starsText;

    private void Awake()
    {
        ResolveReferences();

        if (coinsText == null)
        {
            Debug.LogError($"{nameof(UICurrencyView)} on '{name}' is missing {nameof(coinsText)}.", this);
        }

        if (starsText == null)
        {
            Debug.LogError($"{nameof(UICurrencyView)} on '{name}' is missing {nameof(starsText)}.", this);
        }
    }

    public void Refresh(int coins, int stars)
    {
        ResolveReferences();

        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }

        if (starsText != null)
        {
            starsText.text = $"Stars: {stars}";
        }
    }

    private void ResolveReferences()
    {
        if (coinsText != null)
        {
            return;
        }

        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "GoldAmount")
            {
                coinsText = texts[i];
                return;
            }
        }
    }
}
