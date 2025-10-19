using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CinematicLoader : MonoBehaviour {
    [Header("Overlay System")]
    public Canvas overlayCanvas;
    public Image overlayImage;
    public float fadeSpeed = 2f;
    
    private bool isOverlayActive = false;
    private string currentOverlayContext = "";
    
    // State tracking for persistent changes
    private HashSet<string> modifiedItems = new HashSet<string>();
    
    public bool IsOverlayActive => isOverlayActive;
    public string CurrentOverlayContext => currentOverlayContext;

    void Start() {
        // Ensure overlay starts hidden
        if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(false);
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
                
            default:
                Debug.LogWarning($"No cinematic behavior defined for: {cinematicId}");
                yield break;
        }
    }
    
    private IEnumerator PlayFridgeCinematic() {
        Debug.Log("Fridge cinematic: Opening fridge door...");
        yield return ShowOverlay("fridge_initial");
        yield return new WaitForSeconds(2f);
        yield return HideOverlay();
    }
    
    private IEnumerator PlayGirlClosetCinematic() {
        Debug.Log("Girl's closet cinematic: Examining closet contents...");
        yield return ShowOverlay("closet_girl_initial");
        yield return new WaitForSeconds(1.5f);
        yield return HideOverlay();
    }
    
    private IEnumerator PlayJacketCinematic() {
        Debug.Log("Jacket cinematic: Touching father's jacket...");
        
        // Show detailed jacket view
        yield return ShowOverlay("jacket_detail");
        currentOverlayContext = "jacket_detail";
        
        // Play dialogue while overlay is shown
        if (GameManager.I?.dialogueLoader != null) {
            yield return GameManager.I.dialogueLoader.PlayDialogueCoroutine("dialogue_jacket_first");
        }
        
        // After dialogue, show empty closet and mark as modified
        yield return ShowOverlay("closet_final");
        modifiedItems.Add("parents_jacket");
        currentOverlayContext = "closet_empty";
        
        // Stay in overlay mode - player must use exit arrow to return
    }
    
    private IEnumerator PlayParentsClosetCinematic() {
        Debug.Log("Parents' closet cinematic: Looking through parents' things...");
        
        // Show appropriate closet state based on whether jacket was taken
        string closetImage = modifiedItems.Contains("parents_jacket") ? "closet_final" : "closet_initial";
        yield return ShowOverlay(closetImage);
        currentOverlayContext = "closet_view";
        
        // Stay in overlay mode - player can interact with items or exit
    }
    
    private IEnumerator ShowOverlay(string imageName) {
        if (overlayCanvas == null || overlayImage == null) {
            Debug.LogError("Overlay components not assigned!");
            yield break;
        }
        
        // Load the image from Resources
        Sprite overlaySprite = Resources.Load<Sprite>(imageName);
        if (overlaySprite == null) {
            Debug.LogWarning($"Overlay image not found: {imageName}");
            yield break;
        }
        
        // Set up the overlay
        overlayImage.sprite = overlaySprite;
        overlayCanvas.gameObject.SetActive(true);
        isOverlayActive = true;
        
        // Fade in effect
        Color color = overlayImage.color;
        color.a = 0f;
        overlayImage.color = color;
        
        while (color.a < 1f) {
            color.a += Time.deltaTime * fadeSpeed;
            overlayImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        overlayImage.color = color;
    }
    
    public IEnumerator HideOverlay() {
        if (!isOverlayActive || overlayImage == null) yield break;
        
        // Fade out effect
        Color color = overlayImage.color;
        
        while (color.a > 0f) {
            color.a -= Time.deltaTime * fadeSpeed;
            overlayImage.color = color;
            yield return null;
        }
        
        color.a = 0f;
        overlayImage.color = color;
        
        if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(false);
        isOverlayActive = false;
        currentOverlayContext = "";
    }
    
    public bool HasItemBeenModified(string itemId) {
        return modifiedItems.Contains(itemId);
    }
}