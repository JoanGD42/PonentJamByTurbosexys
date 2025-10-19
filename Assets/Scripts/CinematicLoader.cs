using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CinematicLoader : MonoBehaviour {
    [Header("Overlay System")]
    public Canvas[] overlayCanvases;  // Multiple overlay canvases for different contexts
    public float fadeSpeed = 2f;
    
    private bool isOverlayActive = false;
    private string currentOverlayContext = "";
    private Canvas currentActiveCanvas = null;
    
    // State tracking for persistent changes
    private HashSet<string> modifiedItems = new HashSet<string>();
    
    public bool IsOverlayActive => isOverlayActive;
    public string CurrentOverlayContext => currentOverlayContext;

    void Start() {
        // Ensure all overlay canvases start hidden
        if (overlayCanvases != null) {
            for (int i = 0; i < overlayCanvases.Length; i++) {
                if (overlayCanvases[i] != null) {
                    Debug.Log($"CinematicLoader: Hiding overlay canvas {i} ({overlayCanvases[i].name}) on Start");
                    overlayCanvases[i].gameObject.SetActive(false);
                }
            }
        }
        Debug.Log("CinematicLoader: All overlay canvases should now be hidden");
    }

    public IEnumerator PlayCinematic(string cinematicId) {
        Debug.Log($"Playing cinematic for: {cinematicId}");
        
        switch (cinematicId) {
            case "cinematic_fridge_first":
                yield return PlayFridgeCinematic();
                break;
                
            case "close_up_closet_girl":
                yield return PlayGirlClosetCinematic();
                break;
                
            case "cinematic_jacket_first":
                yield return PlayJacketCinematic();
                break;
                
            case "close_up_closet":
                yield return PlayParentsClosetCinematic();
                break;
                
            case "nightstand_dad":
                yield return PlayNightstandCinematic("mesita pare", 5); // Canvas 5 for dad's nightstand
                break;
                
            case "nightstand_mom":
                yield return PlayNightstandCinematic("mesita mare", 6); // Canvas 6 for mom's nightstand
                break;
                
            default:
                Debug.LogWarning($"No cinematic behavior defined for: {cinematicId}");
                yield break;
        }
    }
    
    private IEnumerator PlayFridgeCinematic() {
        Debug.Log("Fridge cinematic: Opening fridge door...");
        yield return ShowOverlay("fridge_initial");
    }
    
    private IEnumerator PlayGirlClosetCinematic() {
        Debug.Log("Girl's closet cinematic: Examining closet contents...");
        // string closetImage = modifiedItems.Contains("faldilla") ? "armari noia 2" : "armari noia 1";
        // yield return ShowOverlay(closetImage, closetImage == "armari noia 2" ? 1 : 0);
        yield return ShowOverlay("armari noia 1", 0);   
    }
    
    private IEnumerator PlayJacketCinematic() {
        Debug.Log("Jacket cinematic: Touching father's jacket...");
        
        // Show detailed jacket view (canvas 3 for sniff jaqueta)
        yield return ShowOverlay("sniff jaqueta", 3);
        currentOverlayContext = "jacket_detail";
        
        // Play dialogue while overlay is shown
        if (GameManager.I?.dialogueLoader != null) {
            yield return GameManager.I.dialogueLoader.PlayDialogueCoroutine("dialogue_jacket_first");
        }
        
        // After dialogue, show empty closet and mark as modified
        yield return ShowOverlay("armari pares 2", 4);
        CollectItem("parents_jacket"); // Use the correct itemId that matches the button
        currentOverlayContext = "closet_empty";
    }
    
    private IEnumerator PlayParentsClosetCinematic() {
        Debug.Log("Parents' closet cinematic: Looking through parents' things...");
        
        // Show appropriate closet state based on whether jacket was taken
        string closetImage = modifiedItems.Contains("jaqueta") ? "armari pares 2" : "armari pares 1";
        yield return ShowOverlay(closetImage, closetImage == "armari pares 2" ? 4 : 2); // Use first canvas for closet
    }

    private IEnumerator PlayNightstandCinematic(string overlayName, int canvasIndex) {
        Debug.Log($"Nightstand cinematic: Examining {overlayName}...");
        yield return ShowOverlay(overlayName, overlayName == "mesita mare" ? 5 : 6);
    }
    
    private IEnumerator ShowOverlay(string overlayName) {
        yield return ShowOverlay(overlayName, 0); // Default to first canvas
    }
    
    public IEnumerator ShowOverlay(string overlayName, int canvasIndex) {
        if (overlayCanvases == null || canvasIndex >= overlayCanvases.Length) {
            Debug.LogError($"Overlay canvas {canvasIndex} not available!");
            yield break;
        }
        
        Canvas targetCanvas = overlayCanvases[canvasIndex];
        if (targetCanvas == null) {
            Debug.LogError($"Overlay canvas {canvasIndex} is null!");
            yield break;
        }
        
        // Get all Image components in this canvas
        Image[] overlayImages = targetCanvas.GetComponentsInChildren<Image>(true);
        if (overlayImages.Length == 0) {
            Debug.LogError($"No Image components found in overlay canvas {canvasIndex}!");
            yield break;
        }
        
        // Use the sprites already assigned in the Inspector
        List<Image> activeImages = new List<Image>();
        foreach (var img in overlayImages) {
            if (img.sprite != null) {
                activeImages.Add(img);
                Debug.Log($"Using pre-assigned sprite: {img.sprite.name} for image: {img.name}");
            } else {
                Debug.LogWarning($"Image {img.name} has no sprite assigned in Inspector!");
            }
        }
        
        if (activeImages.Count == 0) {
            Debug.LogWarning($"No images with sprites found in overlay canvas {canvasIndex}!");
            yield break;
        }
        
        // Activate the canvas and set state
        targetCanvas.gameObject.SetActive(true);
        currentActiveCanvas = targetCanvas;
        isOverlayActive = true;
        currentOverlayContext = overlayName;
        
        // Fade in all active images
        foreach (var img in activeImages) {
            Color color = img.color;
            color.a = 0f;
            img.color = color;
        }
        
        float alpha = 0f;
        while (alpha < 1f) {
            alpha += Time.deltaTime * fadeSpeed;
            foreach (var img in activeImages) {
                Color color = img.color;
                color.a = alpha;
                img.color = color;
            }
            yield return null;
        }
        
        // Ensure full alpha
        foreach (var img in activeImages) {
            Color color = img.color;
            color.a = 1f;
            img.color = color;
        }
    }
    
    public IEnumerator HideOverlay() {
        if (!isOverlayActive || currentActiveCanvas == null) yield break;
        
        // Get all images in the current active canvas
        Image[] overlayImages = currentActiveCanvas.GetComponentsInChildren<Image>(false);
        
        // Fade out all images
        float alpha = 1f;
        while (alpha > 0f) {
            alpha -= Time.deltaTime * fadeSpeed;
            foreach (var img in overlayImages) {
                if (img != null) {
                    Color color = img.color;
                    color.a = alpha;
                    img.color = color;
                }
            }
            yield return null;
        }
        
        // Ensure zero alpha and hide canvas
        foreach (var img in overlayImages) {
            if (img != null) {
                Color color = img.color;
                color.a = 0f;
                img.color = color;
            }
        }
        
        if (currentActiveCanvas != null) {
            currentActiveCanvas.gameObject.SetActive(false);
        }
        
        currentActiveCanvas = null;
        currentOverlayContext = "";
        // Set overlay as inactive only after it's completely closed
        isOverlayActive = false;
    }
    
    public bool HasItemBeenModified(string itemId) {
        return modifiedItems.Contains(itemId);
    }
    
    // Generic method to disable any button by itemId and mark item as collected
    private void CollectItem(string itemId) {
        // Add to modified items collection
        modifiedItems.Add(itemId);
        Debug.Log($"Item collected: {itemId}");
        
        // Find and disable the corresponding button
        DisableButtonForItem(itemId);
    }
    
    private void DisableButtonForItem(string itemId) {
        // Find all ItemInteractable components with the specified itemId
        ItemInteractable[] allInteractables = FindObjectsByType<ItemInteractable>(FindObjectsSortMode.None);
        foreach (var interactable in allInteractables) {
            if (interactable.itemId == itemId) {
                Debug.Log($"Disabling button for collected item: {itemId}");
                // Destroy the ItemInteractable component to prevent further interactions
                Destroy(interactable);
                break;
            }
        }
    }
}