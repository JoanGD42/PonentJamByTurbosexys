using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Configurable screen-space region (center-bottom) that shows a highlight when hovered and triggers room change on click.
/// Attach this to a Persistent UI GameObject (e.g., Root Canvas) and assign a highlight Image or GameObject.
/// </summary>
public class RoomExitController : MonoBehaviour {
    [Header("Region (screen-space)")]
    [Tooltip("Width of the interactive region as a fraction of screen width (0..1)")]
    [Range(0.05f, 0.8f)]
    public float regionWidthFraction = 0.25f;
    [Tooltip("Height of the interactive region as fraction of screen height")]
    [Range(0.02f, 0.3f)]
    public float regionHeightFraction = 0.12f;
    [Tooltip("Vertical offset from bottom in pixels")]
    public float verticalOffset = 10f;

    [Header("Visual")]
    public GameObject highlightObject; // enable/disable to show the region

    void Start() {
        if (highlightObject != null) highlightObject.SetActive(false);
    }

    void Update() {
        if (GameManager.I == null) return;
        if (GameManager.I.IsInputBlocked) {
            if (highlightObject != null && highlightObject.activeSelf) highlightObject.SetActive(false);
            return;
        }

        if (InputManager.Instance == null) return;
        Vector2 pointer = InputManager.Instance.PointerScreenPosition;

        Rect region = CalculateRegion();
        bool inside = region.Contains(pointer);

        if (inside && highlightObject != null && !highlightObject.activeSelf) highlightObject.SetActive(true);
        else if (!inside && highlightObject != null && highlightObject.activeSelf) highlightObject.SetActive(false);

        if (inside && InputManager.Instance.WasClickThisFrame()) {
            // trigger scene transition
            GameManager.I.TransitionToNextRoom();
        }
    }

    Rect CalculateRegion() {
        float w = Screen.width * regionWidthFraction;
        float h = Screen.height * regionHeightFraction;
        float x = (Screen.width - w) / 2f;
        float y = verticalOffset;
        return new Rect(x, y, w, h);
    }

    // Debug draw in Scene view
    void OnDrawGizmosSelected() {
        var r = CalculateRegion();
        Gizmos.color = Color.cyan;
        // draw approximate rect in world-space by mapping corners through Camera.ScreenToWorldPoint
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 a = cam.ScreenToWorldPoint(new Vector3(r.xMin, r.yMin, cam.nearClipPlane));
        Vector3 b = cam.ScreenToWorldPoint(new Vector3(r.xMax, r.yMin, cam.nearClipPlane));
        Vector3 c = cam.ScreenToWorldPoint(new Vector3(r.xMax, r.yMax, cam.nearClipPlane));
        Vector3 d = cam.ScreenToWorldPoint(new Vector3(r.xMin, r.yMax, cam.nearClipPlane));
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
    }
}