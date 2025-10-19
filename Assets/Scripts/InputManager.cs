using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }

    // InputActions created at runtime so you don't need an asset to start.
    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction cancelAction;

    // State updated by actions
    private Vector2 pointerScreenPos;
    private bool clickedThisFrame;
    private bool cancelThisFrame;

    // Events other systems can subscribe to
    public event Action Clicked;
    public event Action CancelPressed;

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateActions();
    }

    void OnEnable() {
        pointAction.Enable();
        clickAction.Enable();
        cancelAction.Enable();

        clickAction.performed += OnClickPerformed;
        cancelAction.performed += OnCancelPerformed;
    }

    void OnDisable() {
        // Add null checks to prevent infinite error loop when singleton destroys duplicate instances
        if (clickAction != null) {
            clickAction.performed -= OnClickPerformed;
            clickAction.Disable();
        }
        
        if (cancelAction != null) {
            cancelAction.performed -= OnCancelPerformed;
            cancelAction.Disable();
        }

        if (pointAction != null) {
            pointAction.Disable();
        }
    }

    void CreateActions() {
        // Point (mouse/touch/gamepad virtual pointer)
        pointAction = new InputAction("Point", InputActionType.PassThrough, binding: "<Pointer>/position");
        // Click: mouse left, touchscreen primary touch, gamepad south (A / X)
        clickAction = new InputAction("Click", InputActionType.Button);
        clickAction.AddBinding("<Mouse>/leftButton");
        clickAction.AddBinding("<Touchscreen>/primaryTouch/press");
        clickAction.AddBinding("<Gamepad>/buttonSouth");

        // Cancel / Back (escape, gamepad B)
        cancelAction = new InputAction("Cancel", InputActionType.Button);
        cancelAction.AddBinding("<Keyboard>/escape");
        cancelAction.AddBinding("<Gamepad>/buttonEast");
    }

    void Update() {
        // Update pointer position every frame from the point action.
        if (Gamepad.current != null && Pointer.current == null) {
            // some platforms may not have a Pointer; fall back to gamepad "virtual pointer" if desired
        }

        // Read pointer
        if (pointAction.enabled) {
            pointerScreenPos = pointAction.ReadValue<Vector2>();
        } else {
            pointerScreenPos = Vector2.zero;
        }

        // clickedThisFrame is set in callback; clear here after others had a chance
        // (we let other systems check it during the frame before it's cleared)
        // Note: callbacks run before Update for input system, so this is safe.
    }

    void LateUpdate() {
        // clear per-frame flags, after all Update() calls in the frame have run.
        clickedThisFrame = false;
        cancelThisFrame = false;
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx) {
        if (GameManager.I != null && GameManager.I.IsInputBlocked) return;
        clickedThisFrame = true;
        Clicked?.Invoke();
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx) {
        if (GameManager.I != null && GameManager.I.IsInputBlocked) return;
        cancelThisFrame = true;
        CancelPressed?.Invoke();
    }

    // public helpers

    /// <summary> Pointer in screen space (pixels) — use Camera.main.ScreenToWorldPoint when needed. </summary>
    public Vector2 PointerScreenPosition => pointerScreenPos;

    /// <summary> Was the primary click pressed this frame? (consumable-ish; returns true only if it occurred since last LateUpdate) </summary>
    public bool WasClickThisFrame() => clickedThisFrame;

    /// <summary> Subscribe or poll; useful for gamepad/keyboard support too. </summary>
    public bool WasCancelThisFrame() => cancelThisFrame;
}