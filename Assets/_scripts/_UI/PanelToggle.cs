using UnityEngine;

public class PanelToggle : MonoBehaviour
{
    public GameObject panel; // Assign your panel in the inspector

    public void TogglePanel()
    {
        if (panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    public void ShowPanel()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
