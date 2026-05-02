using UnityEngine;
using System.Collections;
using StarterAssets; // Required for FirstPersonController

public class FinalSequence : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup blackScreen;
    public GameObject player;
    public AudioSource whisperAudio;

    [Header("Settings")]
    public float fadeDuration = 3f;

    private bool triggered = false;

    void Start()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogError("[FinalSequence] TimeManager not found!");
            return;
        }

        TimeManager.Instance.OnFinalMoment += StartFinalSequence;
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnFinalMoment -= StartFinalSequence;
    }

    void StartFinalSequence()
    {
        if (triggered) return;
        triggered = true;

        StartCoroutine(FinalRoutine());
    }

    IEnumerator FinalRoutine()
    {
        // 1. Disable player movement
        if (player != null)
        {
            var controller = player.GetComponent<FirstPersonController>();
            if (controller != null)
                controller.enabled = false;
            else
                Debug.LogWarning("[FinalSequence] FirstPersonController not found on player!");
        }

        // Unlock cursor for cinematic ending
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Play whisper + subtitle
        if (whisperAudio != null)
            whisperAudio.Play();

        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowWhisper("It's your turn.", 3f);
        else
            Debug.LogWarning("[FinalSequence] SubtitleManager missing!");

        // 3. Wait before fade
        yield return new WaitForSeconds(2f);

        // 4. Fade to black
        if (blackScreen != null)
        {
            float t = 0f;
            float startAlpha = blackScreen.alpha;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                blackScreen.alpha = Mathf.Lerp(startAlpha, 1f, t / fadeDuration);
                yield return null;
            }

            blackScreen.alpha = 1f;
        }
        else
        {
            Debug.LogWarning("[FinalSequence] BlackScreen not assigned!");
        }

        // 5. Silence moment
        yield return new WaitForSeconds(1f);

        // 6. Final message
        if (SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.Show(
                "Apartment 17B is now available for rent.",
                6f
            );
        }
    }
}