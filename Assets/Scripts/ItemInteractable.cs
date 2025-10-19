using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Item interaction using Unity Button component with yellow hover highlighting.
/// Attach to a Button GameObject in the room scenes.
/// </summary>
public class ItemInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Data")]
    public string itemId; // must match manifest id (e.g. "kitchen_fridge")

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
        // Handle input blocking and scene activity
        bool shouldBeInteractable = true;
        
        if (GameManager.I == null) {
            shouldBeInteractable = false;
        } else if (GameManager.I.IsInputBlocked) {
            shouldBeInteractable = false;
        } else {
            // Only respond if this object's scene is the active room's scene
            var myScene = gameObject.scene.name;
            if (myScene != GameManager.I.ActiveRoomSceneName) {
                shouldBeInteractable = false;
            }
        }
        
        if (button != null) {
            button.interactable = shouldBeInteractable;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (button != null && button.interactable && buttonImage != null) {
            buttonImage.color = Color.yellow;
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (buttonImage != null) {
            buttonImage.color = originalColor;
        }
    }

    private void OnButtonClick() {
        if (GameManager.I == null || GameManager.I.IsInputBlocked) return;
        
        // If already interacted, do nothing (or you can play secondary response)
        if (GameManager.I.HasInteracted(itemId)) { return; }

        GameManager.I.RegisterInteraction(itemId);
        StartCoroutine(HandleInteractionCoroutine());
    }

    IEnumerator HandleInteractionCoroutine() {
        GameManager.I.BlockInput();

        var itemData = GameManager.I.FindItem(itemId);
        if (itemData == null) {
            GameManager.I.UnblockInput();
            yield break;
        }

        // Play cinematic if present
        if (!string.IsNullOrEmpty(itemData.cinematicId)) {
            yield return GameManager.I.cinematicLoader.PlayCinematic(itemData.cinematicId);
            
        // Cinematic not present, play dialogue if present
        } else if (!string.IsNullOrEmpty(itemData.dialogueId)) {
            yield return GameManager.I.dialogueLoader.PlayDialogueCoroutine(itemData.dialogueId);
        }

        GameManager.I.UnblockInput();
    }
}