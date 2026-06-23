using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ButtonImageObjectToggle : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject image1;
    [SerializeField] private GameObject image2;
    [SerializeField] private bool showImage2OnStart;

    private bool isImage2Visible;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        isImage2Visible = showImage2OnStart;
        ApplyState();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(Toggle);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Toggle);
        }
    }

    public void Toggle()
    {
        isImage2Visible = !isImage2Visible;
        ApplyState();
    }

    private void ApplyState()
    {
        if (image1 != null)
        {
            image1.SetActive(!isImage2Visible);
        }

        if (image2 != null)
        {
            image2.SetActive(isImage2Visible);
        }
    }

    private void ResolveReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (image1 == null)
        {
            image1 = FindChild("Image_1");
        }

        if (image2 == null)
        {
            image2 = FindChild("Image_2");
        }
    }

    private GameObject FindChild(string childName)
    {
        var child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }
}
