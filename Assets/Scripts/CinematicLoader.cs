using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinematicLoader : MonoBehaviour {
    // Plays a cinematic by id (uses GameManager.I.manifest)
    public IEnumerator PlayCinematic(string cinematicId) {
        var manifest = GameManager.I.manifest;
        var cd = manifest.cinematics.Find(c => c.id == cinematicId);
        if (cd == null) yield break;

        if (cd.type == "sprite_frames") {
            // Create a full-screen temporary sprite object (or centered above scene)
            var go = new GameObject("Cinematic_" + cinematicId);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "UI"; // ensure it's on top; adjust according to your project
            sr.sortingOrder = 1000;

            // load frames and play at cd.fps
            List<Sprite> frames = new List<Sprite>();
            foreach (var path in cd.framePaths) {
                var s = Resources.Load<Sprite>(path);
                if (s != null) frames.Add(s);
            }

            if (frames.Count == 0) {
                Debug.LogWarning("Cinematic frames not found for " + cinematicId);
                Destroy(go);
                yield break;
            }

            float interval = cd.fps > 0 ? 1f / cd.fps : 1f / 6f;
            float timer = 0f;
            int idx = 0;
            float total = 0f;
            while (total < cd.duration) {
                sr.sprite = frames[idx % frames.Count];
                yield return new WaitForSeconds(interval);
                idx++;
                total += interval;
            }

            Destroy(go);
        } else {
            Debug.LogWarning("Unknown cinematic type: " + cd.type);
            yield break;
        }
    }
}