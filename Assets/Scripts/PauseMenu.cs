using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("UI References (Auto-generated if blank)")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button leaveButton;

    private bool _isPaused = false;
    private StarterAssets.FirstPersonController _playerController;
    private StarterAssets.StarterAssetsInputs _playerInputs;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Find Player
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerController = player.GetComponent<StarterAssets.FirstPersonController>();
            _playerInputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        }

        // Auto-generate UI if not assigned
        if (pausePanel == null)
        {
            GenerateUI();
        }

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (leaveButton != null) leaveButton.onClick.AddListener(Leave);
        
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        // Don't allow pausing if Phone is open
        bool phoneOpen = PhoneMessageUI.Instance != null && PhoneMessageUI.Instance.phoneWindow != null && PhoneMessageUI.Instance.phoneWindow.activeSelf;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) 
            {
                Resume();
            }
            else if (!phoneOpen)
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        if (_playerController != null) _playerController.enabled = false;
        if (_playerInputs != null)
        {
            _playerInputs.cursorInputForLook = false;
            _playerInputs.cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        if (_playerController != null) _playerController.enabled = true;
        
        // Return cursor control
        if (_playerInputs != null)
        {
            _playerInputs.cursorInputForLook = true;
            _playerInputs.cursorLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void Leave()
    {
        Time.timeScale = 1f; // Critical: restore time scale before leaving!
        SceneManager.LoadScene("Main Menu"); // Match your Main Menu scene name
    }

    private void GenerateUI()
    {
        // Create Canvas
        var canvasGO = new GameObject("PauseMenuCanvas");
        canvasGO.transform.SetParent(this.transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Panel
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvasGO.transform, false);
        var bgRect = pausePanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var bgImg = pausePanel.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.9f); // Dark horror background

        // Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(pausePanel.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = Vector2.zero;
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 80;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.8f, 0, 0, 1); // Deep red
        titleText.fontStyle = FontStyles.Bold;

        // Resume Button
        var resumeBtnGO = new GameObject("ResumeButton");
        resumeBtnGO.transform.SetParent(pausePanel.transform, false);
        var resRect = resumeBtnGO.AddComponent<RectTransform>();
        resRect.anchorMin = new Vector2(0.5f, 0.5f);
        resRect.anchorMax = new Vector2(0.5f, 0.5f);
        resRect.sizeDelta = new Vector2(400, 70);
        resRect.anchoredPosition = new Vector2(0, 0);
        var resImg = resumeBtnGO.AddComponent<Image>();
        resImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        resumeButton = resumeBtnGO.AddComponent<Button>();

        var resTextGO = new GameObject("Text");
        resTextGO.transform.SetParent(resumeBtnGO.transform, false);
        var resTextRect = resTextGO.AddComponent<RectTransform>();
        resTextRect.anchorMin = Vector2.zero; resTextRect.anchorMax = Vector2.one;
        resTextRect.offsetMin = Vector2.zero; resTextRect.offsetMax = Vector2.zero;
        var resText = resTextGO.AddComponent<TextMeshProUGUI>();
        resText.text = "Resume Nightmare";
        resText.fontSize = 30;
        resText.alignment = TextAlignmentOptions.Center;
        resText.color = Color.white;

        // Leave Button
        var leaveBtnGO = new GameObject("LeaveButton");
        leaveBtnGO.transform.SetParent(pausePanel.transform, false);
        var leaveRect = leaveBtnGO.AddComponent<RectTransform>();
        leaveRect.anchorMin = new Vector2(0.5f, 0.4f);
        leaveRect.anchorMax = new Vector2(0.5f, 0.4f);
        leaveRect.sizeDelta = new Vector2(400, 70);
        leaveRect.anchoredPosition = new Vector2(0, -90);
        var leaveImg = leaveBtnGO.AddComponent<Image>();
        leaveImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        leaveButton = leaveBtnGO.AddComponent<Button>();

        var leaveTextGO = new GameObject("Text");
        leaveTextGO.transform.SetParent(leaveBtnGO.transform, false);
        var leaveTextRect = leaveTextGO.AddComponent<RectTransform>();
        leaveTextRect.anchorMin = Vector2.zero; leaveTextRect.anchorMax = Vector2.one;
        leaveTextRect.offsetMin = Vector2.zero; leaveTextRect.offsetMax = Vector2.zero;
        var leaveText = leaveTextGO.AddComponent<TextMeshProUGUI>();
        leaveText.text = "Leave";
        leaveText.fontSize = 30;
        leaveText.alignment = TextAlignmentOptions.Center;
        leaveText.color = Color.white;
    }
}
