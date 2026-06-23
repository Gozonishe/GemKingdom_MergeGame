using UnityEngine;

public sealed class EnergyManager : MonoBehaviour
{
    [Header("Energy")]
    [SerializeField] private int maxEnergy = 100;
    [SerializeField] private int currentEnergy = 100;
    [SerializeField] private int mergeCost = 1;

    [Header("UI")]
    [SerializeField] private UIEnergyView energyView;

    public int MaxEnergy => maxEnergy;
    public int CurrentEnergy => currentEnergy;
    public int MergeCost => mergeCost;

    private void Awake()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        ValidateReferences();
        RefreshView();
    }

    public bool HasEnoughEnergy()
    {
        return currentEnergy >= mergeCost;
    }

    public bool SpendEnergy()
    {
        if (!HasEnoughEnergy())
        {
            return false;
        }

        currentEnergy = Mathf.Max(0, currentEnergy - mergeCost);
        RefreshView();
        return true;
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        RefreshView();
    }

    public void SetEnergy(int current, int max)
    {
        maxEnergy = Mathf.Max(0, max);
        currentEnergy = Mathf.Clamp(current, 0, maxEnergy);
        RefreshView();
    }

    private void RefreshView()
    {
        if (energyView != null)
        {
            energyView.Refresh(currentEnergy, maxEnergy);
        }
    }

    private void ValidateReferences()
    {
        if (energyView == null)
        {
            Debug.LogError($"{nameof(EnergyManager)} on '{name}' is missing {nameof(energyView)}. Energy UI will not update.", this);
        }
    }
}
