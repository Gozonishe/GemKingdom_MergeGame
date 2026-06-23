using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIButtonClickSound : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private AudioClip overrideClickClip;
    [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

    private void Reset()
    {
        ResolveButton();
    }

    private void Awake()
    {
        ResolveButton();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(PlayClickSound);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        var audioController = UIAudioController.Instance;

        if (audioController == null)
        {
            return;
        }

        if (overrideClickClip != null)
        {
            audioController.PlayUISound(overrideClickClip, volumeScale);
            return;
        }

        audioController.PlayButtonClick();
    }

    private void ResolveButton()
    {
        if (button != null)
        {
            return;
        }

        button = GetComponent<Button>();

        if (button == null)
        {
            button = GetComponentInChildren<Button>(true);
        }
    }
}
