using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueLoader : MonoBehaviour {
    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel; // optional panel to show/hide
    [Header("Audio")]
    public AudioSource audioSource;

    public IEnumerator PlayDialogueCoroutine(string dialogueId) {
        var manifest = GameManager.I.manifest;
        var dd = manifest.dialogues.Find(d => d.id == dialogueId);
        if (dd == null) yield break;
        
        // Only show panel if it's assigned
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(true);
        }

        foreach (var line in dd.lines) {
            // play VO if present
            if (!string.IsNullOrEmpty(line.audioClip) && audioSource != null) {
                var clip = Resources.Load<AudioClip>(line.audioClip);
                if (clip != null) audioSource.PlayOneShot(clip);
            }

            // typewriter
            yield return TypeLineCoroutine(line.text);
            // wait for click or small delay; for testing we auto-advance after 1.0s or wait for click
            // wait for click or auto-advance
            float timer = 0f;
            bool advanced = false;
            while (!advanced) {
                timer += Time.deltaTime;
                if (InputManager.Instance != null && InputManager.Instance.WasClickThisFrame()) {
                    advanced = true;
                } else if (timer > 1.0f) {
                    advanced = true;
                }
                yield return null;
            }
        }

        // Only hide panel if it's assigned
        if (dialoguePanel != null) {
            dialoguePanel.SetActive(false);
        }
    }

    IEnumerator TypeLineCoroutine(string fullText) {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        for (int i = 0; i < fullText.Length; i++) {
            dialogueText.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(0.02f); // typing speed
        }
    }
}