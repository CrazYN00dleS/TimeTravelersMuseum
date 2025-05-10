using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class MinimapTeleporter : MonoBehaviour
{
    [Header("Minimap References")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] public Transform markerTransform; // Made public so it can be accessed by MinimapFollower
    [SerializeField] private Camera minimapCamera;
    
    [Header("Teleport References")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private GameObject teleportMarkerVisual;
    
    [Header("Settings")]
    [SerializeField] private float teleportHeight = 0.1f; // Slight offset from ground
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private LayerMask groundLayer = 1; // Default layer
    [SerializeField] private float teleportDelay = 0.2f; // Short delay before teleport
    [SerializeField] private Color markerColor = new Color(0, 0.8f, 1, 0.5f);
    [SerializeField] private bool onlyTeleportOnRelease = true; // Added to ensure we only teleport on release
    [SerializeField] private bool constrainMarkerToMinimapBounds = true; // Constrain marker movement
    
    private Vector3 cachedTeleportPosition;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable markerGrabInteractable;
    private bool isMarkerGrabbed = false; // To track if marker is being held
    private bool teleportRequested = false; // Track if teleport has been requested
    private Vector3 originalMarkerLocalPosition; // Store original marker position
    
    // Event that other scripts can subscribe to for debugging
    public delegate void TeleportEvent(string message, Vector3 position);
    public static event TeleportEvent OnTeleportEvent;
    
    // Public accessor for the raw image for other components 
    public RawImage MinimapImage => minimapImage;
    
    private void Awake()
    {
        LogEvent("MinimapTeleporter Awake");
        
        // Find XR Origin if not assigned
        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
                Debug.LogError("[MinimapTeleporter] No XROrigin found in scene!");
        }
        
        // Create teleport marker visual if needed
        if (teleportMarkerVisual == null)
        {
            teleportMarkerVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teleportMarkerVisual.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
            teleportMarkerVisual.GetComponent<Renderer>().material.color = markerColor;
            teleportMarkerVisual.GetComponent<Collider>().enabled = false;
        }
        
        // Store original marker position
        if (markerTransform != null)
        {
            originalMarkerLocalPosition = markerTransform.localPosition;
        }
        
        SetupMarkerInteraction();
        
        // Hide teleport marker initially
        if (teleportMarkerVisual != null)
        {
            teleportMarkerVisual.SetActive(false);
        }
        
        // Check if we need to add a UIConstraint component
        if (constrainMarkerToMinimapBounds && markerTransform != null)
        {
            var constraint = markerTransform.GetComponent<MinimapUIConstraint>();
            if (constraint == null)
            {
                constraint = markerTransform.gameObject.AddComponent<MinimapUIConstraint>();
                constraint.enabled = true;
                
                // Set up references
                System.Reflection.FieldInfo minimapImageField = typeof(MinimapUIConstraint).GetField("minimapImage", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                System.Reflection.FieldInfo grabInteractableField = typeof(MinimapUIConstraint).GetField("grabInteractable", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    
                System.Reflection.FieldInfo teleporterField = typeof(MinimapUIConstraint).GetField("teleporter", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                if (minimapImageField != null)
                    minimapImageField.SetValue(constraint, minimapImage);
                    
                if (grabInteractableField != null && markerGrabInteractable != null)
                    grabInteractableField.SetValue(constraint, markerGrabInteractable);
                    
                if (teleporterField != null)
                    teleporterField.SetValue(constraint, this);
                
                LogEvent("Added MinimapUIConstraint component to marker");
            }
        }
    }
    
    private void SetupMarkerInteraction()
    {
        // Find marker grab interactable
        if (markerTransform != null)
        {
            markerGrabInteractable = markerTransform.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (markerGrabInteractable != null)
            {
                LogEvent("Found grab interactable on marker", markerTransform.position);
                
                // Remove any existing listeners to avoid duplicates
                markerGrabInteractable.selectEntered.RemoveListener(OnMarkerGrabbed);
                markerGrabInteractable.selectExited.RemoveListener(OnMarkerReleased);
                
                // Add our listeners
                markerGrabInteractable.selectEntered.AddListener(OnMarkerGrabbed);
                markerGrabInteractable.selectExited.AddListener(OnMarkerReleased);
                
                // Modify grab interactable to keep markers in place
                if (constrainMarkerToMinimapBounds)
                {
                    // Keep the marker's local z position fixed
                    markerGrabInteractable.trackPosition = true;
                    markerGrabInteractable.smoothPosition = true;
                    markerGrabInteractable.throwOnDetach = false;
                    
                    // Disable rotation for UI elements
                    markerGrabInteractable.trackRotation = false;
                }
            }
            else
            {
                Debug.LogWarning("[MinimapTeleporter] No XRGrabInteractable found on marker!");
            }
        }
        else
        {
            Debug.LogError("[MinimapTeleporter] No marker transform assigned!");
        }
    }
    
    private void OnDestroy()
    {
        LogEvent("MinimapTeleporter OnDestroy");
        
        if (markerGrabInteractable != null)
        {
            markerGrabInteractable.selectEntered.RemoveListener(OnMarkerGrabbed);
            markerGrabInteractable.selectExited.RemoveListener(OnMarkerReleased);
        }
        
        // Cancel any pending teleport operations
        CancelInvoke("PerformTeleport");
    }
    
    private void OnMarkerGrabbed(SelectEnterEventArgs args)
    {
        // Set flag that we're grabbing the marker
        isMarkerGrabbed = true;
        teleportRequested = false; // Reset teleport request on grab
        
        LogEvent("Marker GRABBED - canceling teleports", markerTransform.position);
        
        // Cancel any pending teleports when grabbing
        CancelInvoke("PerformTeleport");
        
        // Start updating teleport destination during drag
        StartCoroutine(UpdateDuringDrag());
    }
    
    private void OnMarkerReleased(SelectExitEventArgs args)
    {
        LogEvent("Marker RELEASED", markerTransform.position);
        
        // Calculate teleport position from marker
        UpdateTeleportDestination();
        
        // Set flags - important order: request teleport, then set not grabbed
        teleportRequested = true;
        isMarkerGrabbed = false;
        
        // Only teleport on release if option is enabled
        if (onlyTeleportOnRelease)
        {
            // Add small delay before teleporting to allow UI to update
            LogEvent("Requesting delayed teleport", cachedTeleportPosition);
            Invoke("PerformTeleport", teleportDelay);
        }
    }
    
    private System.Collections.IEnumerator UpdateDuringDrag()
    {
        LogEvent("Starting drag update coroutine");
        
        while (isMarkerGrabbed && markerGrabInteractable != null && markerGrabInteractable.isSelected)
        {
            // Constrain marker position to minimap bounds
            if (constrainMarkerToMinimapBounds)
            {
                ConstrainMarkerPosition();
            }
            
            UpdateTeleportDestination();
            yield return null;
        }
        
        LogEvent("Drag update coroutine ended");
    }
    
    private void ConstrainMarkerPosition()
    {
        if (minimapImage == null || markerTransform == null) return;
        
        // Get the RectTransform of the minimap image
        RectTransform minimapRect = minimapImage.rectTransform;
        if (minimapRect == null) return;
        
        // Get the local position of the marker
        Vector3 localPos = markerTransform.localPosition;
        
        // Calculate bounds in local space
        float halfWidth = minimapRect.rect.width * 0.5f;
        float halfHeight = minimapRect.rect.height * 0.5f;
        
        // Clamp x and y to keep marker within minimap bounds
        localPos.x = Mathf.Clamp(localPos.x, -halfWidth, halfWidth);
        localPos.y = Mathf.Clamp(localPos.y, -halfHeight, halfHeight);
        
        // Keep z the same as the original position to maintain the plane
        localPos.z = originalMarkerLocalPosition.z;
        
        // Apply constrained position
        markerTransform.localPosition = localPos;
    }
    
    // Public method to update destination, can be called by UI constraint
    public void UpdateDestinationFromMarker()
    {
        if (isMarkerGrabbed && markerTransform != null)
        {
            UpdateTeleportDestination();
        }
    }
    
    private void UpdateTeleportDestination()
    {
        if (minimapImage == null || minimapCamera == null || markerTransform == null)
        {
            Debug.LogError("[MinimapTeleporter] Missing required references!");
            return;
        }
        
        // Convert marker position on minimap to world position
        Vector2 normalizedPos = ConvertMarkerPositionToNormalized();
        Vector3 newPosition = ConvertNormalizedToWorldPosition(normalizedPos);
        
        // Only log if position changed significantly
        if (Vector3.Distance(newPosition, cachedTeleportPosition) > 0.1f)
        {
            LogEvent("Teleport destination updated", newPosition);
        }
        
        cachedTeleportPosition = newPosition;
        
        // Update teleport marker visual position
        if (teleportMarkerVisual != null)
        {
            teleportMarkerVisual.transform.position = cachedTeleportPosition + Vector3.up * 0.01f; // Slight offset to avoid z-fighting
            teleportMarkerVisual.SetActive(true);
        }
    }
    
    private Vector2 ConvertMarkerPositionToNormalized()
    {
        RectTransform minimapRect = minimapImage.rectTransform;
        Vector2 localPos = markerTransform.localPosition;
        Rect rect = minimapRect.rect;
        
        // Calculate normalized position (0-1 range) of marker within minimap
        return new Vector2(
            (localPos.x + rect.width * 0.5f) / rect.width,
            (localPos.y + rect.height * 0.5f) / rect.height
        );
    }
    
    private Vector3 ConvertNormalizedToWorldPosition(Vector2 normalizedPos)
    {
        // Cast ray from minimapCamera using normalized position
        Ray ray = minimapCamera.ViewportPointToRay(new Vector3(normalizedPos.x, normalizedPos.y, 0));
        
        // Raycast to find ground position
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.point + Vector3.up * teleportHeight;
        }
        
        // Fallback: Use plane at y=0 if no ground hit
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance) + Vector3.up * teleportHeight;
        }
        
        LogEvent("Couldn't determine ground position - using fallback");
        return new Vector3(ray.GetPoint(10).x, teleportHeight, ray.GetPoint(10).z);
    }
    
    public void PerformTeleport()
    {
        LogEvent("PerformTeleport called", cachedTeleportPosition);
        
        // Multiple safety checks to ensure we're not teleporting while grabbing
        if (isMarkerGrabbed)
        {
            LogEvent("TELEPORT CANCELED - Marker is still grabbed");
            return;
        }
        
        if (!teleportRequested)
        {
            LogEvent("TELEPORT CANCELED - No teleport was requested");
            return;
        }
        
        if (xrOrigin == null)
        {
            LogEvent("TELEPORT CANCELED - No XR Origin found");
            return;
        }
        
        // Actually perform the teleport
        LogEvent("TELEPORTING PLAYER", cachedTeleportPosition);
        xrOrigin.MoveCameraToWorldLocation(cachedTeleportPosition);
        
        // Reset teleport request after successful teleport
        teleportRequested = false;
        
        // Hide the teleport marker after teleporting
        if (teleportMarkerVisual != null)
        {
            teleportMarkerVisual.SetActive(false);
        }
    }
    
    // Public method to facilitate testing
    public void TestTeleport(Vector3 position)
    {
        LogEvent("Manual TestTeleport called", position);
        cachedTeleportPosition = position;
        teleportRequested = true;
        PerformTeleport();
    }
    
    // Reset marker to center of minimap
    public void ResetMarkerPosition()
    {
        if (markerTransform != null)
        {
            markerTransform.localPosition = originalMarkerLocalPosition;
            LogEvent("Marker position reset to original position");
            UpdateTeleportDestination();
        }
    }
    
    // Helper to log events both to console and to event listeners
    public void LogEvent(string message, Vector3 position = default)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[MinimapTeleporter] {message}" + (position != default ? $" at {position}" : ""));
        }
        
        // Broadcast event for any listeners
        OnTeleportEvent?.Invoke(message, position);
    }
    
    // Check if marker is being grabbed
    public bool IsMarkerGrabbed()
    {
        return isMarkerGrabbed;
    }
} 