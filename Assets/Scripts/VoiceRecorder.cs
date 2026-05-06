using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────
//  VoiceRecorder — Apartment 17B
//  A damaged voice recorder in the kitchen drawer.
//
//  BEFORE 3:22 AM:  plays normal/static audio
//  AFTER  3:22 AM:  plays horror playback with
//                   a different whisper subtitle
//
//  Attach to your recorder prop.
//  Assign normalClip, horrorClip, and audioSource.
// ─────────────────────────────────────────────

public class VoiceRecorder : MonoBehaviour, IInteractable
{
    [Header("Audio Clips")]
    [Tooltip("Audio heard before 3:22 AM — static, hiss, or ambient")]
    public AudioClip normalClip;

    [Tooltip("Audio heard after 3:22 AM — plays 'I'm still inside'")]
    public AudioClip horrorClip;

    [Header("References")]
    public AudioSource audioSource;

    [Header("Settings")]
    public string promptBeforeHorror = "[E] Play Recording";
    public string promptAfterHorror  = "[E] Play Recording";

    // Internal: 3:22 AM is ~5 real seconds after 3:17 AM (at timeScale=60)
    // We track via HorrorManager phase progression instead.
    private bool _horrorPlayed = false;
    private bool _isPlaying = false;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogError("[VoiceRecorder] No AudioSource found! Assign one in Inspector.");
    }

    // ── IInteractable ─────────────────────────

    public string GetPrompt()
    {
        if (_isPlaying) return "";  // Already playing

        bool isHorrorPhase = TimeManager.Instance != null &&
            (TimeManager.Instance.IsPhase(TimeManager.GamePhase.HorrorBegins) ||
             TimeManager.Instance.IsPhase(TimeManager.GamePhase.FinalMoment));

        return isHorrorPhase ? promptAfterHorror : promptBeforeHorror;
    }

    public void Interact()
    {
        if (_isPlaying) return;

        bool isHorrorPhase = TimeManager.Instance != null &&
            (TimeManager.Instance.IsPhase(TimeManager.GamePhase.HorrorBegins) ||
             TimeManager.Instance.IsPhase(TimeManager.GamePhase.FinalMoment));

        if (isHorrorPhase && !_horrorPlayed)
            StartCoroutine(PlayHorrorRecording());
        else
            StartCoroutine(PlayNormalRecording());
    }

    // ── Playback Routines ─────────────────────

    IEnumerator PlayNormalRecording()
    {
        _isPlaying = true;

        SubtitleManager.Instance?.Show("*static* …nothing but noise.", 3f);

        if (audioSource != null && normalClip != null)
        {
            audioSource.clip = normalClip;
            audioSource.Play();
            yield return new WaitForSeconds(normalClip.length);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        _isPlaying = false;
    }

    IEnumerator PlayHorrorRecording()
    {
        _isPlaying   = true;
        _horrorPlayed = true;

        // Brief silence — as if rewinding
        SubtitleManager.Instance?.Show("*click*", 1.5f);
        yield return new WaitForSeconds(2f);

        if (audioSource != null && horrorClip != null)
        {
            audioSource.clip = horrorClip;
            audioSource.Play();
        }

        // Show whisper subtitle of what's "on the recording"
        yield return new WaitForSeconds(1f);
        SubtitleManager.Instance?.ShowWhisper("\"I'm still inside.\"", 4f);

        if (horrorClip != null)
            yield return new WaitForSeconds(Mathf.Max(0f, horrorClip.length - 1f));
        else
            yield return new WaitForSeconds(4f);

        SubtitleManager.Instance?.Show("…that wasn't there before.", 3f);

        _isPlaying = false;
    }
}
