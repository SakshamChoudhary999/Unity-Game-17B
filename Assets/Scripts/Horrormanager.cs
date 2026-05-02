using UnityEngine;
using System.Collections;
using StarterAssets;

public class HorrorManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource whisperAudio;
    public AudioSource ambientAudio;
    public AudioSource wallBreathing;
    public AudioSource wallKnockAudio;

    [Header("Scene Objects")]
    public Light mainLight;
    public GameObject wallCompartment;

    [Header("Timing (seconds, measured from horror start)")]
    public float lightDimDelay = 4f;
    public float ambientShiftDelay = 6f;
    public float wallBreathDelay = 9f;

    private FirstPersonController _controller;

    void Start()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogError("[HorrorManager] TimeManager not found! " +
                           "Make sure it exists in the scene and runs before HorrorManager.");
            return;
        }

        TimeManager.Instance.OnHorrorBegins += StartHorror;

        _controller = Object.FindFirstObjectByType<FirstPersonController>();

        if (_controller == null)
            Debug.LogError("[HorrorManager] FirstPersonController not found!");
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnHorrorBegins -= StartHorror;
    }

    void StartHorror()
    {
        Debug.Log("[HorrorManager] Horror started — beginning staggered sequence.");
        StartCoroutine(HorrorSequence());
    }

    IEnumerator HorrorSequence()
    {
        // 3:17 AM — Beat 1: Whisper (instant)
        PlayWhisper("...don't...");

        // Beat 2: Light dims
        yield return new WaitForSeconds(lightDimDelay);
        StartCoroutine(SmoothLight(0.3f, 2f));

        // Beat 3: Ambient shift
        yield return new WaitForSeconds(Mathf.Max(0f, ambientShiftDelay - lightDimDelay));
        StartCoroutine(SmoothPitch(0.7f));

        // Beat 4: Wall breathing
        yield return new WaitForSeconds(Mathf.Max(0f, wallBreathDelay - ambientShiftDelay));
        StartWallBreathing();

        // 3:22 AM — 5 in-game minutes = 5 real seconds (timeScale=60)
        yield return new WaitForSeconds(5f);
        StartCoroutine(Trigger322());

        // 3:25 AM — 3 more in-game minutes = 3 real seconds
        yield return new WaitForSeconds(3f);
        Trigger325();
    }

    // ─────────────────────────────────────────
    //  3:22 AM
    // ─────────────────────────────────────────

    IEnumerator Trigger322()
    {
        yield return StartCoroutine(RandomKnocks());

        SubtitleManager.Instance?.Show("Did that come from inside the wall?", 3f);

        yield return new WaitForSeconds(2f);

        if (wallCompartment != null)
            wallCompartment.SetActive(true);

        SubtitleManager.Instance?.Show("Something opened...", 2f);
    }

    IEnumerator RandomKnocks()
    {
        int count = Random.Range(2, 5);

        for (int i = 0; i < count; i++)
        {
            if (wallKnockAudio != null)
                wallKnockAudio.Play();
            else
                Debug.LogWarning("[HorrorManager] wallKnockAudio not assigned.");

            yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
        }
    }

    // ─────────────────────────────────────────
    //  3:25 AM
    // ─────────────────────────────────────────

    void Trigger325()
    {
        SubtitleManager.Instance?.Show("Something's wrong...", 3f);
        StartCoroutine(ControlGlitch());
    }

    IEnumerator ControlGlitch()
    {
        if (_controller == null)
        {
            Debug.LogWarning("[HorrorManager] Control glitch skipped — controller missing.");
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        _controller.enabled = false;

        if (whisperAudio != null && !whisperAudio.isPlaying)
            whisperAudio.Play();

        yield return new WaitForSeconds(1.5f);

        _controller.enabled = true;
    }

    // ─────────────────────────────────────────
    //  EFFECT METHODS
    // ─────────────────────────────────────────

    void PlayWhisper(string text)
    {
        if (whisperAudio != null)
            whisperAudio.Play();
        else
            Debug.LogWarning("[HorrorManager] whisperAudio not assigned.");

        SubtitleManager.Instance?.ShowWhisper(text);
    }

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