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
    private float lastClickTime = -10f;  // Debounce rapid clicks

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
        // Simple: button is only interactable when input is not blocked
        bool shouldBeInteractable = (GameManager.I == null) || !GameManager.I.IsInputBlocked;
        
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
        Debug.Log($"RoomExitController ({gameObject.name}): OnButtonClick triggered!");
        Debug.Log($"  - GameManager.I: {(GameManager.I != null ? "Found" : "NULL")}");
        Debug.Log($"  - IsInputBlocked: {(GameManager.I != null ? GameManager.I.IsInputBlocked : "N/A")}");
        Debug.Log($"  - cinematicLoader: {(cinematicLoader != null ? "Found" : "NULL")}");
        
        // Debounce: ignore clicks within 0.5 seconds of last click
        if (Time.time - lastClickTime < 0.5f) {
            Debug.Log($"RoomExitController ({gameObject.name}): Click ignored - too soon after last click");
            return;
        }
        lastClickTime = Time.time;
        
        if (GameManager.I == null || GameManager.I.IsInputBlocked) {
            Debug.Log($"RoomExitController ({gameObject.name}): Click blocked - GameManager null or input blocked");
            return;
        }
        
        // Try to hide overlay if one exists
        if (cinematicLoader != null && cinematicLoader.CurrentOverlayContext != "") {
            Debug.Log($"RoomExitController ({gameObject.name}): Overlay active - closing overlay");
            StartCoroutine(cinematicLoader.HideOverlay());
        } else {
            Debug.Log($"RoomExitController ({gameObject.name}): No overlay active - doing room transition");
            GameManager.I.TransitionToNextRoom();
        }
    }
}