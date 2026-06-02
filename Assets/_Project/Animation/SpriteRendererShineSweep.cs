using UnityEngine;

public sealed class SpriteRendererShineSweep : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private Shader shineShader;
    [SerializeField] private Vector2 initialDelayRange = new Vector2(1f, 3f);
    [SerializeField] private float instanceDelayJitter = 0.75f;
    [SerializeField] private float minDelay = 3.5f;
    [SerializeField] private float maxDelay = 5.5f;
    [SerializeField] private float sweepDuration = 0.55f;
    [SerializeField] private float stripeWidth = 0.12f;
    [SerializeField] private float angle = -18f;
    [SerializeField] private float maxAlpha = 0.3f;
    [SerializeField] private int sortingOrderOffset = 20;
    [SerializeField] private bool useUnscaledTime = true;

    private SpriteRenderer shineRenderer;
    private Material shineMaterial;
    private float timer;
    private float nextDelay;
    private bool isSweeping;

    private void Awake()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<SpriteRenderer>();
        }

        CreateShineRenderer();
    }

    private void OnEnable()
    {
        isSweeping = false;
        timer = 0f;
        nextDelay = GetInitialDelay();
        SetShineAlpha(0f);
        SyncSourceRenderer();
    }

    private void Update()
    {
        if (sourceRenderer == null || shineRenderer == null || shineMaterial == null)
        {
            return;
        }

        SyncSourceRenderer();
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
        var alpha = Mathf.Sin(progress * Mathf.PI) * maxAlpha;
        shineMaterial.SetFloat("_Progress", progress);
        shineMaterial.SetFloat("_Angle", angle);
        shineMaterial.SetFloat("_StripeWidth", Mathf.Max(0.001f, stripeWidth));
        SetShineAlpha(alpha);

        if (progress >= 1f)
        {
            isSweeping = false;
            timer = 0f;
            nextDelay = GetNextDelay();
            SetShineAlpha(0f);
        }
    }

    private void OnDestroy()
    {
        if (shineMaterial != null)
        {
            Destroy(shineMaterial);
        }
    }

    private void CreateShineRenderer()
    {
        if (sourceRenderer == null || shineShader == null)
        {
            enabled = false;
            return;
        }

        var shineObject = new GameObject("SpriteShineSweep", typeof(SpriteRenderer));
        shineObject.layer = gameObject.layer;
        shineObject.transform.SetParent(transform, false);
        shineObject.transform.localPosition = Vector3.zero;
        shineObject.transform.localRotation = Quaternion.identity;
        shineObject.transform.localScale = Vector3.one;

        shineRenderer = shineObject.GetComponent<SpriteRenderer>();
        shineMaterial = new Material(shineShader);
        shineRenderer.sharedMaterial = shineMaterial;
        SyncSourceRenderer();
        SetShineAlpha(0f);
    }

    private void SyncSourceRenderer()
    {
        if (sourceRenderer == null || shineRenderer == null)
        {
            return;
        }

        shineRenderer.sprite = sourceRenderer.sprite;
        shineRenderer.flipX = sourceRenderer.flipX;
        shineRenderer.flipY = sourceRenderer.flipY;
        shineRenderer.drawMode = sourceRenderer.drawMode;
        shineRenderer.size = sourceRenderer.size;
        shineRenderer.maskInteraction = sourceRenderer.maskInteraction;
        shineRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shineRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
    }

    private void SetShineAlpha(float alpha)
    {
        if (shineMaterial != null)
        {
            shineMaterial.SetFloat("_MaxAlpha", Mathf.Clamp01(alpha));
        }
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
