using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MinimapMarkerBounds : MonoBehaviour
{
    [Header("References")]
    public RawImage minimapImage; // The RawImage parent
    public CrystalBallPortal portalController; // Your existing portal controller
    public Camera minimapCamera; // The camera that renders the minimap scene

    [Header("Settings")]
    public bool updateOnDrag = true; // Whether to update the target position while dragging
    public bool clampToBounds = true; // Whether to restrict movement to the RawImage bounds

    private RectTransform minimapRect;
    private Vector2 lastValidPosition;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        // Get references if not assigned
        if (minimapImage == null)
            minimapImage = GetComponentInParent<RawImage>();

        if (minimapImage != null)
            minimapRect = minimapImage.rectTransform;

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // Listen for the end of movement to update the teleport target
            grabInteractable.selectExited.AddListener(OnReleased);

            if (updateOnDrag)
            {
                // For continuous updates while dragging
                // Note: This might be performance intensive
                grabInteractable.selectEntered.AddListener(OnGrabbed);
            }
        }

        // Store initial position
        if (minimapRect != null)
            lastValidPosition = transform.localPosition;
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnReleased);

            if (updateOnDrag)
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Start updating position continuously during drag
        if (updateOnDrag)
            StartCoroutine(UpdateDuringDrag());
    }

    System.Collections.IEnumerator UpdateDuringDrag()
    {
        // Update while being held
        while (grabInteractable.isSelected)
        {
            ClampPositionAndUpdateTarget();
            yield return null;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // When released, ensure position is valid and update the teleport target
        ClampPositionAndUpdateTarget();
    }

    void Update()
    {
        // Optional: Continuous checking regardless of grab state
        // Can be disabled if performance is a concern
        if (!updateOnDrag && !grabInteractable.isSelected)
        {
            ClampPositionAndUpdateTarget();
        }
    }

    void ClampPositionAndUpdateTarget()
    {
        if (minimapRect == null) return;

        // Get current local position
        Vector3 localPos = transform.localPosition;

        if (clampToBounds)
        {
            // Calculate boundaries based on RawImage rect
            Rect rect = minimapRect.rect;
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;

            // Clamp position within boundaries
            localPos.x = Mathf.Clamp(localPos.x, -halfWidth, halfWidth);
            localPos.y = Mathf.Clamp(localPos.y, -halfHeight, halfHeight);

            // Apply clamped position
            transform.localPosition = localPos;
            lastValidPosition = localPos;
        }

        // Calculate normalized position (0-1) within minimap
        CalculateWorldPositionFromMarker();
    }

    void CalculateWorldPositionFromMarker()
    {
        if (portalController == null || minimapRect == null || minimapCamera == null)
            return;

        // Get the marker's position relative to the minimap
        Vector2 localPos = transform.localPosition;

        // Calculate normalized coordinates (0-1) based on minimap size
        Rect rect = minimapRect.rect;
        Vector2 normalizedPos = new Vector2(
            (localPos.x + rect.width * 0.5f) / rect.width,
            (localPos.y + rect.height * 0.5f) / rect.height
        );

        // Convert to viewport coordinates
        Vector3 viewportPoint = new Vector3(normalizedPos.x, normalizedPos.y, 0);

        // Create a ray from the minimap camera through this viewport point
        Ray ray = minimapCamera.ViewportPointToRay(viewportPoint);

        // Define ground plane (assuming Y=0 is ground)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Cast ray to find world position
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);

            // Update the teleport target in scene
            if (portalController.teleportMarker != null)
            {
                portalController.teleportMarker.transform.position = worldPos;

                // Store this position for teleporting
                portalController.SetTeleportTarget(worldPos);

                //Debug.Log($"Updated teleport target to: {worldPos}");
            }
        }
    }
}