using UnityEngine;

public sealed class UIAudioBootstrapper : MonoBehaviour
{
    [SerializeField] private UIAudioController audioControllerPrefab;

    private void Awake()
    {
        if (UIAudioController.Instance != null)
        {
            return;
        }

        if (audioControllerPrefab != null)
        {
            Instantiate(audioControllerPrefab);
            return;
        }

        new GameObject("UIAudio").AddComponent<UIAudioController>();
    }
}
