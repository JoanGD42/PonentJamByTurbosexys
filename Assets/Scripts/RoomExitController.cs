using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Dual-purpose exit button: closes overlays OR transitions between rooms.
/// Uses Unity's built-in hover detection and yellow color highlight.
/// </summary>
public class RoomExitController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    private Button button;
    private Image buttonImage;
    private Color originalColor;
    private CinematicLoader cinematicLoader;
    private bool isClosingOverlay = false; // Track if we're currently closing an overlay

    void Start() {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        cinematicLoader = FindFirstObjectByType<CinematicLoader>();
        
        if (button != null) {
            button.onClick.AddListener(OnButtonClick);
        }
        
        if (buttonImage != null) {
            originalColor = buttonImage.color;
        }
    }

    void Update() {
        // Handle input blocking state changes AND overlay closing state
        bool shouldBeInteractable = true;
        
        if (GameManager.I != null && GameManager.I.IsInputBlocked) {
            shouldBeInteractable = false;
        }
        
        // Also disable during overlay closing (when overlay was active but is now closing)
        if (cinematicLoader != null && isClosingOverlay) {
            shouldBeInteractable = false;
            
            // Safety check: if overlay is not actually active but flag is stuck, reset it
            if (!cinematicLoader.IsOverlayActive) {
                Debug.LogWarning($"RoomExitController ({gameObject.name}): Detected stuck isClosingOverlay flag, resetting");
                isClosingOverlay = false;
                shouldBeInteractable = true;
            }
        }
        
        if (button != null) {
            button.interactable = shouldBeInteractable;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        // Only show hover effect if button is interactable
        if (button != null && button.interactable && buttonImage != null) {
            if (GameManager.I != null && GameManager.I.IsInputBlocked) return;
            buttonImage.color = Color.yellow;
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        // Always restore original color when leaving
        if (buttonImage != null) {
            buttonImage.color = originalColor;
        }
    }

    private void OnButtonClick() {
        if (GameManager.I == null || GameManager.I.IsInputBlocked) return;
        
        // Check if we're in overlay mode
        if (cinematicLoader != null && cinematicLoader.IsOverlayActive) {
            // Close overlay - disable button during animation
            StartCoroutine(CloseOverlay());
        } else {
            // Normal room transition
            GameManager.I.TransitionToNextRoom();
        }
    }
    
    private IEnumerator CloseOverlay() {
        if (cinematicLoader != null) {
            // Set flag to indicate we're closing overlay (makes button non-interactable)
            isClosingOverlay = true;
            Debug.Log($"RoomExitController ({gameObject.name}): Starting overlay close, button disabled");
            
            yield return cinematicLoader.HideOverlay();
            
            // Always clear flag after overlay operation (success or failure)
            isClosingOverlay = false;
            Debug.Log($"RoomExitController ({gameObject.name}): Overlay close completed, button re-enabled");
        } else {
            Debug.LogError($"RoomExitController ({gameObject.name}): cinematicLoader is null!");
            // Make sure flag is cleared even if cinematicLoader is null
            isClosingOverlay = false;
        }
    }
}