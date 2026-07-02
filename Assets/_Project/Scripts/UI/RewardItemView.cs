using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private GameObject appearEffectPrefab;

    [Header("Icon Shine")]
    [SerializeField] private bool ensureIconShineSweep = true;
    [SerializeField] private Vector2 shineInitialDelayRange = new(0.15f, 0.35f);
    [SerializeField] private Vector2 shineRepeatDelayRange = new(2f, 3f);
    [SerializeField, Min(1f)] private float shineStripeWidth = 34f;
    [SerializeField, Range(0f, 1f)] private float shineMaxAlpha = 0.42f;

    public void Set(Sprite icon, int amount)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            EnsureIconShineSweep();
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

    private void EnsureIconShineSweep()
    {
        if (!ensureIconShineSweep || iconImage == null || iconImage.sprite == null)
        {
            return;
        }

        var shineSweep = iconImage.GetComponent<IconEventShineSweep>();
        if (shineSweep == null)
        {
            shineSweep = iconImage.gameObject.AddComponent<IconEventShineSweep>();
        }

        shineSweep.Configure(shineInitialDelayRange, shineRepeatDelayRange, shineStripeWidth, shineMaxAlpha);
        shineSweep.RestartSweep(Random.Range(shineInitialDelayRange.x, shineInitialDelayRange.y));
    }
}
