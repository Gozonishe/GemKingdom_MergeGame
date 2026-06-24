using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text starsText;
    [SerializeField] private Button claimButton;

    private bool isVisible;

    public event Action Hidden;
    public bool IsVisible => isVisible;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        ValidateReferences();
        Hide();
    }

    private void OnEnable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(Hide);
        }
    }

    private void OnDisable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(Hide);
        }
    }

    public void Show(int coins, int stars)
    {
        isVisible = true;

        if (titleText != null)
        {
            titleText.text = "Reward";
        }

        if (coinsText != null)
        {
            coinsText.text = coins.ToString();
        }

        if (starsText != null)
        {
            starsText.text = stars.ToString();
        }

        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void Hide()
    {
        var wasVisible = isVisible;
        isVisible = false;

        if (root != null)
        {
            root.SetActive(false);
        }

        if (wasVisible)
        {
            Hidden?.Invoke();
        }
    }

    private void ValidateReferences()
    {
        if (titleText == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(titleText)}.", this);
        }

        if (coinsText == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(coinsText)}.", this);
        }

        if (starsText == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(starsText)}.", this);
        }

        if (claimButton == null)
        {
            Debug.LogError($"{nameof(RewardPopup)} on '{name}' is missing {nameof(claimButton)}.", this);
        }
    }
}
