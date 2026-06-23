using UnityEngine;
using UnityEngine.UI;

public sealed class UIAudioSettingsButtonsController : MonoBehaviour
{
    [SerializeField] private UIAudioController audioController;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private string soundButtonName = "SoundBtnSwitch";
    [SerializeField] private string musicButtonName = "MusicBtnSwitch";

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
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
        if (soundButton != null)
        {
            soundButton.onClick.RemoveListener(ToggleUISound);
            soundButton.onClick.AddListener(ToggleUISound);
        }

        if (musicButton != null)
        {
            musicButton.onClick.RemoveListener(ToggleMusic);
            musicButton.onClick.AddListener(ToggleMusic);
        }
    }

    private void Unsubscribe()
    {
        if (soundButton != null)
        {
            soundButton.onClick.RemoveListener(ToggleUISound);
        }

        if (musicButton != null)
        {
            musicButton.onClick.RemoveListener(ToggleMusic);
        }
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

            if (!scene.IsValid() || !scene.isLoaded || button.name != buttonName)
            {
                continue;
            }

            return button;
        }

        return null;
    }
}
