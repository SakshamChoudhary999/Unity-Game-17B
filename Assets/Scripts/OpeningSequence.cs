using UnityEngine;
using System.Collections;

public class OpeningSequence : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup blackScreen;
    public GameObject player;

    [Header("Opening Lines")]
    [TextArea]
    public string[] lines;

    [Header("Settings")]
    public float fadeDuration = 2f;

    void Start()
    {
        // Safety check
        if (SubtitleManager.Instance == null)
        {
            Debug.LogError("SubtitleManager is missing in scene!");
            return;
        }

        // Disable player controls at start
        var controller = player.GetComponent<StarterAssets.FirstPersonController>();
        if (controller != null) controller.enabled = false;

        var inputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.move = Vector2.zero;
            inputs.look = Vector2.zero;
            inputs.cursorLocked = false;
            inputs.cursorInputForLook = false;
        }

        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // Start fully black
        blackScreen.alpha = 1f;

        // Fade from black → visible
        yield return StartCoroutine(Fade(1f, 0f));

        // Play subtitles using your system
        SubtitleManager.Instance.ShowQueue(lines);

        // Wait until subtitles are finished
        yield return new WaitUntil(() => !SubtitleManager.Instance.IsPlaying);

        // Enable gameplay
        var controller = player.GetComponent<StarterAssets.FirstPersonController>();
        if (controller != null) controller.enabled = true;

        var inputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.cursorLocked = true;
            inputs.cursorInputForLook = true;
        }

        // Start the game clock NOW that the monologue is over
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartClock();
        }

        // Disable this script only (not the GameObject, because the child BlackScreen is needed later)
        this.enabled = false;
    }

    IEnumerator Fade(float start, float end)
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(start, end, t / fadeDuration);
            yield return null;
        }

        blackScreen.alpha = end;
    }
}