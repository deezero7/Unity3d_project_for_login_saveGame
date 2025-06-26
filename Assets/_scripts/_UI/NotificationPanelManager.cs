using UnityEngine;
using UnityEngine.UI; // Required for Image, Text, Button etc.
using System.Collections; // Required for Coroutines
using TMPro;

public class NotificationPanelManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The GameObject representing the notification panel.")]
    public GameObject notificationPanel;

    [Tooltip("Optional: The Text component to display the message content.")]
    public TMP_Text messageText; // Assign this if you have a Text component inside your panel

    [Header("Settings")]
    [Tooltip("How long the panel stays active after a message, in seconds.")]
    public float displayDuration = 10f;

    [Tooltip("Optional: How long the panel takes to fade in/out, in seconds.")]
    [Range(0.01f, 1f)] // Restrict range for reasonable fade times
    public float fadeDuration = 0.5f;

    private Coroutine currentDisplayCoroutine;

    /*
    centralized control over the whole panel’s appearance (Image, Text, TMP_Text, Button background, etc.)
     * CanvasGroup is used for fading effects.
     * If you don't want to use fading, you can remove this and related code.
     * You attach it to the parent panel (the GameObject that groups your UI).
     * Then you can control:
     * alpha (fades the whole UI block)
        interactable (whether buttons/text inputs can be used)
        blocksRaycasts (whether the UI catches clicks)
     */     
    private CanvasGroup canvasGroup; // Used for fading, if attached

    void Awake()
    {
        // Try to get the CanvasGroup component if it exists
        canvasGroup = notificationPanel.GetComponent<CanvasGroup>();

        // Ensure the panel is initially disabled
        notificationPanel.SetActive(false);

        // If using CanvasGroup, set initial alpha to 0 and disable raycasts
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    /// <summary>
    /// Displays the notification panel with an optional message.
    /// Resets the timer if the panel is already active.
    /// </summary>
    /// <param name="message">The text message to display. Optional.</param>
    public void ShowNotification(string message = "")
    {
        // Stop any existing timer to reset it
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        // Set the message text if a Text component is assigned
        if (messageText != null && !string.IsNullOrEmpty(message))
        {
            messageText.text = message;
        }
        else if (messageText != null && string.IsNullOrEmpty(message))
        {
            // If no message provided, use a default or clear previous
            messageText.text = "New Message!";
        }


        // Activate the panel if it's not active
        if (!notificationPanel.activeSelf)
        {
            notificationPanel.SetActive(true);
            // Start fade-in if CanvasGroup is used
            if (canvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration));
            }
        }

        // Start a new timer for the panel to hide
        currentDisplayCoroutine = StartCoroutine(HidePanelAfterDelay());
    }

    /// <summary>
    /// Coroutine to handle the display duration and then hide the panel.
    /// </summary>
    private IEnumerator HidePanelAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        // Start fade-out if CanvasGroup is used
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration));
        }

        // Disable the panel GameObject after the timer (and fade, if applicable)
        notificationPanel.SetActive(false);
        currentDisplayCoroutine = null; // Clear the coroutine reference
    }

    /// <summary>
    /// Coroutine to fade a CanvasGroup.
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;
        float currentAlpha = startAlpha;

        // Enable/disable raycasts for interaction during fade
        if (endAlpha > startAlpha) // Fading in
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        while (Time.time < endTime)
        {
            float elapsed = Time.time - startTime;
            currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            cg.alpha = currentAlpha;
            yield return null; // Wait for the next frame
        }

        cg.alpha = endAlpha; // Ensure final alpha is set precisely

        if (endAlpha < startAlpha) // Fading out
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
    }


    /// <summary>
    /// Immediately hides the panel, stopping any current timer.
    /// Useful for a close button on the panel itself.
    /// </summary>
    public void HidePanelImmediately()
    {
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }

        if (notificationPanel.activeSelf)
        {
            // If fading, instantly set alpha to 0 before deactivating
            if (canvasGroup != null)
            {
                StopAllCoroutines(); // Stop any ongoing fade coroutine
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            notificationPanel.SetActive(false);
            Debug.Log("Notification panel hidden immediately.");
        }
    }
}