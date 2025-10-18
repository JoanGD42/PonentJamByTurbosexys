using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Vector2Data { public float x; public float y; public Vector2 ToVector2() => new Vector2(x, y); }

[Serializable]
public class ItemData {
    public string id;
    public string displayName;
    public Vector2Data position;
    public string dialogueId;
    public string cinematicId;
}

[Serializable]
public class RoomData {
    public string id;
    public string sceneName;
    public List<ItemData> items;
}

[Serializable]
public class DialogueLine {
    public string text;
    public string audioClip; // Resource path or null
}

[Serializable]
public class DialogueData {
    public string id;
    public List<DialogueLine> lines;
}

[Serializable]
public class CinematicData {
    public string id;
    public string type; // e.g. "sprite_frames"
    public List<string> framePaths; // Resource paths
    public int fps;
    public float duration;
}

[Serializable]
public class GameManifest {
    public List<RoomData> rooms;
    public List<DialogueData> dialogues;
    public List<CinematicData> cinematics;
}

public static class GameManifestLoader {
    // Loads Resources/game_manifest.json
    public static GameManifest LoadFromResources(string resourcePath = "game_manifest") {
        var ta = Resources.Load<TextAsset>(resourcePath);
        if (ta == null) {
            Debug.LogError($"GameManifest not found at Resources/{resourcePath}.json");
            return null;
        }
        return JsonUtility.FromJson<GameManifest>(ta.text);
    }
}