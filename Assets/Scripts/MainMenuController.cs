using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour {
    [Header("UI")]
    public GameObject menuRoot;           // the whole menu panel
    public Button startButton;
    public Button optionsButton;
    public Button exitButton;
    public TextMeshProUGUI titleText;

    [Header("Loading UI")]
    public GameObject loadingRoot;        // a small panel containing spinner/text
    public GameObject loadingSpinner;     // optional rotating image or icon
    public TextMeshProUGUI loadingText;   // optional "Loading..."
    [Tooltip("Maximum seconds to wait for the game to report it's ready (to avoid infinite waits).")]
    public float loadingTimeout = 15f;
    [Tooltip("Minimum time to show the loading UI so it doesn't just blink.")]
    public float minLoadingShow = 0.5f;

    void Start() {
        if (menuRoot != null) menuRoot.SetActive(true);
        if (loadingRoot != null) loadingRoot.SetActive(false);

        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptionsClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
    }

    void OnDestroy() {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptionsClicked);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnStartClicked() {
        // Start the coroutine that handles loading + UI
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence() {
        // Defensive checks
        if (GameManager.I == null) {
            Debug.LogError("GameManager not found. Cannot start game.");
            yield break;
        }

        // Show loading UI
        if (menuRoot != null) menuRoot.SetActive(true); // keep menu visible while loading
        if (loadingRoot != null) loadingRoot.SetActive(true);

        if (startButton != null) startButton.interactable = false;
        if (optionsButton != null) optionsButton.interactable = false;
        if (exitButton != null) exitButton.interactable = false;

        float startTime = Time.time;
        // Kick off game startup (non-blocking; GameManager will change its State)
        GameManager.I.StartGame();

        // Wait until GameManager reports Playing, up to timeout
        bool success = false;
        while (Time.time - startTime < loadingTimeout) {
            if (GameManager.I != null && GameManager.I.State == GameState.Playing) {
                success = true;
                break;
            }
            yield return null;
        }

        // ensure minimum visible time for loading UI
        float elapsed = Time.time - startTime;
        if (elapsed < minLoadingShow) yield return new WaitForSeconds(minLoadingShow - elapsed);

        if (success) {
            // Hide menu and loading UI, game should be running now
            if (menuRoot != null) menuRoot.SetActive(false);
            if (loadingRoot != null) loadingRoot.SetActive(false);
        } else {
            // Loading failed / timed out
            Debug.LogError($"StartGame timed out after {loadingTimeout}s. Re-enabling menu.");
            if (loadingText != null) loadingText.text = "Loading failed. Try again.";
            if (startButton != null) startButton.interactable = true;
            if (optionsButton != null) optionsButton.interactable = true;
            if (exitButton != null) exitButton.interactable = true;
            // keep menuRoot visible so user can retry
        }
    }

    private void OnOptionsClicked() {
        Debug.Log("Options pressed (noop).");
    }

    private void OnExitClicked() {
        if (GameManager.I != null) GameManager.I.QuitGame();
        else {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}