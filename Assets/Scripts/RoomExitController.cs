using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Simple UI Button at center-bottom that triggers room transitions.
/// Uses Unity's built-in hover detection and yellow color highlight.
/// </summary>
public class RoomExitController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    private Button button;
    private Image buttonImage;
    private Color originalColor;

    void Start() {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        
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
        if (GameManager.I != null && !GameManager.I.IsInputBlocked) {
            GameManager.I.TransitionToNextRoom();
        }
    }
}