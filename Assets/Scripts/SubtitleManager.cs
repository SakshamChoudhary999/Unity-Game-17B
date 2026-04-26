using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance;

    [Header("UI")]
    public TextMeshProUGUI subtitleText;

    [Header("Settings")]
    public float fadeSpeed = 0.5f;

    [Header("Typewriter")]
    public float typeSpeed = 0.04f;

    private Coroutine currentSubtitle;
    private Queue<string> subtitleQueue = new Queue<string>();

    private bool isPlaying = false;
    private bool isTyping = false;
    private bool skipLine = false;

    public bool IsPlaying => isPlaying; // ✅ ADD THIS
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (subtitleText == null)
        {
            Debug.LogError("SubtitleManager: SubtitleText is NOT assigned!");
            return;
        }

        subtitleText.alpha = 0f;
    }

//void start  tha phele yaha pe

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
            skipLine = true;
    }

    public void Show(string text, float duration = 3f)
    {
        if (currentSubtitle != null)
            StopCoroutine(currentSubtitle);

        subtitleQueue.Clear();
        subtitleText.alpha = 0f;
        subtitleText.text = "";

        currentSubtitle = StartCoroutine(ShowRoutine(text, duration));
    }

    public void ShowQueue(params string[] lines)
    {
        subtitleQueue.Clear();
        foreach (var line in lines)
            subtitleQueue.Enqueue(line);

        if (!isPlaying)
            currentSubtitle = StartCoroutine(PlayQueue());
    }

    public void ShowWhisper(string text, float duration = 3f)
    {
        Show($"<i><color=#AAAAAA>{text}</color></i>", duration);
    }

    IEnumerator PlayQueue()
    {
        isPlaying = true;
        while (subtitleQueue.Count > 0)
        {
            string text = subtitleQueue.Dequeue();
            yield return ShowQueueRoutine(text);
        }
        isPlaying = false;
    }

    IEnumerator ShowQueueRoutine(string text)
    {
        skipLine = false;
        subtitleText.text = "";
        subtitleText.alpha = 1f;

        // Phase 1 — Typewriter
        isTyping = true;
        foreach (char c in text)
        {
            if (skipLine)
            {
                // X pressed → show full text instantly
                subtitleText.text = text;
                skipLine = false; // reset so next X goes to next line
                break;
            }
            subtitleText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;

        // Make sure full text is always shown
        subtitleText.text = text;

        // Phase 2 — Wait ONLY for X (no auto advance)
        while (!skipLine)
            yield return null;

        // Fade out
        float t = 0f;
        while (t < fadeSpeed)
        {
            t += Time.deltaTime;
            subtitleText.alpha = Mathf.Lerp(1f, 0f, t / fadeSpeed);
            yield return null;
        }

        subtitleText.text = "";
        subtitleText.alpha = 0f;
        skipLine = false;

        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator ShowRoutine(string text, float duration)
    {
        skipLine = false;
        subtitleText.alpha = 1f;
        subtitleText.text = "";

        isTyping = true;
        foreach (char c in text)
        {
            if (skipLine)
            {
                subtitleText.text = text;
                skipLine = false;
                break;
            }
            subtitleText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;

        subtitleText.text = text;
        yield return new WaitForSeconds(duration);

        float t = 0f;
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