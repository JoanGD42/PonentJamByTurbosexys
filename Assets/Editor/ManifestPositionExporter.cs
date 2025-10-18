#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.Callbacks;

/// <summary>
/// ManifestPositionExporter
/// - Tools -> Manifest -> Export All Positions
/// - Tools -> Manifest -> Export Current Scene Positions
/// - Tools -> Manifest -> Toggle Auto Export On Play
///
/// Exports ItemInteractable.transform.position for items defined in Resources/game_manifest.json.
/// Creates a backup game_manifest.json.bak before writing.
/// </summary>
public static class ManifestPositionExporter {
    private const string manifestResourcePath = "Assets/Resources/game_manifest.json";
    private const string autoExportPrefKey = "ManifestExporter.AutoExportOnPlay";

    [MenuItem("Tools/Manifest/Export All Positions")]
    public static void ExportAllPositionsMenu() {
        ExportPositionsFromScenes(false);
    }

    [MenuItem("Tools/Manifest/Export Current Scene Positions")]
    public static void ExportCurrentScenePositionsMenu() {
        ExportPositionsFromScenes(true);
    }

    [MenuItem("Tools/Manifest/Toggle Auto Export On Play")]
    public static void ToggleAutoExportOnPlay() {
        bool current = EditorPrefs.GetBool(autoExportPrefKey, false);
        EditorPrefs.SetBool(autoExportPrefKey, !current);
        Debug.Log($"Manifest Exporter: Auto Export On Play set to {!current}");
    }

    /// <summary>
    /// Main exporter. If currentSceneOnly == true, only updates the manifest room that matches the currently open scene.
    /// Otherwise loops through all rooms defined in the manifest and opens scenes additively to gather positions.
    /// </summary>
    public static void ExportPositionsFromScenes(bool currentSceneOnly) {
        if (!File.Exists(manifestResourcePath)) {
            Debug.LogError($"Manifest not found at {manifestResourcePath}. Create Resources/game_manifest.json first.");
            return;
        }

        string json = File.ReadAllText(manifestResourcePath);
        GameManifest manifest;
        try {
            manifest = JsonUtility.FromJson<GameManifest>(json);
        } catch {
            Debug.LogError("Failed to parse manifest JSON.");
            return;
        }

        if (manifest == null || manifest.rooms == null) {
            Debug.LogError("Manifest appears empty or malformed.");
            return;
        }

        // Backup original manifest
        string backupPath = manifestResourcePath + ".bak";
        File.Copy(manifestResourcePath, backupPath, true);
        Debug.Log($"ManifestPositionExporter: Backed up manifest to {backupPath}");

        var originalScene = EditorSceneManager.GetActiveScene();

        if (currentSceneOnly) {
            // Only update the manifest entry that matches the currently open scene name
            var open = EditorSceneManager.GetActiveScene();
            if (!open.IsValid() || string.IsNullOrEmpty(open.name)) {
                Debug.LogError("No valid active scene to export from.");
                return;
            }

            bool updated = UpdateManifestFromScene(open, manifest);
            if (!updated) Debug.LogWarning($"No manifest room matched the open scene '{open.name}'. Nothing updated.");
        } else {
            // Loop through all manifest rooms, find scene assets by name, open them additively, gather positions, close them.
            for (int i = 0; i < manifest.rooms.Count; i++) {
                var room = manifest.rooms[i];
                if (string.IsNullOrEmpty(room.sceneName)) {
                    Debug.LogWarning($"Room {room.id} has no sceneName; skipping.");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets(room.sceneName + " t:scene");
                if (guids == null || guids.Length == 0) {
                    Debug.LogWarning($"Scene '{room.sceneName}' not found in project; skipping room '{room.id}'.");
                    continue;
                }

                string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                Debug.Log($"Opened scene '{room.sceneName}' ({scenePath})");

                // Update manifest from opened scene
                UpdateManifestFromScene(opened, manifest);

                // Close the scene we opened to avoid clutter
                EditorSceneManager.CloseScene(opened, true);
            }
        }

        // Write manifest back
        string outJson = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(manifestResourcePath, outJson);
        AssetDatabase.Refresh();
        Debug.Log($"ManifestPositionExporter: Export complete. Manifest updated at {manifestResourcePath}");

        // restore original active scene (best-effort)
        if (originalScene.IsValid()) {
            EditorSceneManager.OpenScene(originalScene.path, OpenSceneMode.Single);
        }
    }

    /// <summary>
    /// Scans the provided scene for ItemInteractable components and writes positions into the matching room in the manifest.
    /// Returns true if any matching room was updated.
    /// </summary>
    private static bool UpdateManifestFromScene(UnityEngine.SceneManagement.Scene scene, GameManifest manifest) {
        bool anyUpdated = false;
        string sceneName = scene.name;
        // Find matching room in manifest - exact match on sceneName
        var room = manifest.rooms.FirstOrDefault(r => r.sceneName == sceneName);
        if (room == null) {
            // try a fallback: try matching by sceneName containing keywords
            room = manifest.rooms.FirstOrDefault(r => !string.IsNullOrEmpty(r.sceneName) && sceneName.Contains(r.sceneName));
            if (room == null) return false;
        }

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots) {
            var items = root.GetComponentsInChildren<ItemInteractable>(true);
            foreach (var it in items) {
                if (string.IsNullOrEmpty(it.itemId)) continue;
                if (room.items == null) continue;
                var md = room.items.FirstOrDefault(x => x.id == it.itemId);
                if (md != null) {
                    if (md.position == null) md.position = new Vector2Data();
                    Vector3 pos = it.transform.position;
                    md.position.x = pos.x;
                    md.position.y = pos.y;
                    anyUpdated = true;
                    Debug.Log($"Updated manifest pos: room='{room.id}' item='{it.itemId}' pos=({pos.x:0.###},{pos.y:0.###})");
                } else {
                    Debug.LogWarning($"Scene '{sceneName}' contains ItemInteractable with id '{it.itemId}' which is not present in manifest room '{room.id}'.");
                }
            }
        }
        return anyUpdated;
    }

    // Optional: Auto export on play (exports current scene)
    [InitializeOnLoadMethod]
    private static void InitializeAutoExportHook() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {
        // When entering PlayMode, run export if enabled
        if (state == PlayModeStateChange.ExitingEditMode) {
            bool autoExport = EditorPrefs.GetBool(autoExportPrefKey, false);
            if (autoExport) {
                Debug.Log("ManifestPositionExporter: Auto-export on Play enabled. Exporting current scene positions before Play.");
                ExportPositionsFromScenes(true);
            }
        }
    }
}
#endif