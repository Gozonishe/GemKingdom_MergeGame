using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public sealed class UIAudioSceneInstaller : MonoBehaviour
{
    [Header("Audio Controller")]
    [SerializeField] private UIAudioController audioController;
    [SerializeField] private AudioClip defaultButtonClickClip;
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float uiSoundVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;
    [SerializeField] private bool playMusicOnEnable = true;

    [Header("Button Click Sounds")]
    [SerializeField] private bool installClickSounds = true;
    [SerializeField] private Transform buttonsRoot;
    [SerializeField] private bool includeInactiveButtons = true;
    [SerializeField] private bool refreshButtonsNextFrame = true;

    private Coroutine refreshCoroutine;

    private void Awake()
    {
        EnsureAudioController();
        ApplyAudioSettings();
        RefreshButtonClickSounds();
    }

    private void OnEnable()
    {
        EnsureAudioController();
        ApplyAudioSettings();

        if (refreshButtonsNextFrame)
        {
            RefreshButtonClickSoundsDelayed();
        }
    }

    private void Start()
    {
        EnsureAudioController();
        ApplyAudioSettings();
        RefreshButtonClickSounds();

        if (playMusicOnEnable)
        {
            audioController?.PlayMenuMusic();
        }
    }

    private void OnDisable()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    [ContextMenu("Refresh Button Click Sounds")]
    public void RefreshButtonClickSounds()
    {
        if (!installClickSounds)
        {
            return;
        }

        var root = buttonsRoot != null ? buttonsRoot : transform;
        var buttons = root.GetComponentsInChildren<Button>(includeInactiveButtons);

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null || button.GetComponent<UIButtonClickSound>() != null)
            {
                continue;
            }

            button.gameObject.AddComponent<UIButtonClickSound>();
        }
    }

    public void RefreshButtonClickSoundsDelayed()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        refreshCoroutine = StartCoroutine(RefreshButtonClickSoundsNextFrame());
    }

    private IEnumerator RefreshButtonClickSoundsNextFrame()
    {
        yield return null;
        RefreshButtonClickSounds();
        refreshCoroutine = null;
    }

    private void EnsureAudioController()
    {
        if (audioController != null)
        {
            return;
        }

        audioController = UIAudioController.Instance;
        if (audioController != null)
        {
            return;
        }

        var audioObject = new GameObject("UIAudio");
        audioController = audioObject.AddComponent<UIAudioController>();
    }

    private void ApplyAudioSettings()
    {
        if (audioController == null)
        {
            return;
        }

        audioController.ConfigureAudioClips(defaultButtonClickClip, musicClip);
        audioController.SetUISoundVolume(uiSoundVolume);
        audioController.SetMusicVolume(musicVolume);
    }
}
