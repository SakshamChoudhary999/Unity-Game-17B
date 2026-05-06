using UnityEngine;
using TMPro;

// ─────────────────────────────────────────────
//  ClockHUD — Apartment 17B
//  Displays the in-game clock (e.g. "3:17 AM")
//  using TimeManager.GetFormattedTime().
//
//  Attach to the ClockText GameObject.
//  Assign clockText in Inspector.
// ─────────────────────────────────────────────

public class ClockHUD : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI clockText;

    [Header("Settings")]
    [Tooltip("Only show clock during horror phase (3:17 AM+)")]
    public bool showOnlyDuringHorror = false;

    [Tooltip("Color of clock text at night (horror phase)")]
    public Color nightColor = new Color(0.8f, 0.15f, 0.15f, 1f);   // dark red

    [Tooltip("Color of clock text during day (evening)")]
    public Color dayColor = new Color(1f, 0.9f, 0.7f, 0.85f);       // warm cream

    private bool _horrorStarted = false;

    void Start()
    {
        if (clockText == null)
        {
            Debug.LogError("[ClockHUD] clockText not assigned!");
            return;
        }

        // Subscribe to horror event to change color
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnHorrorBegins += OnHorrorBegins;

            // Hide until horror if configured
            if (showOnlyDuringHorror)
                clockText.alpha = 0f;
        }
        else
        {
            Debug.LogWarning("[ClockHUD] TimeManager not found — clock may not update.");
        }

        clockText.color = dayColor;
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnHorrorBegins -= OnHorrorBegins;
    }

    void OnHorrorBegins()
    {
        _horrorStarted = true;
        if (clockText != null)
        {
            clockText.color = nightColor;

            // If hidden before, fade in
            if (showOnlyDuringHorror)
                StartCoroutine(FadeIn());
        }
    }

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            clockText.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        clockText.alpha = 1f;
    }

    void Update()
    {
        if (clockText == null || TimeManager.Instance == null) return;

        // Don't update text during "Sleeping" black-screen phase
        if (TimeManager.Instance.IsPhase(TimeManager.GamePhase.Sleeping)) return;

        clockText.text = TimeManager.Instance.GetFormattedTime();
    }
}
