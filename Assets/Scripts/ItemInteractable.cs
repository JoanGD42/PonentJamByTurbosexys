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
        
        // Debug logging to help identify setup issues
        Debug.Log($"ItemInteractable Start: {gameObject.name}, itemId: {itemId}, scene: {gameObject.scene.name}");
        Debug.Log($"  - Button: {(button != null ? "Found" : "MISSING")}, Interactable: {button?.interactable}");
        Debug.Log($"  - Image: {(buttonImage != null ? "Found" : "MISSING")}, RaycastTarget: {buttonImage?.raycastTarget}");
        Debug.Log($"  - Canvas: {GetComponentInParent<Canvas>()?.name}, Sort: {GetComponentInParent<Canvas>()?.sortingOrder}");
        Debug.Log($"  - FULL PATH: {GetFullPath(transform)}");
    }
    
    private string GetFullPath(Transform transform) {
        string path = transform.name;
        while (transform.parent != null) {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
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
            // OR if this object is in an overlay canvas (Root_Persistent scene)
            var myScene = gameObject.scene.name;
            if (myScene != GameManager.I.ActiveRoomSceneName && 
                myScene != "Root_Persistent") {
                shouldBeInteractable = false;
            }
        }
        
        if (button != null) {
            button.interactable = shouldBeInteractable;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Debug.Log($"ItemInteractable Hover Enter: {gameObject.name}, itemId: {itemId}");
        if (button != null && button.interactable && buttonImage != null) {
            buttonImage.color = new Color(1f, 1f, 0f, 0.05f);
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (buttonImage != null) {
            buttonImage.color = originalColor;
        }
    }

    private void OnButtonClick() {
        Debug.Log($"ItemInteractable Click: {gameObject.name}, itemId: {itemId}");
        if (GameManager.I == null || GameManager.I.IsInputBlocked) {
            Debug.Log($"Click blocked - GameManager null: {GameManager.I == null}, Input blocked: {GameManager.I?.IsInputBlocked}");
            return;
        }
        
        // Allow overlays to be opened multiple times
        // Only register interaction for one-time items (like narratives with permanent changes)
        bool shouldRegisterInteraction = true;
        
        var itemData = GameManager.I.FindItem(itemId);
        if (itemData != null && !string.IsNullOrEmpty(itemData.cinematicId)) {
            // Check if this is a repeatable overlay interaction
            string cinematicId = itemData.cinematicId;
            if (cinematicId == "close_up_closet_girl" || 
                cinematicId == "close_up_closet" || 
                cinematicId == "nightstand_dad" || 
                cinematicId == "nightstand_mom" ||
                cinematicId == "cinematic_fridge_first") {
                shouldRegisterInteraction = false; // These can be repeated
            }
        }
        
        // For one-time interactions, check if already done
        if (shouldRegisterInteraction && GameManager.I.HasInteracted(itemId)) { 
            return; 
        }

        if (shouldRegisterInteraction) {
            GameManager.I.RegisterInteraction(itemId);
        }
        
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