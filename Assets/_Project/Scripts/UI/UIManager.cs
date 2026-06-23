using UnityEngine;

public sealed class UIManager : MonoBehaviour
{
    [Header("Root Panels")]
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private GameObject ordersRoot;
    [SerializeField] private GameObject economyRoot;
    [SerializeField] private GameObject rewardsRoot;

    public void ShowGameplayUI()
    {
        SetRootActive(gameplayRoot, true);
        SetRootActive(ordersRoot, true);
        SetRootActive(economyRoot, true);
        SetRootActive(rewardsRoot, false);
    }

    public void ShowRewardsUI()
    {
        SetRootActive(rewardsRoot, true);
    }

    public void HideRewardsUI()
    {
        SetRootActive(rewardsRoot, false);
    }

    private static void SetRootActive(GameObject target, bool isActive)
    {
        if (target != null && target.activeSelf != isActive)
        {
            target.SetActive(isActive);
        }
    }
}
