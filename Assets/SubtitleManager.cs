using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance;

    public TextMeshProUGUI subtitleText;
    public float fadeSpeed = 0.5f;

    private Coroutine currentSubtitle;

    void Awake()
    {
        Instance = this;
        subtitleText.alpha = 0f;
    }

    public void Show(string text, float duration)
    {
        if (currentSubtitle != null)
            StopCoroutine(currentSubtitle);
        currentSubtitle = StartCoroutine(ShowRoutine(text, duration));
    }

    public void ShowWhisper(string text, float duration)
    {
        Show($"<i><color=#AAAAAA>{text}</color></i>", duration);
    }

    IEnumerator ShowRoutine(string text, float duration)
    {
        subtitleText.text = text;

        // Fade in
        float t = 0;
        while (t < fadeSpeed)
        {
            t += Time.deltaTime;
            subtitleText.alpha = Mathf.Lerp(0f, 1f, t / fadeSpeed);
            yield return null;
        }

        subtitleText.alpha = 1f;
        yield return new WaitForSeconds(duration);

        // Fade out
        t = 0;
        while (t < fadeSpeed)
        {
            t += Time.deltaTime;
            subtitleText.alpha = Mathf.Lerp(1f, 0f, t / fadeSpeed);
            yield return null;
        }

        subtitleText.text = "";
        subtitleText.alpha = 0f;
    }
}