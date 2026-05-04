using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────
//  TimeManager — Apartment 17B
//  Drives the full one-night game loop.
//  Attach this to an empty GameObject called
//  "GameManager" in your scene.
// ─────────────────────────────────────────────

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    // ── Game Phases ──────────────────────────
    public enum GamePhase
    {
        Exploration,   // 6:30 PM  — normal
        SleepReady,    // 11:30 PM — bed becomes interactable
        Sleeping,      // transitional black screen
        HorrorBegins,  // 3:17 AM  — horror activates
        FinalMoment,   // 3:30 AM  — ending sequence
    }

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Exploration;

    // ── Inspector ─────────────────────────────
    [Header("Game Clock (in-game minutes per real second)")]
    public float timeScale = 60f;          // 1 real second = 1 in-game minute

    [Header("Phase Trigger Times (in-game 24h)")]
    public float sleepReadyHour = 23.5f; // 11:30 PM
    public float wakeUpHour = 3.2f;  // 3:12 AM
    public float horrorStartHour = 3.283f;// 3:17 AM
    public float finalMomentHour = 3.5f;  // 3:30 AM

    [Header("Scene References")]
    public GameObject bed;                 // assign your Bed GameObject
    public CanvasGroup blackScreen;        // full-screen black CanvasGroup
    public GameObject player;             // your FPS player

    [Header("Sleep Transition")]
    public float sleepFadeDuration = 2f;
    public float sleepHoldDuration = 3f;  // seconds of black screen

    // ── Internal ──────────────────────────────
    private float _currentHour = 18.5f;   // start: 6:30 PM
    private bool _phaseTriggered_Sleep = false;
    private bool _phaseTriggered_Horror = false;
    private bool _phaseTriggered_Final = false;

    // ── Events — subscribe from other scripts ─
    // e.g. HorrorManager.cs:  TimeManager.Instance.OnHorrorBegins += ActivateHorror;
    public event System.Action OnSleepReady;
    public event System.Action OnWakeUp;
    public event System.Action OnHorrorBegins;
    public event System.Action OnFinalMoment;

    // ─────────────────────────────────────────
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Start the clock
        StartCoroutine(GameClock());
    }

    // ─────────────────────────────────────────
    //  MAIN CLOCK LOOP
    // ─────────────────────────────────────────
    IEnumerator GameClock()
    {
        while (true)
        {
            // Always advance — SleepSequence handles the clock jump internally.
            // CheckPhaseTransitions must run during Sleeping so the horror
            // condition (CurrentPhase == Sleeping && hour >= 3:17) can fire.
            _currentHour += (Time.deltaTime / 60f) * timeScale;

            // Wrap past midnight:  24.0 → 0.0
            if (_currentHour >= 24f)
                _currentHour -= 24f;

            CheckPhaseTransitions();

            yield return null;
        }
    }

    // ─────────────────────────────────────────
    //  PHASE CHECKS
    // ─────────────────────────────────────────
    void CheckPhaseTransitions()
    {
        // 11:30 PM — bed becomes interactable
        if (!_phaseTriggered_Sleep && _currentHour >= sleepReadyHour)
        {
            _phaseTriggered_Sleep = true;
            TriggerSleepReady();
        }

        // 3:17 AM — horror begins  (only after sleep)
        if (!_phaseTriggered_Horror
            && CurrentPhase == GamePhase.Sleeping
            && _currentHour >= horrorStartHour)
        {
            _phaseTriggered_Horror = true;
            TriggerWakeUp();
        }

        // 3:30 AM — final moment
        // Guard: horror MUST have started first — prevents broken triggers
        // if sleep system fails or is skipped during testing.
        if (!_phaseTriggered_Final
            && CurrentPhase == GamePhase.HorrorBegins
            && _currentHour >= finalMomentHour)
        {
            _phaseTriggered_Final = true;
            TriggerFinalMoment();
        }
    }

    // ─────────────────────────────────────────
    //  PHASE TRIGGERS
    // ─────────────────────────────────────────

    // Called at 11:30 PM
    void TriggerSleepReady()
    {
        CurrentPhase = GamePhase.SleepReady;

        // Show subtitle hint
        SubtitleManager.Instance?.ShowQueue("I should get some sleep.");

        // Make bed interactable — guard against missing component
        if (bed != null)
        {
            var bedScript = bed.GetComponent<BedInteractable>();
            if (bedScript != null)
                bedScript.Enable();
            else
                Debug.LogWarning("[TimeManager] BedInteractable component not found on Bed!");
        }

        OnSleepReady?.Invoke();

        Debug.Log("[TimeManager] Phase → SleepReady (11:30 PM)");
    }

    // Called by BedInteractable when player lies down
    public void TriggerSleep()
    {
        if (CurrentPhase != GamePhase.SleepReady) return;

        CurrentPhase = GamePhase.Sleeping;
        StartCoroutine(SleepSequence());

        Debug.Log("[TimeManager] Phase → Sleeping");
    }

    // Sleep fade out → jump time → fade in at 3:12 AM
    IEnumerator SleepSequence()
    {
        // TODO: swap to disabling FirstPersonController + StarterAssetsInputs
        // components only — SetActive(false) on the whole GameObject can
        // break the AudioListener and Cinemachine camera target.
        var controller = player.GetComponent<StarterAssets.FirstPersonController>();
        if (controller != null)
            controller.enabled = false;

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f, sleepFadeDuration));

        // Hold black + play audio (optional: clock ticking, breathing)
        yield return new WaitForSeconds(sleepHoldDuration);

        // Jump clock to 3:12 AM
        _currentHour = wakeUpHour;

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f, sleepFadeDuration));

        // TODO: match the above — re-enable components, not the whole GameObject.
        if (controller != null)
            controller.enabled = true;

        // Phase stays as Sleeping — clock resumes from 3:12 AM.
        // At 3:17, CheckPhaseTransitions fires TriggerWakeUp(),
        // which sets HorrorBegins and fires OnWakeUp + OnHorrorBegins together.
        SubtitleManager.Instance?.Show("...", 2f);

        Debug.Log("[TimeManager] Sleep sequence done — clock resuming at 3:12 AM");
    }

    // ─────────────────────────────────────────
    //  WAKE-UP + HORROR  (triggered by clock at 3:17 AM)
    // ─────────────────────────────────────────
    void TriggerWakeUp()
    {
        // Guard: if something fires this twice (reload, editor bug), ignore.
        if (CurrentPhase == GamePhase.HorrorBegins) return;

        // Direct transition: Sleeping → HorrorBegins
        CurrentPhase = GamePhase.HorrorBegins;

        // OnWakeUp fires first (ambient shift, player can move).
        // OnHorrorBegins fires immediately after (HorrorManager activates).
        // Both fire together at 3:17 — horror starts the moment eyes open.
        OnWakeUp?.Invoke();
        OnHorrorBegins?.Invoke();

        Debug.Log("[TimeManager] Phase → HorrorBegins (3:17 AM)");
    }

    // Called at 3:30 AM
    void TriggerFinalMoment()
    {
        CurrentPhase = GamePhase.FinalMoment;
        OnFinalMoment?.Invoke();

        Debug.Log("[TimeManager] Phase → FinalMoment (3:30 AM)");
    }

    // ─────────────────────────────────────────
    //  UTILITY
    // ─────────────────────────────────────────

    // Returns formatted clock string e.g. "3:17 AM"
    public string GetFormattedTime()
    {
        int hours = Mathf.FloorToInt(_currentHour) % 24;
        int minutes = Mathf.FloorToInt((_currentHour % 1f) * 60f);

        string suffix = hours >= 12 ? "PM" : "AM";

        int display = hours % 12;
        if (display == 0) display = 12;

        return $"{display}:{minutes:00} {suffix}";
    }

    // Other scripts can read the current phase
    public bool IsPhase(GamePhase phase) => CurrentPhase == phase;

    // ─────────────────────────────────────────
    //  FADE HELPER  (reuses blackScreen canvas)
    // ─────────────────────────────────────────
    IEnumerator Fade(float from, float to, float duration)
    {
        if (blackScreen == null) yield break;

        float t = 0f;
        blackScreen.alpha = from;

        while (t < duration)
        {
            t += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        blackScreen.alpha = to;
    }
}