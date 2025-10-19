using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum GameState {
    Menu,
    Loading,
    Playing,
    Paused
}

public class GameManager : MonoBehaviour {
    public static GameManager I { get; private set; }

    [Header("Manifest")]
    public string manifestResourcePath = "game_manifest";
    public GameManifest manifest;

    [Header("Systems")]
    public SceneLoader sceneLoader;
    public DialogueLoader dialogueLoader;
    public CinematicLoader cinematicLoader;

    // runtime state
    private HashSet<string> interactedItems = new HashSet<string>();
    private HashSet<string> collectedItems = new HashSet<string>(); // Persistent collected items

    // Room ordering derived from manifest order
    private List<RoomData> roomsOrdered;
    private int activeRoomIndex = 0;

    public GameState State { get; private set; } = GameState.Menu;

    public bool IsInputBlocked { get; private set; } = false;

    // convenience accessors
    public RoomData ActiveRoom => (roomsOrdered != null && activeRoomIndex >= 0 && activeRoomIndex < roomsOrdered.Count) ? roomsOrdered[activeRoomIndex] : null;
    public string ActiveRoomSceneName => ActiveRoom != null ? ActiveRoom.sceneName : null;
    public string ActiveRoomId => ActiveRoom != null ? ActiveRoom.id : null;

    void Awake() {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        manifest = GameManifestLoader.LoadFromResources(manifestResourcePath);

        // find system references if not manually assigned (cleaner pattern)
        sceneLoader = sceneLoader ?? FindFirstObjectByType<SceneLoader>();
        dialogueLoader = dialogueLoader ?? FindFirstObjectByType<DialogueLoader>();
        cinematicLoader = cinematicLoader ?? FindFirstObjectByType<CinematicLoader>();

        // start in Menu state; no scenes loaded yet
        State = GameState.Menu;
    }

    #region Game Start / Loading
    /// <summary>
    /// Called by the Main Menu Start button to transition to loading screen, then preload scenes.
    /// </summary>
    public void StartGame() {
        if (State != GameState.Menu) return;
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine() {
        State = GameState.Loading;
        
        if (sceneLoader == null) {
            Debug.LogError("SceneLoader not found. Assign SceneLoader in GameManager inspector.");
            yield break;
        }
        
        if (manifest == null) {
            Debug.LogError("Manifest missing. Cannot start game.");
            yield break;
        }

        roomsOrdered = manifest.rooms.ToList();

        // Preload all room scenes additively
        var sceneNames = roomsOrdered.Select(r => r.sceneName);
        yield return sceneLoader.LoadScenesAdditiveRoutine(sceneNames);

        // After loaded, disable all room scenes then enable the initial room
        foreach (var r in roomsOrdered) {
            sceneLoader.SetSceneActiveObjects(r.sceneName, false);
        }

        // Start in kitchen if present (manifest order assumed; otherwise first room)
        int startIndex = roomsOrdered.FindIndex(r => r.id == "kitchen" || (r.sceneName != null && r.sceneName.ToLower().Contains("kitchen")));
        if (startIndex < 0) startIndex = 0;
        activeRoomIndex = startIndex;

        // Show the starting room
        sceneLoader.SetSceneActiveObjects(ActiveRoomSceneName, true);

        State = GameState.Playing;
        yield break;
    }
    #endregion

    #region Interaction state
    public bool HasInteracted(string itemId) => interactedItems.Contains(itemId);

    public void RegisterInteraction(string itemId) {
        if (!string.IsNullOrEmpty(itemId)) interactedItems.Add(itemId);
    }

    public void BlockInput() => IsInputBlocked = true;
    public void UnblockInput() => IsInputBlocked = false;
    #endregion

    #region Room transitions
    public void TransitionToNextRoom(int? forceIndex = null) {
        if (State != GameState.Playing) return;
        if (roomsOrdered == null || roomsOrdered.Count == 0) return;
        if (IsInputBlocked) {
            Debug.Log("GameManager: Transition blocked - input is already blocked (transition in progress)");
            return;
        }
        
        // Block input immediately to prevent rapid clicks
        BlockInput();
        
        // Use forced index or cycle through rooms using modulo (kitchen ↔ dorm ↔ kitchen...)
        int nextIndex = forceIndex ?? (activeRoomIndex + 1) % roomsOrdered.Count;
        Debug.Log($"GameManager: Transitioning from index {activeRoomIndex} to index {nextIndex} (forced: {forceIndex})");
        StartCoroutine(TransitionRoutine(nextIndex));
    }

    private IEnumerator TransitionRoutine(int nextIndex) {
        Debug.Log($"GameManager: Starting transition routine to index {nextIndex}");

        var currentScene = ActiveRoomSceneName;
        Debug.Log($"GameManager: Hiding current scene: {currentScene}");
        sceneLoader.SetSceneActiveObjects(currentScene, false);

        activeRoomIndex = nextIndex;
        var nextScene = ActiveRoomSceneName;
        Debug.Log($"GameManager: Showing next scene: {nextScene}");
        sceneLoader.SetSceneActiveObjects(nextScene, true);

        // Clear EventSystem selection to prevent phantom clicks on new scene's buttons
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null) {
            eventSystem.SetSelectedGameObject(null);
        }

        // tiny settle time
        yield return new WaitForSeconds(0.2f);
        
        Debug.Log($"GameManager: Transition completed to {ActiveRoomSceneName}");
        UnblockInput();
    }
    #endregion

    #region Collected Items Management
    public void CollectItem(string itemId) {
        collectedItems.Add(itemId);
        Debug.Log($"GameManager: Item collected and persisted: {itemId}");
    }
    
    public bool HasItemBeenCollected(string itemId) {
        return collectedItems.Contains(itemId);
    }
    #endregion

    public ItemData FindItem(string itemId) {
        if (manifest == null || manifest.rooms == null) return null;
        foreach (var r in manifest.rooms) {
            var it = r.items.Find(x => x.id == itemId);
            if (it != null) return it;
        }
        return null;
    }

    // Quit helper
    public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}