using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CinematicLoader : MonoBehaviour {
    [Header("Overlay System")]
    public Canvas[] overlayCanvases;  // Multiple overlay canvases for different contexts
    public float fadeSpeed = 2f;
    
    private string currentOverlayContext = "";
    private Canvas currentActiveCanvas = null;
    
    public string CurrentOverlayContext => currentOverlayContext;

    void Start() {
        // Ensure all overlay canvases start with raycasters disabled (keep canvases active for reuse)
        if (overlayCanvases != null) {
            for (int i = 0; i < overlayCanvases.Length; i++) {
                if (overlayCanvases[i] != null) {
                    var raycaster = overlayCanvases[i].GetComponent<GraphicRaycaster>();
                    if (raycaster != null) {
                        raycaster.enabled = false;
                        Debug.Log($"CinematicLoader: Disabled raycaster for canvas {i} ({overlayCanvases[i].name}) on Start");
                    }
                }
            }
        }
        Debug.Log("CinematicLoader: All overlay canvases initialized");
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
        // Check if jacket was already collected - prevent re-running
        if (GameManager.I.HasItemBeenCollected("parents_jacket")) {
            Debug.Log("Jacket cinematic: Item already collected, skipping cinematic");
            yield break;
        }
        
        // Block input during entire cinematic to prevent phantom clicks
        GameManager.I.BlockInput();
        Debug.Log("Jacket cinematic: Input blocked for entire cinematic");
        
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
        currentOverlayContext = "closet_empty";
        
        // Wait a moment then hide overlay and complete cinematic
        Debug.Log("Jacket cinematic: Waiting 1 second before completing...");
        yield return new WaitForSeconds(1f);
        Debug.Log("Jacket cinematic: Hiding overlay...");
        
        // HARDCODED cleanup for jacket cinematic: fade out both canvas 3 and 4
        yield return HideJacketCinematicOverlays();
        
        Debug.Log("Jacket cinematic: HideOverlay coroutine completed");
        
        // Collect item and complete cinematic
        CollectItem("parents_jacket"); 
        
        Debug.Log("Jacket cinematic: Completed successfully!");
        
        // Unblock input - cinematic is done
        GameManager.I.UnblockInput();
        Debug.Log("Jacket cinematic: Input unblocked, cinematic complete");
    }
    
    private IEnumerator HideJacketCinematicOverlays() {
        // Hardcoded cleanup for jacket cinematic: fade out canvas 3 (sniff jaqueta) and canvas 4 (armari pares 2)
        Debug.Log("HideJacketCinematicOverlays: Starting fade out on canvases 3 and 4");
        
        List<Canvas> canvasesToHide = new List<Canvas> { overlayCanvases[3], overlayCanvases[4] };
        
        // Fade out both canvases simultaneously
        float alpha = 1f;
        while (alpha > 0f) {
            alpha -= Time.deltaTime * fadeSpeed;
            
            foreach (var canvas in canvasesToHide) {
                if (canvas == null) continue;
                
                Image[] images = canvas.GetComponentsInChildren<Image>(true);
                foreach (var img in images) {
                    if (img != null) {
                        Color color = img.color;
                        color.a = alpha;
                        img.color = color;
                    }
                }
            }
            yield return null;
        }
        
        // Disable raycasters on cinematic canvases (3 and 4 only)
        foreach (var canvas in canvasesToHide) {
            if (canvas == null) continue;
            
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) {
                raycaster.enabled = false;
            }
        }
        
        // NOTE: Canvas 2 (armari pares 1) stays enabled - it's the base room overlay
        // The exit button is on this canvas, so we need it to remain clickable!
        
        currentActiveCanvas = null;
        currentOverlayContext = "";
        Debug.Log("HideJacketCinematicOverlays: Completed");
    }
    
    private IEnumerator PlayParentsClosetCinematic() {
        Debug.Log("Parents' closet cinematic: Looking through parents' things...");
        
        // Show appropriate closet state based on whether jacket was taken
        string closetImage = GameManager.I.HasItemBeenCollected("parents_jacket") ? "armari pares 2" : "armari pares 1";
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
        
        // Enable raycasting on this canvas so it can receive clicks
        var raycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null) {
            raycaster.enabled = true;
        }
        
        currentActiveCanvas = targetCanvas;
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
        if (currentActiveCanvas == null) {
            Debug.Log($"CinematicLoader.HideOverlay: Early exit - no active canvas");
            yield break;
        }
        
        Debug.Log($"CinematicLoader.HideOverlay: Starting fade out on canvas {currentActiveCanvas.name}");
        
        // Fade out the current active canvas
        Image[] overlayImages = currentActiveCanvas.GetComponentsInChildren<Image>(true);
        
        // Fade out
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
        
        // Ensure zero alpha
        foreach (var img in overlayImages) {
            if (img != null) {
                Color color = img.color;
                color.a = 0f;
                img.color = color;
            }
        }
        
        // Disable raycaster
        var raycaster = currentActiveCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null) {
            raycaster.enabled = false;
        }
        
        currentActiveCanvas = null;
        currentOverlayContext = "";
        Debug.Log($"CinematicLoader.HideOverlay: Completed");
    }
    
    public bool HasItemBeenModified(string itemId) {
        return GameManager.I.HasItemBeenCollected(itemId);
    }
    
    // Generic method to disable any button by itemId and mark item as collected
    private void CollectItem(string itemId) {
        // Add to GameManager's persistent collection
        GameManager.I.CollectItem(itemId);
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