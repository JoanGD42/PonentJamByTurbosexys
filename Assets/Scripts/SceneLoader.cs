using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    // Load a single scene additively
    public IEnumerator LoadSceneAdditiveRoutine(string sceneName) {
        if (SceneManager.GetSceneByName(sceneName).isLoaded) {
            yield break;
        }
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;
    }

    // Load multiple scenes additively (in sequence)
    public IEnumerator LoadScenesAdditiveRoutine(IEnumerable<string> sceneNames) {
        foreach (var name in sceneNames) {
            yield return LoadSceneAdditiveRoutine(name);
        }
    }

    // Enable or disable all root objects in a named scene
    public void SetSceneActiveObjects(string sceneName, bool active) {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return;
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots) {
            // Avoid toggling persistent systems if they are in the same scene - typically they won't be.
            r.SetActive(active);
        }
    }
}