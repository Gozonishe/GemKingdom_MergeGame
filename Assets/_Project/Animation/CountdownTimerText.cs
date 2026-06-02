using TMPro;
using UnityEngine;

public sealed class CountdownTimerText : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private float durationHours = 24f;
    [SerializeField] private bool restartOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private float endTime;
    private bool isInitialized;
    private int lastShownTotalMinutes = -1;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        if (restartOnEnable || !isInitialized || GetRemainingSeconds() <= 0f)
        {
            StartCountdown();
        }

        RefreshText();
    }

    private void Update()
    {
        RefreshText();
    }

    private void StartCountdown()
    {
        endTime = GetCurrentTime() + Mathf.Max(0f, durationHours * 3600f);
        isInitialized = true;
        lastShownTotalMinutes = -1;
    }

    private void RefreshText()
    {
        if (targetText == null)
        {
            return;
        }

        var remainingSeconds = GetRemainingSeconds();
        var totalMinutes = Mathf.FloorToInt(remainingSeconds / 60f);
        var maxShownMinutes = Mathf.Max(0, Mathf.RoundToInt(durationHours * 60f) - 1);
        if (remainingSeconds > 0f)
        {
            totalMinutes = Mathf.Min(totalMinutes, maxShownMinutes);
        }

        if (totalMinutes == lastShownTotalMinutes)
        {
            return;
        }

        lastShownTotalMinutes = totalMinutes;
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        targetText.text = $"{hours:00}h {minutes:00}m";
    }

    private float GetRemainingSeconds()
    {
        return Mathf.Max(0f, endTime - GetCurrentTime());
    }

    private float GetCurrentTime()
    {
        return useUnscaledTime ? Time.realtimeSinceStartup : Time.time;
    }
}
