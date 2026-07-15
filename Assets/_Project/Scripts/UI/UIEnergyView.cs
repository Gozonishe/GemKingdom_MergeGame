using TMPro;
using UnityEngine;

public sealed class UIEnergyView : MonoBehaviour
{
    [SerializeField] private TMP_Text energyText;

    private void Awake()
    {
        if (energyText == null)
        {
            Debug.LogError($"{nameof(UIEnergyView)} on '{name}' is missing {nameof(energyText)}.", this);
        }
    }

    public void Refresh(int current)
    {
        if (energyText != null)
        {
            energyText.text = current.ToString();
        }
    }
}
