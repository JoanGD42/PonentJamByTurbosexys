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
        // Handle input blocking state changes
        if (GameManager.I != null && GameManager.I.IsInputBlocked) {
            if (button != null) button.interactable = false;
        } else {
            if (button != null) button.interactable = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (GameManager.I != null && GameManager.I.IsInputBlocked) return;
        if (buttonImage != null) buttonImage.color = Color.yellow;
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (buttonImage != null) buttonImage.color = originalColor;
    }

    private void OnButtonClick() {
        if (GameManager.I == null || GameManager.I.IsInputBlocked) return;
        
        // Check if we're in overlay mode
        if (cinematicLoader != null && cinematicLoader.IsOverlayActive) {
            // Close overlay instead of transitioning rooms
            StartCoroutine(CloseOverlay());
        } else {
            // Normal room transition
            GameManager.I.TransitionToNextRoom();
        }
    }
    
    private IEnumerator CloseOverlay() {
        if (cinematicLoader != null) {
            yield return cinematicLoader.HideOverlay();
        }
    }
}