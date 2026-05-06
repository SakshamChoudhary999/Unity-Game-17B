using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// MainMenu — Apartment 17B
// Buttons are wired in Awake() — no Inspector OnClick setup required.

public class MainMenu : MonoBehaviour
{
    [Header("Optional — leave blank to auto-find")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public CanvasGroup     fadeOverlay;

    private bool _transitioning = false;

    // ── Lifecycle ────────────────────────────────────────

    void Awake()
    {
        // Auto-find if not assigned in Inspector
        if (titleText   == null) { var go = GameObject.Find("TitleText");   if (go) titleText   = go.GetComponent<TextMeshProUGUI>(); }
        if (subtitleText== null) { var go = GameObject.Find("SubtitleText");if (go) subtitleText = go.GetComponent<TextMeshProUGUI>(); }
        if (fadeOverlay == null) { var go = GameObject.Find("FadeOverlay"); if (go) fadeOverlay  = go.GetComponent<CanvasGroup>(); }

        // ── Wire buttons by name (no Inspector click needed) ──
        WireButton("EnterButton", EnterApartment);
        WireButton("QuitButton",  LeaveGame);
    }

    void Start()
    {
        // Fade in from black on scene load
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = false;   // NEVER block raycasts — handled by alpha only
            fadeOverlay.interactable   = false;
            StartCoroutine(FadeIn(1.8f));
        }

        // Title flicker
        if (titleText != null)
            StartCoroutine(TitleFlicker());
    }

    // ── Button Wiring ─────────────────────────────────────

    void WireButton(string goName, UnityEngine.Events.UnityAction callback)
    {
        var go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning("[MainMenu] Button not found: " + goName); return; }
        var btn = go.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning("[MainMenu] No Button on: " + goName); return; }
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(callback);
        Debug.Log("[MainMenu] Wired " + goName + " -> " + callback.Method.Name);
    }

    // ── Button Handlers ───────────────────────────────────

    public void EnterApartment()
    {
        if (_transitioning) return;
        Debug.Log("[MainMenu] EnterApartment pressed");
        _transitioning = true;
        StartCoroutine(LoadScene("Scene_01"));
    }

    public void LeaveGame()
    {
        if (_transitioning) return;
        Debug.Log("[MainMenu] LeaveGame pressed");
        _transitioning = true;
        StartCoroutine(QuitSequence());
    }

    // ── Scene Transition ──────────────────────────────────

    IEnumerator LoadScene(string sceneName)
    {
        if (fadeOverlay != null)
            yield return StartCoroutine(FadeOut(1.0f));

        SceneManager.LoadScene(sceneName);
    }

    IEnumerator QuitSequence()
    {
        if (fadeOverlay != null)
            yield return StartCoroutine(FadeOut(0.6f));

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Fade Helpers ──────────────────────────────────────

    IEnumerator FadeIn(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (fadeOverlay != null)
                fadeOverlay.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.alpha = 0f;
    }

    IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (fadeOverlay != null)
                fadeOverlay.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        if (fadeOverlay != null) fadeOverlay.alpha = 1f;
    }

    // ── Title Flicker ─────────────────────────────────────

    IEnumerator TitleFlicker()
    {
        while (true)
        {
            if (!_transitioning && Random.value < 0.012f)
            {
                titleText.alpha = 0f;
                yield return new WaitForSeconds(0.08f);
                titleText.alpha = 1f;

                if (Random.value < 0.4f)
                {
                    yield return new WaitForSeconds(0.04f);
                    titleText.alpha = 0.3f;
                    yield return new WaitForSeconds(0.1f);
                    titleText.alpha = 1f;
                }
            }
            yield return null;
        }
    }
}