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
    public void TransitionToNextRoom() {
        if (State != GameState.Playing) return;
        if (roomsOrdered == null || roomsOrdered.Count == 0) return;
        
        // Cycle through rooms using modulo (kitchen ↔ dorm ↔ kitchen...)
        int nextIndex = (activeRoomIndex + 1) % roomsOrdered.Count;
        StartCoroutine(TransitionRoutine(nextIndex));
    }

    private IEnumerator TransitionRoutine(int nextIndex) {
        BlockInput();

        var currentScene = ActiveRoomSceneName;
        sceneLoader.SetSceneActiveObjects(currentScene, false);

        activeRoomIndex = nextIndex;
        var nextScene = ActiveRoomSceneName;
        sceneLoader.SetSceneActiveObjects(nextScene, true);

        // tiny settle time
        yield return new WaitForSeconds(0.2f);

        UnblockInput();
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