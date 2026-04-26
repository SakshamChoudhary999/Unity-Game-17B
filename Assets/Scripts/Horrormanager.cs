using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────
//  HorrorManager — Apartment 17B
//  Subscribes to TimeManager.OnHorrorBegins.
//  Staggers every horror effect across time
//  so 3:17 AM builds rather than dumps.
//  Attach to an empty GameObject: "HorrorManager"
// ─────────────────────────────────────────────

public class HorrorManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource whisperAudio;     // one-shot whisper clip
    public AudioSource ambientAudio;     // looping room tone — pitch will shift
    public AudioSource wallBreathing;    // looping wall breathing — initially stopped

    [Header("Scene Objects")]
    public Light mainLight;              // main room light

    [Header("Timing (seconds, measured from horror start)")]
    public float lightDimDelay = 4f;  // whisper is instant at 0
    public float ambientShiftDelay = 6f;  // must be >= lightDimDelay
    public float wallBreathDelay = 9f;  // must be >= ambientShiftDelay

    // ─────────────────────────────────────────
    void Start()
    {
        // Null guard — TimeManager must exist and have run Awake first.
        // If this fires, check Script Execution Order in Project Settings.
        if (TimeManager.Instance == null)
        {
            Debug.LogError("[HorrorManager] TimeManager not found! " +
                           "Make sure it exists in the scene and runs before HorrorManager.");
            return;
        }

        TimeManager.Instance.OnHorrorBegins += StartHorror;
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnHorrorBegins -= StartHorror;
    }

    // ─────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────
    void StartHorror()
    {
        Debug.Log("[HorrorManager] Horror started — beginning staggered sequence.");
        StartCoroutine(HorrorSequence());
    }

    // ─────────────────────────────────────────
    //  STAGGERED HORROR SEQUENCE
    //  Each beat lands in silence after the last.
    //  The gaps are where the dread lives.
    // ─────────────────────────────────────────
    IEnumerator HorrorSequence()
    {
        // Beat 1 — Whisper hits the moment eyes open. No delay.
        PlayWhisper("...don't...");

        // Beat 2 — Light dims slowly (player notices something changed)
        yield return new WaitForSeconds(lightDimDelay);
        StartCoroutine(SmoothLight(0.3f, 2f));

        // Beat 3 — Room tone drops (subconscious unease)
        // Mathf.Max: safe if Inspector values are set out of order
        yield return new WaitForSeconds(Mathf.Max(0f, ambientShiftDelay - lightDimDelay));
        StartCoroutine(SmoothPitch(0.7f));

        // Beat 4 — Wall breathing starts (explicit, physical threat)
        yield return new WaitForSeconds(Mathf.Max(0f, wallBreathDelay - ambientShiftDelay));
        StartWallBreathing();
    }

    // ─────────────────────────────────────────
    //  EFFECT METHODS
    // ─────────────────────────────────────────

    void PlayWhisper(string text)
    {
        // Audio
        if (whisperAudio != null)
            whisperAudio.Play();
        else
            Debug.LogWarning("[HorrorManager] whisperAudio not assigned.");

        // Subtitle (italic grey — defined in SubtitleManager.ShowWhisper)
        SubtitleManager.Instance?.ShowWhisper(text);
    }

    // Smooth light dim — feels like the room is breathing out.
    // Runs as its own coroutine so the main sequence keeps moving.
    IEnumerator SmoothLight(float targetIntensity, float duration = 2f)
    {
        if (mainLight == null)
        {
            Debug.LogWarning("[HorrorManager] mainLight not assigned.");
            yield break;
        }

        float start = mainLight.intensity;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            mainLight.intensity = Mathf.Lerp(start, targetIntensity, t / duration);
            yield return null;
        }

        mainLight.intensity = targetIntensity;
    }

    IEnumerator SmoothPitch(float targetPitch, float duration = 2f)
    {
        if (ambientAudio == null)
        {
            Debug.LogWarning("[HorrorManager] ambientAudio not assigned.");
            yield break;
        }

        float start = ambientAudio.pitch;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            ambientAudio.pitch = Mathf.Lerp(start, targetPitch, t / duration);
            yield return null;
        }

        ambientAudio.pitch = targetPitch;
    }

    void StartWallBreathing()
    {
        // wallBreathing is an AudioSource — calling Play() directly
        // so it works regardless of the "Play On Awake" Inspector setting.
        if (wallBreathing != null)
        {
            if (!wallBreathing.isPlaying)
            {
                wallBreathing.loop = true;
                wallBreathing.Play();
            }
        }
        else
            Debug.LogWarning("[HorrorManager] wallBreathing AudioSource not assigned.");
    }
}