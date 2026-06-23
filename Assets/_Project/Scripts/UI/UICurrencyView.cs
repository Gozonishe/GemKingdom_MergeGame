using TMPro;
using UnityEngine;

public sealed class UICurrencyView : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text starsText;

    private void Awake()
    {
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
        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }

        if (starsText != null)
        {
            starsText.text = stars.ToString();
        }
    }
}
