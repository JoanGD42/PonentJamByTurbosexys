using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour {
    [Header("UI")]
    public GameObject menuRoot; // root GameObject that contains the menu (Canvas child)
    public Button startButton;
    public Button optionsButton;
    public Button exitButton;
    public TextMeshProUGUI titleText; // optional; if you prefer Text, use Text instead

    void Start() {
        // Safety: ensure menu shows on game start
        if (menuRoot != null) menuRoot.SetActive(true);

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
        // Hide the menu immediately
        if (menuRoot != null) menuRoot.SetActive(false);

        // Kick off GameManager's StartGame
        if (GameManager.I != null) GameManager.I.StartGame();
    }

    private void OnOptionsClicked() {
        // intentionally does nothing for now
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