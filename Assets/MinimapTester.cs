using UnityEngine;
using UnityEngine.UI;

public class MinimapTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MinimapTeleporter teleporter;
    
    [Header("Test Settings")]
    [SerializeField] private Vector3 testPosition = new Vector3(0, 0, 5);
    [SerializeField] private bool showDebugInfo = true;
    
    // Simple UI for debugging
    private Text infoText;
    private Canvas debugCanvas;
    
    private void Start()
    {
        // Try to find teleporter if not assigned
        if (teleporter == null)
        {
            teleporter = GetComponent<MinimapTeleporter>();
        }
        
        if (showDebugInfo)
        {
            CreateDebugUI();
        }
    }
    
    private void CreateDebugUI()
    {
        // Create a simple world canvas
        GameObject canvasObj = new GameObject("MinimapTestCanvas");
        canvasObj.transform.SetParent(transform);
        debugCanvas = canvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.WorldSpace;
        
        // Add canvas scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Position the canvas
        canvasObj.transform.localPosition = new Vector3(0, 0.2f, 0);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        // Create text element
        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(canvasObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        
        // Setup text component
        infoText = textObj.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 14;
        infoText.color = Color.white;
        infoText.text = "Minimap Test Ready";
        infoText.alignment = TextAnchor.MiddleCenter;
        
        // Set text area size
        RectTransform rt = infoText.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 100);
        rt.localPosition = Vector3.zero;
    }
    
    private void Update()
    {
        if (infoText != null && showDebugInfo)
        {
            // Show basic state info
            string info = "Minimap Test\n";
            info += "-----------\n";
            
            if (teleporter != null)
            {
                info += "Teleporter: Connected\n";
                info += "Right-click -> Test Teleport\n";
            }
            else
            {
                info += "Teleporter: Not Found\n";
            }
            
            infoText.text = info;
        }
    }
    
    [ContextMenu("Test Teleport")]
    public void TestTeleport()
    {
        if (teleporter == null)
        {
            Debug.LogError("No teleporter assigned!");
            return;
        }
        
        Debug.Log("Testing teleport to: " + testPosition);
        teleporter.TestTeleport(testPosition);
    }
} 