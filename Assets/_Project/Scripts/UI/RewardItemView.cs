using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject appearEffectPrefab;

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

    public void PlayAppearEffect()
    {
        if (appearEffectPrefab == null)
        {
            return;
        }

        var effectParent = iconImage != null ? iconImage.transform : transform;
        var effect = Instantiate(appearEffectPrefab, effectParent, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        effect.transform.localScale = appearEffectPrefab.transform.localScale;

        var particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
        var destroyDelay = 0f;

        for (var i = 0; i < particleSystems.Length; i++)
        {
            var particleSystem = particleSystems[i];
            particleSystem.Clear(true);
            particleSystem.Play(true);

            var main = particleSystem.main;
            var particleDuration = main.duration + main.startDelay.constantMax + main.startLifetime.constantMax;
            destroyDelay = Mathf.Max(destroyDelay, particleDuration);
        }

        Destroy(effect, destroyDelay > 0f ? destroyDelay + 0.1f : 1f);
    }
}
