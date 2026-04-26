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

        // Disable player at start
        player.SetActive(false);

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
        player.SetActive(true);

        // Disable this system
        gameObject.SetActive(false);
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