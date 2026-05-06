using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────
//  PhoneMessageUI — Apartment 17B
//  Handles the interactive smartphone UI.
//  Press [TAB] to toggle phone. Unlocks cursor.
// ─────────────────────────────────────────────

[System.Serializable]
public struct MessageData
{
    public string body;
    public bool isOutgoing;
}

public class PhoneMessageUI : MonoBehaviour
{
    public static PhoneMessageUI Instance;

    [Header("UI References")]
    public GameObject phoneWindow;         // The main phone UI window
    
    [Header("Chat Panel")]
    public GameObject chatPanel;           // ChatScrollView
    public Transform messageContainer;     // Content of the ChatScrollView
    public ScrollRect chatScrollRect;      // The ScrollRect component for chat
    public TextMeshProUGUI headerText;     // "Messages > Unknown Number"
    
    [Header("Contacts Panel")]
    public GameObject contactsPanel;       // ContactsScrollView
    public Transform contactsContainer;    // Content of the ContactsScrollView
    public Button backButton;              // Back button in the header

    [Header("Dynamic UI")]
    public TextMeshProUGUI timeText;       // The clock in the header
    public TextMeshProUGUI inputBarText;   // The text at the bottom

    [Header("Prefabs")]
    public GameObject incomingBubblePrefab; // Grey left-aligned bubble
    public GameObject outgoingBubblePrefab; // Blue right-aligned bubble

    [Header("Audio")]
    public AudioSource notificationAudio;   // The ping sound

    [Header("Blur Effect")]
    public UnityEngine.Rendering.Volume blurVolume;

    // State
    private bool _isOpen = false;
    private string _pendingReply = "";
    private string _currentActiveChat = ""; // empty means Contacts Menu is active

    // Data
    private Dictionary<string, List<MessageData>> chatHistories = new Dictionary<string, List<MessageData>>();
    private Dictionary<string, GameObject> contactButtons = new Dictionary<string, GameObject>();
    private List<string> contactOrder = new List<string>();
    private int selectedContactIndex = 0;

    private StarterAssets.FirstPersonController _playerController;
    private StarterAssets.StarterAssetsInputs _playerInputs;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (phoneWindow == null)
        {
            var pWindow = GameObject.Find("PhoneWindow");
            if (pWindow != null)
            {
                phoneWindow = pWindow;
            }
        }

        if (phoneWindow != null)
        {
            if (chatPanel == null) chatPanel = phoneWindow.transform.Find("ChatScrollView")?.gameObject;
            if (contactsPanel == null) contactsPanel = phoneWindow.transform.Find("ContactsScrollView")?.gameObject;
            
            if (chatPanel != null) {
                if (messageContainer == null) messageContainer = chatPanel.transform.Find("Viewport/Content");
                if (chatScrollRect == null) chatScrollRect = chatPanel.GetComponent<ScrollRect>();
            }
            if (contactsPanel != null) {
                if (contactsContainer == null) contactsContainer = contactsPanel.transform.Find("Viewport/Content");
            }

            if (headerText == null) headerText = phoneWindow.transform.Find("Header/HeaderText")?.GetComponent<TextMeshProUGUI>();
            if (timeText == null) timeText = phoneWindow.transform.Find("Header/TimeText")?.GetComponent<TextMeshProUGUI>();
            if (inputBarText == null) inputBarText = phoneWindow.transform.Find("InputBar/Text")?.GetComponent<TextMeshProUGUI>();
            if (backButton == null) backButton = phoneWindow.transform.Find("Header/BackButton")?.GetComponent<Button>();
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(CloseChat);
        }

