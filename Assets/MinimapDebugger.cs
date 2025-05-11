using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MinimapTeleporter teleporter;
    
    [Header("UI Settings")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private int maxLogLines = 10;
    [SerializeField] private Color logTextColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color teleportColor = Color.green;
    
    [Header("Testing")]
    [SerializeField] private Vector3 testTeleportLocation = new Vector3(0, 0, 5);
    
    // UI elements
    private Canvas debugCanvas;
    private Text debugLogText;
    private Text statusText;
    
    // Log history
    private Queue<string> logEntries = new Queue<string>();
    
    private void OnEnable()
    {
        // Subscribe to teleport events
        MinimapTeleporter.OnTeleportEvent += OnTeleportEvent;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from teleport events
        MinimapTeleporter.OnTeleportEvent -= OnTeleportEvent;
    }
    
    private void Start()
    {
        FindTeleporter();
        
        if (showDebugUI)
        {
            CreateDebugUI();
        }
        
        AddLogEntry("MinimapDebugger started");
    }
    
    private void FindTeleporter()
    {
        // Try to find teleporter if not assigned
        if (teleporter == null)
        {
            teleporter = GetComponent<MinimapTeleporter>();
            
            if (teleporter == null)
            {
                teleporter = FindObjectOfType<MinimapTeleporter>();
                
                if (teleporter == null)
                {
                    AddLogEntry("No MinimapTeleporter found in scene!", LogType.Error);
                }
                else
                {
                    AddLogEntry("Found MinimapTeleporter in scene");
                }
            }
            else
            {
                AddLogEntry("Using MinimapTeleporter on this object");
            }
        }
        else
        {
            AddLogEntry("MinimapTeleporter reference set in inspector");
        }
    }
    
    private void CreateDebugUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("DebugCanvas");
        canvasObj.transform.SetParent(transform);
        debugCanvas = canvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.WorldSpace;
        
        // Add canvas components
        canvasObj.AddComponent<CanvasScaler>();
        
        // Position the canvas in front of the object
        canvasObj.transform.localPosition = new Vector3(0, 0.3f, 0.3f);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
        
        // Create background panel
        GameObject panelObj = new GameObject("DebugPanel");
        panelObj.transform.SetParent(canvasObj.transform);
        panelObj.transform.localPosition = Vector3.zero;
        panelObj.transform.localScale = Vector3.one;
        
        // Add panel image
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.sizeDelta = new Vector2(500, 400);
        
        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.cyan;
        titleText.text = "MINIMAP TELEPORTER DEBUG";
        titleText.alignment = TextAnchor.UpperCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.sizeDelta = new Vector2(480, 30);
        titleRect.anchoredPosition = new Vector2(0, 170);
        
        // Create status text
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panelObj.transform);
        statusText = statusObj.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 18;
        statusText.color = Color.white;
        statusText.text = "Status: Initializing...";
        statusText.alignment = TextAnchor.UpperLeft;
        RectTransform statusRect = statusText.rectTransform;
        statusRect.sizeDelta = new Vector2(480, 60);
        statusRect.anchoredPosition = new Vector2(0, 130);
        
        // Create log text area
        GameObject logObj = new GameObject("LogText");
        logObj.transform.SetParent(panelObj.transform);
        debugLogText = logObj.AddComponent<Text>();
        debugLogText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        debugLogText.fontSize = 14;
        debugLogText.color = logTextColor;
        debugLogText.text = "Log entries will appear here...";
        debugLogText.alignment = TextAnchor.UpperLeft;
        RectTransform logRect = debugLogText.rectTransform;
        logRect.sizeDelta = new Vector2(480, 240);
        logRect.anchoredPosition = new Vector2(0, -20);
        
        // Create test button
        GameObject buttonObj = new GameObject("TestButton");
        buttonObj.transform.SetParent(panelObj.transform);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(150, 40);
        buttonRect.anchoredPosition = new Vector2(-80, -170);
        
        // Add button components
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.8f);
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // Add button text
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform);
        Text buttonText = buttonTextObj.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 16;
        buttonText.text = "Test Teleport";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        RectTransform buttonTextRect = buttonText.rectTransform;
        buttonTextRect.sizeDelta = new Vector2(140, 30);
        buttonTextRect.anchoredPosition = Vector2.zero;
        
        // Setup button click event
        button.onClick.AddListener(() => {
            if (teleporter != null)
            {
                AddLogEntry("Manual test teleport triggered", LogType.Teleport);
                teleporter.TestTeleport(testTeleportLocation);
            }
            else
            {
                AddLogEntry("Cannot test - teleporter not found", LogType.Error);
            }
        });
        
        // Create reset button
        GameObject resetButtonObj = new GameObject("ResetButton");
        resetButtonObj.transform.SetParent(panelObj.transform);
        RectTransform resetButtonRect = resetButtonObj.AddComponent<RectTransform>();
        resetButtonRect.sizeDelta = new Vector2(150, 40);
        resetButtonRect.anchoredPosition = new Vector2(80, -170);
        
        // Add reset button components
        Image resetButtonImage = resetButtonObj.AddComponent<Image>();
        resetButtonImage.color = new Color(0.8f, 0.2f, 0.2f);
        Button resetButton = resetButtonObj.AddComponent<Button>();
        resetButton.targetGraphic = resetButtonImage;
        
        // Add reset button text
        GameObject resetButtonTextObj = new GameObject("ResetButtonText");
        resetButtonTextObj.transform.SetParent(resetButtonObj.transform);
        Text resetButtonText = resetButtonTextObj.AddComponent<Text>();
        resetButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resetButtonText.fontSize = 16;
        resetButtonText.text = "Reset Marker";
        resetButtonText.alignment = TextAnchor.MiddleCenter;
        resetButtonText.color = Color.white;
        RectTransform resetButtonTextRect = resetButtonText.rectTransform;
        resetButtonTextRect.sizeDelta = new Vector2(140, 30);
        resetButtonTextRect.anchoredPosition = Vector2.zero;
        
        // Setup reset button click event
        resetButton.onClick.AddListener(() => {
            if (teleporter != null)
            {
                AddLogEntry("Resetting marker position", LogType.Warning);
                teleporter.ResetMarkerPosition();
            }
            else
            {
                AddLogEntry("Cannot reset - teleporter not found", LogType.Error);
            }
        });
    }
    
    private void Update()
    {
        if (statusText != null && teleporter != null)
        {
            UpdateStatusText();
        }
    }
    
    private void UpdateStatusText()
    {
        string status = "STATUS:\n";
        status += $"Marker Grabbed: {(teleporter.IsMarkerGrabbed() ? "YES" : "NO")}\n";
        
        // Get private field values through reflection for more debug info
        var teleportRequestedField = typeof(MinimapTeleporter).GetField("teleportRequested", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        var cachedPositionField = typeof(MinimapTeleporter).GetField("cachedTeleportPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (teleportRequestedField != null && cachedPositionField != null)
        {
            bool teleportRequested = (bool)teleportRequestedField.GetValue(teleporter);
            Vector3 position = (Vector3)cachedPositionField.GetValue(teleporter);
            
            status += $"Teleport Requested: {(teleportRequested ? "YES" : "NO")}\n";
            status += $"Target: {position.x:F1}, {position.y:F1}, {position.z:F1}";
        }
        
        statusText.text = status;
    }
    
    public void OnTeleportEvent(string message, Vector3 position)
    {
        // Determine log type based on message content
        LogType logType = LogType.Normal;
        
        if (message.Contains("TELEPORTING"))
            logType = LogType.Teleport;
        else if (message.Contains("CANCELED") || message.Contains("WARNING"))
            logType = LogType.Warning;
        else if (message.Contains("ERROR"))
            logType = LogType.Error;
            
        AddLogEntry(message, logType);
    }
    
    private void AddLogEntry(string entry, LogType logType = LogType.Normal)
    {
        // Format with time
        string formattedEntry = $"[{Time.frameCount}] {entry}";
        
        // Add to queue
        logEntries.Enqueue(formattedEntry);
        
        // Keep queue at max size
        while (logEntries.Count > maxLogLines)
        {
            logEntries.Dequeue();
        }
        
        // Update UI if available
        UpdateLogText(logType);
        
        // Also log to console
        switch (logType)
        {
            case LogType.Error:
                Debug.LogError($"[MinimapDebugger] {entry}");
                break;
            case LogType.Warning:
                Debug.LogWarning($"[MinimapDebugger] {entry}");
                break;
            default:
                Debug.Log($"[MinimapDebugger] {entry}");
                break;
        }
    }
    
    private void UpdateLogText(LogType latestLogType)
    {
        if (debugLogText == null) return;
        
        // Reset text color
        debugLogText.color = logTextColor;
        
        // Set color based on latest log type
        switch (latestLogType)
        {
            case LogType.Error:
                debugLogText.color = errorColor;
                break;
            case LogType.Warning:
                debugLogText.color = warningColor;
                break;
            case LogType.Teleport:
                debugLogText.color = teleportColor;
                break;
        }
        
        // Build text from queue
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (string entry in logEntries)
        {
            sb.AppendLine(entry);
        }
        
        // Update text
        debugLogText.text = sb.ToString();
    }
    
    // Log types for coloring
    private enum LogType
    {
        Normal,
        Warning,
        Error,
        Teleport
    }
    
    [ContextMenu("Test Teleport")]
    public void TestTeleportFromMenu()
    {
        if (teleporter != null)
        {
            AddLogEntry("Context menu test teleport triggered", LogType.Teleport);
            teleporter.TestTeleport(testTeleportLocation);
        }
        else
        {
            AddLogEntry("Cannot test - teleporter not found", LogType.Error);
        }
    }
    
    [ContextMenu("Reset Marker Position")]
    public void ResetMarkerPosition()
    {
        if (teleporter != null)
        {
            AddLogEntry("Resetting marker position", LogType.Warning);
            teleporter.ResetMarkerPosition();
        }
        else
        {
            AddLogEntry("Cannot reset - teleporter not found", LogType.Error);
        }
    }
} 