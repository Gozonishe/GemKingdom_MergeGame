using UnityEngine;
using UnityEngine.UI;

public sealed class IconEventShineSweep : MonoBehaviour
{
    [SerializeField] private Vector2 initialDelayRange = new(1f, 3f);
    [SerializeField] private float instanceDelayJitter = 0.75f;
    [SerializeField] private float minDelay = 5f;
    [SerializeField] private float maxDelay = 5f;
    [SerializeField] private float sweepDuration = 0.55f;
    [SerializeField] private float stripeWidth = 45f;
    [SerializeField] private float stripeHeightMultiplier = 1.7f;
    [SerializeField] private float angle = -18f;
    [SerializeField] private float maxAlpha = 0.58f;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform targetRect;
    private Image sourceImage;
    private RectTransform shineRect;
    private Image shineImage;
    private float timer;
    private float nextDelay;
    private bool isSweeping;

    private void Awake()
    {
        targetRect = transform as RectTransform;
        sourceImage = GetComponent<Image>();
        EnsureSpriteMask();
        CreateShine();
    }

    private void OnEnable()
    {
        RestartSweep(GetInitialDelay());
    }

    public void Configure(Vector2 initialDelay, Vector2 repeatDelay, float width, float alpha)
    {
        initialDelayRange = initialDelay;
        minDelay = Mathf.Max(0f, repeatDelay.x);
        maxDelay = Mathf.Max(minDelay, repeatDelay.y);
        stripeWidth = Mathf.Max(1f, width);
        maxAlpha = Mathf.Clamp01(alpha);
    }

    public void RestartSweep(float delay)
    {
        isSweeping = false;
        timer = 0f;
        nextDelay = Mathf.Max(0f, delay);
        SetShineAlpha(0f);
    }

    private void Update()
    {
        if (targetRect == null || shineRect == null || shineImage == null)
        {
            return;
        }

        timer += GetDeltaTime();

        if (!isSweeping)
        {
            if (timer >= nextDelay)
            {
                isSweeping = true;
                timer = 0f;
            }

            return;
        }

        var progress = sweepDuration > 0f ? Mathf.Clamp01(timer / sweepDuration) : 1f;
        UpdateShine(progress);

        if (progress >= 1f)
        {
            isSweeping = false;
            timer = 0f;
            nextDelay = GetNextDelay();
            SetShineAlpha(0f);
        }
    }

    private void EnsureSpriteMask()
    {
        if (sourceImage == null)
        {
            return;
        }

        var mask = GetComponent<Mask>();
        if (mask == null)
        {
            mask = gameObject.AddComponent<Mask>();
        }

        mask.showMaskGraphic = true;
    }

    private void CreateShine()
    {
        if (sourceImage == null)
        {
            enabled = false;
            return;
        }

        var shineObject = new GameObject("ShineSweep", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shineObject.layer = gameObject.layer;
        shineObject.transform.SetParent(transform, false);

        shineRect = shineObject.GetComponent<RectTransform>();
        shineRect.anchorMin = new Vector2(0.5f, 0.5f);
        shineRect.anchorMax = new Vector2(0.5f, 0.5f);
        shineRect.pivot = new Vector2(0.5f, 0.5f);
        shineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        shineImage = shineObject.GetComponent<Image>();
        shineImage.raycastTarget = false;
        shineImage.color = new Color(1f, 1f, 1f, 0f);
        UpdateShineSize();
        SetShineAlpha(0f);
    }

    private void UpdateShine(float progress)
    {
        UpdateShineSize();
        var alpha = Mathf.Sin(progress * Mathf.PI) * maxAlpha;
        var rect = targetRect.rect;
        var startX = -rect.width * 0.5f - stripeWidth;
        var endX = rect.width * 0.5f + stripeWidth;

        shineRect.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, progress), 0f);
        SetShineAlpha(alpha);
    }

    private void UpdateShineSize()
    {
        if (targetRect == null || shineRect == null)
        {
            return;
        }

        shineRect.sizeDelta = new Vector2(stripeWidth, targetRect.rect.height * stripeHeightMultiplier);
    }

    private void SetShineAlpha(float alpha)
    {
        if (shineImage == null)
        {
            return;
        }

        var color = shineImage.color;
        color.a = Mathf.Clamp01(alpha);
        shineImage.color = color;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private float GetNextDelay()
    {
        return Random.Range(minDelay, Mathf.Max(minDelay, maxDelay));
    }

    private float GetInitialDelay()
    {
        var min = Mathf.Max(0f, initialDelayRange.x);
        var max = Mathf.Max(min, initialDelayRange.y);
        var instanceOffset = Mathf.Abs(GetInstanceID() % 1000) / 999f * Mathf.Max(0f, instanceDelayJitter);
        return Random.Range(min, max) + instanceOffset;
    }
}
