using UnityEngine;
using UnityEngine.UI;

public sealed class PearlChallengeWindowController : MonoBehaviour
{
    [SerializeField] private GameObject pearlChallengeWindowRoot;
    [SerializeField] private GameObject dimBackgroundRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        ValidateReferences();

        if (pearlChallengeWindowRoot != null)
        {
            pearlChallengeWindowRoot.SetActive(false);
        }

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(false);
        }

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseWindow);
        }
    }

    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseWindow);
        }
    }

    public void OpenWindow()
    {
        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(PearlChallengeWindowController)} on '{name}' cannot open the window because {nameof(pearlChallengeWindowRoot)} is not assigned.", this);
            return;
        }

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(true);
        }

        pearlChallengeWindowRoot.SetActive(true);
    }

    public void CloseWindow()
    {
        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(PearlChallengeWindowController)} on '{name}' cannot close the window because {nameof(pearlChallengeWindowRoot)} is not assigned.", this);
            return;
        }

        pearlChallengeWindowRoot.SetActive(false);

        if (dimBackgroundRoot != null)
        {
            dimBackgroundRoot.SetActive(false);
        }
    }

    private bool ValidateReferences()
    {
        var isValid = true;

        if (pearlChallengeWindowRoot == null)
        {
            Debug.LogError($"{nameof(PearlChallengeWindowController)} on '{name}' is missing a reference to {nameof(pearlChallengeWindowRoot)}.", this);
            isValid = false;
        }

        if (openButton == null)
        {
            Debug.LogError($"{nameof(PearlChallengeWindowController)} on '{name}' is missing a reference to {nameof(openButton)}.", this);
            isValid = false;
        }

        if (dimBackgroundRoot == null)
        {
            Debug.LogError($"{nameof(PearlChallengeWindowController)} on '{name}' is missing a reference to {nameof(dimBackgroundRoot)}.", this);
            isValid = false;
        }

        if (closeButton == null)
        {
            Debug.LogError($"{nameof(PearlChallengeWindowController)} on '{name}' is missing a reference to {nameof(closeButton)}.", this);
            isValid = false;
        }

        return isValid;
    }
}
