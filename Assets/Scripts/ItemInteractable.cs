using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ItemInteractable : MonoBehaviour {
    [Header("Data")]
    public string itemId; // must match manifest id (e.g. "kitchen_fridge")
    public GameObject highlightObject;

    private bool isPointerOver = false;

    void Start() {
        if (highlightObject != null) highlightObject.SetActive(false);
    }

    void Update() {
        if (GameManager.I == null) return;
        if (GameManager.I.IsInputBlocked) {
            if (highlightObject != null && highlightObject.activeSelf) highlightObject.SetActive(false);
            return;
        }

        // Only respond if this object's scene is the active room's scene
        var myScene = gameObject.scene.name;
        if (myScene != GameManager.I.ActiveRoomSceneName) {
            if (isPointerOver) {
                // cleanup when pointer moves off while room inactive
                isPointerOver = false;
                if (highlightObject != null) highlightObject.SetActive(false);
            }
            return;
        }

        // Use InputManager for pointer
        if (InputManager.Instance == null) return;
        Vector2 screenPos = InputManager.Instance.PointerScreenPosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
        Vector2 point = new Vector2(worldPos.x, worldPos.y);

        bool hitThis = GetComponent<Collider2D>().OverlapPoint(point);
        if (hitThis && !isPointerOver) OnPointerEnter();
        else if (!hitThis && isPointerOver) OnPointerExit();

        if (isPointerOver && InputManager.Instance.WasClickThisFrame()) {
            OnClicked();
        }
    }

    void OnPointerEnter() {
        isPointerOver = true;
        if (highlightObject != null) highlightObject.SetActive(true);
    }

    void OnPointerExit() {
        isPointerOver = false;
        if (highlightObject != null) highlightObject.SetActive(false);
    }

    void OnClicked() {
        // If already interacted, do nothing (or you can play secondary response)
        if (GameManager.I.HasInteracted(itemId)) { return; }

        GameManager.I.RegisterInteraction(itemId);
        StartCoroutine(HandleInteractionCoroutine());
    }

    IEnumerator HandleInteractionCoroutine() {
        GameManager.I.BlockInput();

        var itemData = GameManager.I.FindItem(itemId);

        if (itemData != null && !string.IsNullOrEmpty(itemData.cinematicId)) {
            yield return GameManager.I.cinematicLoader.PlayCinematic(itemData.cinematicId);
        }

        if (itemData != null && !string.IsNullOrEmpty(itemData.dialogueId)) {
            yield return GameManager.I.dialogueLoader.PlayDialogueCoroutine(itemData.dialogueId);
        }

        GameManager.I.UnblockInput();
    }
}