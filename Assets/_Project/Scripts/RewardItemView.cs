using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;

    public void Set(Sprite icon, int amount)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (amountText != null)
        {
            amountText.text = $"x{amount}";
        }
    }
}
