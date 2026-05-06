using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ─────────────────────────────────────────────
//  NoteReadable — Apartment 17B
//  A generic "read this note" interactable.
//  Attach to any prop (notebook page, mirror note,
//  compartment contents, etc.).
//
//  When player presses [E] → full-screen note overlay
//  appears with the note content.
//  Press [E] again or wait holdDuration to dismiss.
// ─────────────────────────────────────────────

public class NoteReadable : MonoBehaviour, IInteractable
{
    [Header("Note Content")]
    [TextArea(3, 10)]
    public string noteText = "...";

    [TextArea(1, 3)]
    public string noteTitle = "";  // Optional header/title

    [Header("UI References")]
    public CanvasGroup noteOverlay;        // Full-screen overlay CanvasGroup
    public TextMeshProUGUI noteTitleTMP;
    public TextMeshProUGUI noteBodyTMP;

    [Header("Settings")]
    public float fadeDuration = 0.5f;
    public float holdDuration = 8f;  // Auto-dismiss after this long (0 = wait for E)
    public string interactPrompt = "[E] Read";

    [Header("Restrict to Phase")]
    public bool onlyDuringHorror = false;  // If true, only interactable after 3:17 AM

    private bool _isReading = false;
    private static NoteReadable _activeNote = null;

    // ── IInteractable ─────────────────────────

    public string GetPrompt()
    {
        if (onlyDuringHorror && TimeManager.Instance != null
            && !TimeManager.Instance.IsPhase(TimeManager.GamePhase.HorrorBegins)
            && !TimeManager.Instance.IsPhase(TimeManager.GamePhase.FinalMoment))
            return "";  // Not yet available

        return _isReading ? "" : interactPrompt;
    }

    public void Interact()
    {
        if (_isReading) return;
        if (_activeNote != null) return;  // Another note is open

        if (onlyDuringHorror && TimeManager.Instance != null
            && !TimeManager.Instance.IsPhase(TimeManager.GamePhase.HorrorBegins)
            && !TimeManager.Instance.IsPhase(TimeManager.GamePhase.FinalMoment))
            return;

        StartCoroutine(ReadNote());
    }

    // ── Read Sequence ─────────────────────────

    IEnumerator ReadNote()
    {
        _isReading = true;
        _activeNote = this;

        // Disable player look/movement
        DisablePlayer(true);

        // Populate text
        if (noteTitleTMP != null) noteTitleTMP.text = noteTitle;
        if (noteBodyTMP  != null) noteBodyTMP.text  = noteText;

        // Fade overlay IN
        if (noteOverlay != null)
        {
            noteOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeOverlay(0f, 1f));
        }

        // Wait for E press or auto-dismiss
        float elapsed = 0f;
        bool dismissed = false;
        while (!dismissed)
        {
            elapsed += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.E)) dismissed = true;
            if (holdDuration > 0 && elapsed >= holdDuration) dismissed = true;
            yield return null;
        }

        // Fade overlay OUT
        if (noteOverlay != null)
        {
            yield return StartCoroutine(FadeOverlay(1f, 0f));
            noteOverlay.gameObject.SetActive(false);
        }

        // Re-enable player
        DisablePlayer(false);

        _isReading = false;
        _activeNote = null;
    }

    IEnumerator FadeOverlay(float from, float to)
    {
        if (noteOverlay == null) yield break;
        float t = 0f;
        noteOverlay.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            noteOverlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        noteOverlay.alpha = to;
    }

    void DisablePlayer(bool disable)
    {
        var controller = FindFirstObjectByType<StarterAssets.FirstPersonController>();
        if (controller != null) controller.enabled = !disable;

        // Lock/unlock cursor
        Cursor.lockState = disable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = disable;
    }
}
