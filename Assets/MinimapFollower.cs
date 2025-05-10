using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using XRMultiplayer;

public class MinimapFollower : MonoBehaviour
{
    public enum FollowTarget
    {
        Camera,
        LeftController,
        RightController
    }

    [Header("Follow Settings")]
    [SerializeField] private FollowTarget targetToFollow = FollowTarget.Camera;
    [SerializeField] private Transform playerCamera; // The VR camera to follow
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, 0.3f); // Offset from the target
    [SerializeField] private float smoothSpeed = 5f; // How smoothly the minimap follows
    [SerializeField] private bool followRotationY = true; // Whether to follow the target's Y rotation
    [SerializeField] private bool showDebugLogs = true; // Toggle for debug messages

    [Header("Toggle Settings")]
    [SerializeField] private InputActionReference toggleAction; // Assign this in inspector
    [SerializeField] private UIComponentToggler uiToggler; // Reference to the UIComponentToggler
    [SerializeField] private bool startVisible = true; // Should the map be visible at start?
    [SerializeField] private Key keyboardToggleKey = Key.M;  // Changed to new Input System Key

    // Make these public to allow setting in inspector
    [Header("Controller Tags")]
    [SerializeField] private string leftControllerTag = "LeftController";
    [SerializeField] private string rightControllerTag = "RightController";

    private Transform leftController;
    private Transform rightController;
    private bool hasLoggedError = false; // Prevent spam of error messages
    private float retryInterval = 0.5f; // How often to retry finding controllers
    private float nextRetryTime = 0f;
    private InputAction keyboardToggleAction;  // New field for keyboard input
    private CanvasGroup canvasGroup;
    private bool isManuallyHidden = false; // Flag to track if user manually toggled the minimap off
    private Transform m_GazeTransform;
    private float m_MaxRenderingDistance = 10f;
    private Vector2 m_MinMaxFacingThreshold = new Vector2(0f, 90f);
    private Vector2 m_MinMaxThresholdDistance = new Vector2(0f, 10f);
    private float m_FacingThreshold = 0f;
    private bool m_InRange = false;

    private void Start()
    {
        // Get or add UIComponentToggler
        if (uiToggler == null)
        {
            uiToggler = GetComponent<UIComponentToggler>();
            if (uiToggler == null)
            {
                uiToggler = gameObject.AddComponent<UIComponentToggler>();
            }
        }

        // Get or add CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (showDebugLogs)
        {
            Debug.Log($"[MinimapFollower] UIComponentToggler reference: {(uiToggler != null ? "Found" : "Missing")}");
            Debug.Log($"[MinimapFollower] Toggle Action reference: {(toggleAction != null ? "Found" : "Missing")}");
            Debug.Log($"[MinimapFollower] CanvasGroup reference: {(canvasGroup != null ? "Found" : "Missing")}");
        }

        // Set initial visibility
        if (!startVisible)
        {
            SetMapVisibility(false);
            if (showDebugLogs)
                Debug.Log("[MinimapFollower] Set initial visibility to hidden");
        }
        else
        {
            SetMapVisibility(true);
            if (showDebugLogs)
                Debug.Log("[MinimapFollower] Set initial visibility to visible");
        }

        if (showDebugLogs)
        {
            // Log all available tags for debugging
            Debug.Log("[MinimapFollower] Searching for controllers...");
            Debug.Log($"[MinimapFollower] Looking for right controller with tag: '{rightControllerTag}'");
            
            // Find and log all objects with the controller tag
            GameObject[] rightControllers = GameObject.FindGameObjectsWithTag(rightControllerTag);
            Debug.Log($"[MinimapFollower] Found {rightControllers.Length} objects with tag '{rightControllerTag}'");
            
            // Log all objects named "Controller" for debugging
            GameObject[] allControllers = GameObject.FindObjectsOfType<GameObject>();
            Debug.Log("[MinimapFollower] Objects with 'Controller' in name:");
            foreach (GameObject obj in allControllers)
            {
                if (obj.name.Contains("Controller"))
                {
                    Debug.Log($"[MinimapFollower] - Name: {obj.name}, Tag: {obj.tag}");
                }
            }
        }

        TryFindControllers();

        // Setup input actions
        if (toggleAction != null)
        {
            toggleAction.action.Enable();
            toggleAction.action.performed += OnToggleAction;
            if (showDebugLogs)
                Debug.Log("[MinimapFollower] Input action enabled and callback registered");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[MinimapFollower] Toggle action is not assigned!");
        }

        // Setup keyboard toggle
        keyboardToggleAction = new InputAction(name: "KeyboardToggle", binding: $"<Keyboard>/{keyboardToggleKey}");
        keyboardToggleAction.performed += _ => ToggleMap();
        keyboardToggleAction.Enable();
        if (showDebugLogs)
            Debug.Log($"[MinimapFollower] Keyboard toggle set up with key: {keyboardToggleKey}");

        // Log initial target selection
        if (showDebugLogs)
        {
            Debug.Log($"[MinimapFollower] Initially following: {targetToFollow}");
        }
    }

    private void OnDestroy()
    {
        if (toggleAction != null)
        {
            toggleAction.action.performed -= OnToggleAction;
        }
        
        if (keyboardToggleAction != null)
        {
            keyboardToggleAction.Disable();
            keyboardToggleAction.Dispose();
        }
    }

    private void OnToggleAction(InputAction.CallbackContext context)
    {
        if (showDebugLogs)
            Debug.Log("[MinimapFollower] Toggle action performed!");
        ToggleMap();
        
        // Record that this was a manual toggle
        if (canvasGroup != null)
        {
            isManuallyHidden = canvasGroup.alpha == 0;
        }
    }

    private void TryFindControllers()
    {
        if (showDebugLogs)
        {
            Debug.Log("[MinimapFollower] Attempting to find controllers...");
        }

        // Only try to find controllers if we don't have them yet
        if (leftController == null)
        {
            GameObject leftObj = GameObject.FindGameObjectWithTag(leftControllerTag);
            if (leftObj != null)
            {
                leftController = leftObj.transform;
                if (showDebugLogs)
                {
                    Debug.Log($"[MinimapFollower] Found left controller: {leftObj.name}");
                }
            }
        }

        if (rightController == null)
        {
            GameObject rightObj = GameObject.FindGameObjectWithTag(rightControllerTag);
            if (rightObj != null)
            {
                rightController = rightObj.transform;
                if (showDebugLogs)
                {
                    Debug.Log($"[MinimapFollower] Found right controller: {rightObj.name}");
                }
            }
        }
    }

    private void LateUpdate()
    {
        // If manually hidden by user, don't update position until manually shown again
        if (isManuallyHidden) return;
        
        if (canvasGroup == null || canvasGroup.alpha == 0) return; // Skip movement updates if not visible

        // Periodically try to find controllers if they're missing
        if (Time.time >= nextRetryTime)
        {
            if ((targetToFollow == FollowTarget.LeftController && leftController == null) ||
                (targetToFollow == FollowTarget.RightController && rightController == null))
            {
                TryFindControllers();
                nextRetryTime = Time.time + retryInterval;
            }
        }

        Transform target = GetCurrentTarget();
        if (target == null)
        {
            if (!hasLoggedError && showDebugLogs)
            {
                Debug.LogError($"[MinimapFollower] Target {targetToFollow} is missing! Check if the object exists and has the correct tag.");
                hasLoggedError = true;
            }
            return;
        }
        
        hasLoggedError = false; // Reset error flag if target is found

        // Calculate the desired position (relative to target's view)
        Vector3 desiredPosition = target.position + 
                                target.right * offset.x +
                                Vector3.up * offset.y +
                                target.forward * offset.z;

        // Smoothly move to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        if (followRotationY)
        {
            // Only follow the Y rotation of the target
            Vector3 eulerAngles = target.eulerAngles;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, eulerAngles.y, transform.eulerAngles.z);
        }
    }

    private Transform GetCurrentTarget()
    {
        switch (targetToFollow)
        {
            case FollowTarget.LeftController:
                return leftController;
            case FollowTarget.RightController:
                return rightController;
            case FollowTarget.Camera:
            default:
                return playerCamera;
        }
    }

    // Public method to change target at runtime
    public void SetTarget(FollowTarget newTarget)
    {
        targetToFollow = newTarget;
        if (showDebugLogs)
        {
            Debug.Log($"[MinimapFollower] Target changed to: {newTarget}");
        }
        hasLoggedError = false; // Reset error flag to allow new error messages
    }

    // Public method to toggle map visibility
    public void ToggleMap()
    {
        if (canvasGroup == null)
        {
            if (showDebugLogs)
                Debug.LogError("[MinimapFollower] Cannot toggle map - CanvasGroup is missing!");
            return;
        }

        bool isCurrentlyVisible = canvasGroup.alpha > 0;
        if (showDebugLogs)
            Debug.Log($"[MinimapFollower] Current visibility state (before toggle): {isCurrentlyVisible}, Alpha: {canvasGroup.alpha}");

        // Toggle visibility
        SetMapVisibility(!isCurrentlyVisible);
        
        // Update manual toggle state
        isManuallyHidden = !isCurrentlyVisible;
        
        if (showDebugLogs)
        {
            Debug.Log($"[MinimapFollower] Map visibility toggled to: {!isCurrentlyVisible}");
            Debug.Log($"[MinimapFollower] New alpha value: {canvasGroup.alpha}");
        }
    }

    private void SetMapVisibility(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1 : 0;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            
            // Find and toggle the marker visibility as well
            Transform markerTransform = transform.GetComponentInChildren<MinimapTeleporter>()?.markerTransform;
            if (markerTransform != null)
            {
                markerTransform.gameObject.SetActive(visible);
            }
        }
    }
} 