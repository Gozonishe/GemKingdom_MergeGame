using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIAudioSettingsButtonsController : MonoBehaviour
{
    [SerializeField] private UIAudioController audioController;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private string soundButtonName = "SoundBtnSwitch";
    [SerializeField] private string musicButtonName = "MusicBtnSwitch";
    [SerializeField, Min(0.1f)] private float refreshInterval = 0.25f;

    private readonly List<Button> subscribedSoundButtons = new List<Button>();
    private readonly List<Button> subscribedMusicButtons = new List<Button>();
    private float nextRefreshTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void ToggleUISound()
    {
        ResolveAudioController();
        audioController?.ToggleUISoundMuted();
    }

    public void ToggleMusic()
    {
        ResolveAudioController();
        audioController?.ToggleMusicMuted();
    }

    private void Subscribe()
    {
        ResolveReferences();
        PruneNullSubscriptions(subscribedSoundButtons);
        PruneNullSubscriptions(subscribedMusicButtons);

        SubscribeButtons(soundButton, soundButtonName, ToggleUISound, subscribedSoundButtons);
        SubscribeButtons(musicButton, musicButtonName, ToggleMusic, subscribedMusicButtons);
    }

    private void Unsubscribe()
    {
        for (var i = 0; i < subscribedSoundButtons.Count; i++)
        {
            if (subscribedSoundButtons[i] != null)
            {
                subscribedSoundButtons[i].onClick.RemoveListener(ToggleUISound);
            }
        }

        for (var i = 0; i < subscribedMusicButtons.Count; i++)
        {
            if (subscribedMusicButtons[i] != null)
            {
                subscribedMusicButtons[i].onClick.RemoveListener(ToggleMusic);
            }
        }

        subscribedSoundButtons.Clear();
        subscribedMusicButtons.Clear();
    }

    private void ResolveReferences()
    {
        ResolveAudioController();

        if (soundButton == null)
        {
            soundButton = FindSceneButton(soundButtonName);
        }

        if (musicButton == null)
        {
            musicButton = FindSceneButton(musicButtonName);
        }
    }

    private void ResolveAudioController()
    {
        if (audioController != null)
        {
            return;
        }

        audioController = UIAudioController.Instance != null
            ? UIAudioController.Instance
            : GetComponent<UIAudioController>();
    }

    private static Button FindSceneButton(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName))
        {
            return null;
        }

        var buttons = Resources.FindObjectsOfTypeAll<Button>();

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var scene = button.gameObject.scene;

            if (!scene.IsValid() || !scene.isLoaded || !IsNamedButton(button, buttonName))
            {
                continue;
            }

            return button;
        }

        return null;
    }

    private static void SubscribeButtons(Button primaryButton, string buttonName, UnityEngine.Events.UnityAction action, List<Button> subscribedButtons)
    {
        AddButtonSubscription(primaryButton, action, subscribedButtons);

        var sceneButtons = FindSceneButtons(buttonName);
        for (var i = 0; i < sceneButtons.Count; i++)
        {
            AddButtonSubscription(sceneButtons[i], action, subscribedButtons);
        }
    }

    private static void AddButtonSubscription(Button button, UnityEngine.Events.UnityAction action, List<Button> subscribedButtons)
    {
        if (button == null || subscribedButtons.Contains(button))
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
        subscribedButtons.Add(button);
    }

    private static List<Button> FindSceneButtons(string buttonName)
    {
        var matchingButtons = new List<Button>();

        if (string.IsNullOrEmpty(buttonName))
        {
            return matchingButtons;
        }

        var buttons = Resources.FindObjectsOfTypeAll<Button>();

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var scene = button.gameObject.scene;

            if (!scene.IsValid() || !scene.isLoaded || !IsNamedButton(button, buttonName))
            {
                continue;
            }

            matchingButtons.Add(button);
        }

        return matchingButtons;
    }

    private static void PruneNullSubscriptions(List<Button> subscribedButtons)
    {
        for (var i = subscribedButtons.Count - 1; i >= 0; i--)
        {
            if (subscribedButtons[i] == null)
            {
                subscribedButtons.RemoveAt(i);
            }
        }
    }

    private static bool IsNamedButton(Button button, string buttonName)
    {
        if (button == null || string.IsNullOrEmpty(buttonName))
        {
            return false;
        }

        var current = button.transform;
        while (current != null)
        {
            if (current.name == buttonName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