        if (phoneWindow != null)
            phoneWindow.SetActive(false);
    }

    void Start()
    {
        if (inputBarText != null) inputBarText.text = "Message...";

        // Set initial state
        CloseChat();

        // Find player to pause movement
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            _playerController = player.GetComponent<StarterAssets.FirstPersonController>();
            _playerInputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        }

        if (TimeManager.Instance == null)
        {
            Debug.LogError("[PhoneMessageUI] TimeManager not found!");
            return;
        }

        // Subscribe to phase events
        TimeManager.Instance.OnSleepReady    += OnSleepReady;
        TimeManager.Instance.OnHorrorBegins  += OnHorrorBegins;
        TimeManager.Instance.OnFinalMoment   += OnFinalMoment;

        // Show Landlord welcome message ~10 seconds after scene start
        StartCoroutine(DelayedMessage(10f, "Landlord", 
            "Keys are on the kitchen counter.\nLet me know if you face any issues.\nPrevious tenant was… a bit strange.\nAnyway, welcome.", false));
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnSleepReady   -= OnSleepReady;
            TimeManager.Instance.OnHorrorBegins -= OnHorrorBegins;
            TimeManager.Instance.OnFinalMoment  -= OnFinalMoment;
        }
    }

    void Update()
    {
        // Toggle phone with TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePhone();
        }

        // Sync Time
        if (timeText != null && TimeManager.Instance != null)
        {
            timeText.text = TimeManager.Instance.GetFormattedTime();
        }

        if (_isOpen)
        {
            if (string.IsNullOrEmpty(_currentActiveChat))
            {
                // Contacts Menu Navigation
                if (contactOrder.Count > 0)
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                    {
                        selectedContactIndex--;
                        if (selectedContactIndex < 0) selectedContactIndex = contactOrder.Count - 1;
                        RefreshContactHighlight();
                    }
                    else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                    {
                        selectedContactIndex++;
                        if (selectedContactIndex >= contactOrder.Count) selectedContactIndex = 0;
                        RefreshContactHighlight();
                    }
                    else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        OpenChat(contactOrder[selectedContactIndex]);
                    }
                }
            }
            else
            {
                // Active Chat Navigation
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseChat();
                }
                else if (!string.IsNullOrEmpty(_pendingReply) && Input.GetKeyDown(KeyCode.R))
                {
                    // Send reply
                    ShowMessage("Me", _pendingReply, true);
                    _pendingReply = "";
                    
                    // Reset input bar
                    if (inputBarText != null)
                        inputBarText.text = "Message...";
                }
            }
        }
    }

    // ── Public API ────────────────────────────

    public void TogglePhone()
    {
        _isOpen = !_isOpen;
        if (phoneWindow != null) phoneWindow.SetActive(_isOpen);

        // Pause/Unpause player
        if (_playerController != null)
            _playerController.enabled = !_isOpen;

        // Handle cursor
        if (_playerInputs != null)
        {
            _playerInputs.cursorInputForLook = !_isOpen;
            _playerInputs.cursorLocked = !_isOpen;
            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isOpen;
        }

        // Toggle blur
        if (blurVolume != null)
        {
            blurVolume.weight = _isOpen ? 1f : 0f;
        }

        if (_isOpen)
        {
            if (string.IsNullOrEmpty(_currentActiveChat))
            {
                RefreshContactHighlight();
            }
            else
            {
                StartCoroutine(ScrollToBottom());
            }
        }
    }

    public void OpenChat(string sender)
    {
        _currentActiveChat = sender;
        if (contactsPanel != null) contactsPanel.SetActive(false);
        if (chatPanel != null) chatPanel.SetActive(true);
        if (backButton != null) backButton.gameObject.SetActive(true);
        
        if (headerText != null) headerText.text = "Messages > " + sender;

        // Clear existing bubbles
        if (messageContainer != null)
        {
            // Gather children first because we are changing their parent
            List<Transform> childrenToDestroy = new List<Transform>();
            foreach(Transform child in messageContainer) {
                childrenToDestroy.Add(child);
            }
            
            foreach(Transform child in childrenToDestroy) {
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            // Re-instantiate history
            if (chatHistories.ContainsKey(sender)) {
                foreach(var msg in chatHistories[sender]) {
                    InstantiateBubble(msg.body, msg.isOutgoing);
                }
            }
        }

        // Remove unread indicator from the contact button
        UpdateContactPreview(sender, null, false);
    }

    public void CloseChat()
    {
        _currentActiveChat = "";
        if (contactsPanel != null) contactsPanel.SetActive(true);
        if (chatPanel != null) chatPanel.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        
        if (headerText != null) headerText.text = "Contacts";
        if (inputBarText != null) inputBarText.text = "Message..."; // clear reply prompt if leaving chat
    }

    public void ShowMessage(string sender, string body, bool isOutgoing = false)
    {
        // Route outgoing messages to the current active chat
        if (isOutgoing && sender == "Me") {
            if (!string.IsNullOrEmpty(_currentActiveChat)) sender = _currentActiveChat;
            else return; // Nowhere to send
        }

        if (!chatHistories.ContainsKey(sender))
        {
            chatHistories[sender] = new List<MessageData>();
            CreateContactButton(sender);
        }

        chatHistories[sender].Add(new MessageData { body = body, isOutgoing = isOutgoing });
        
        bool isUnread = !isOutgoing && _currentActiveChat != sender;
        UpdateContactPreview(sender, body, isUnread);

        if (_currentActiveChat == sender)
        {
            InstantiateBubble(body, isOutgoing);
        }
        else
        {
            if (notificationAudio != null && !isOutgoing)
                notificationAudio.Play();
        }
    }

    private void InstantiateBubble(string body, bool isOutgoing)
    {
        if (messageContainer == null || incomingBubblePrefab == null || outgoingBubblePrefab == null) return;

        GameObject prefab = isOutgoing ? outgoingBubblePrefab : incomingBubblePrefab;
        GameObject bubble = Instantiate(prefab, messageContainer);
        var textComponent = bubble.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null) textComponent.text = body;

        if (_isOpen)
        {
            if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = StartCoroutine(ScrollToBottom());
        }
    }

    private void CreateContactButton(string sender)
    {
        if (contactsContainer == null) return;

        // Build button procedurally
        var btnGO = new GameObject("Contact_" + sender, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        btnGO.transform.SetParent(contactsContainer, false);
        
        var rect = btnGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70); // Auto width, fixed height

        var img = btnGO.GetComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f, 1f); // slightly darker than bg

        var vlg = btnGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 10, 10);
        vlg.childControlHeight = false; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
        vlg.spacing = 5;

        var btn = btnGO.GetComponent<Button>();
        btn.onClick.AddListener(() => OpenChat(sender));

        // Top Row (Name + Unread Dot)
        var topRow = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        topRow.transform.SetParent(btnGO.transform, false);
        topRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
        var hlg = topRow.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlHeight = true; hlg.childControlWidth = false;
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;

        var nameText = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameText.transform.SetParent(topRow.transform, false);
        var nt = nameText.GetComponent<TextMeshProUGUI>();
        nt.text = sender;
        nt.fontSize = 20;
        nt.fontStyle = FontStyles.Bold;
        nt.color = Color.black;
        nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 0); // fixed width for name

        var unreadDot = new GameObject("UnreadDot", typeof(RectTransform), typeof(Image));
        unreadDot.transform.SetParent(topRow.transform, false);
        unreadDot.GetComponent<RectTransform>().sizeDelta = new Vector2(12, 12);
        var dotImg = unreadDot.GetComponent<Image>();
        dotImg.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        dotImg.color = Color.red;
        unreadDot.SetActive(false); // hide by default

        // Bottom Row (Preview text)
        var previewText = new GameObject("PreviewText", typeof(RectTransform), typeof(TextMeshProUGUI));
        previewText.transform.SetParent(btnGO.transform, false);
        previewText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
        var pt = previewText.GetComponent<TextMeshProUGUI>();
        pt.text = "Tap to chat";
        pt.fontSize = 14;
        pt.color = Color.gray;
        pt.overflowMode = TextOverflowModes.Ellipsis;

        contactButtons[sender] = btnGO;
        contactOrder.Add(sender);
        
        if (contactOrder.Count == 1)
        {
            selectedContactIndex = 0;
            RefreshContactHighlight();
        }
    }

    private void RefreshContactHighlight()
    {
        for (int i = 0; i < contactOrder.Count; i++)
        {
            string s = contactOrder[i];
            if (contactButtons.TryGetValue(s, out GameObject btnGO))
            {
                var img = btnGO.GetComponent<Image>();
                if (img != null)
                {
                    // Highlight selected contact with a light blue color
                    if (i == selectedContactIndex)
                        img.color = new Color(0.8f, 0.9f, 1f, 1f);
                    else
                        img.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                }
            }
        }
    }

    private void UpdateContactPreview(string sender, string body, bool isUnread = false)
    {
        if (contactButtons.TryGetValue(sender, out GameObject btnGO))
        {
            if (body != null)
            {
                var pt = btnGO.transform.Find("PreviewText")?.GetComponent<TextMeshProUGUI>();
                if (pt != null) pt.text = body;
            }
            
            var dot = btnGO.transform.Find("TopRow/UnreadDot")?.gameObject;
            if (dot != null) dot.SetActive(isUnread);
        }
    }

    // ── Phase Event Handlers ──────────────────

    void OnSleepReady() { }

    void OnHorrorBegins()
    {
        // 3:17 AM — first unknown message
        StartCoroutine(DelayedMessage(2f, "Unknown Number", "You hear it too, don't you?", false, "Hear what? Who is this?"));
    }

    void OnFinalMoment()
    {
        // 3:30 AM equivalent
        StartCoroutine(DelayedMessage(1.5f, "Unknown Number", "You're late.", false));
    }

    IEnumerator DelayedMessage(float delay, string sender, string body, bool isOutgoing, string reply = "")
    {
        yield return new WaitForSeconds(delay);
        ShowMessage(sender, body, isOutgoing);

        if (!string.IsNullOrEmpty(reply))
        {
            // Wait a second before showing the reply prompt
            yield return new WaitForSeconds(1f);
            QueueReply(sender, reply);
        }
    }

    public void QueueReply(string sender, string replyText)
    {
        // Only show reply UI if we are in this sender's chat
        if (_currentActiveChat == sender)
        {
            _pendingReply = replyText;
            if (inputBarText != null)
            {
                inputBarText.text = "Message...  [R] Reply";
            }
        }
    }

    // ── Utility ──────────────────────

    private Coroutine _scrollCoroutine;

    IEnumerator ScrollToBottom()
    {
        // Wait for next frame so TextMeshPro updates its meshes
        yield return null;

        if (messageContainer != null)
        {
            // Force the layout to rebuild now that text has size
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer.GetComponent<RectTransform>());
        }

        yield return new WaitForEndOfFrame();

        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
            
        _scrollCoroutine = null;
    }
}
