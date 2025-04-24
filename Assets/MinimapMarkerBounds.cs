// MinimapMarkerBounds.cs ¨C Updated: record initial marker position and expose reset to initial
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MinimapMarkerBounds : MonoBehaviour
{
    [Header("References")]
    public RawImage minimapImage;               // The RawImage parent
    public CrystalBallPortal portalController;  // Reference to portal controller
    public Camera minimapCamera;                // Camera rendering the minimap scene

    [Header("Settings")]
    public bool updateOnDrag = true;            // Update target while dragging
    public bool clampToBounds = true;           // Restrict marker to bounds

    private RectTransform minimapRect;
    private Vector3 initialLocalPosition;        // Store initial marker position
    private Vector2 lastValidPosition;
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        // Get minimap rect transform
        if (minimapImage == null)
            minimapImage = GetComponentInParent<RawImage>();
        if (minimapImage != null)
            minimapRect = minimapImage.rectTransform;

        // Record initial position
        initialLocalPosition = transform.localPosition;
        lastValidPosition = initialLocalPosition;

        // Setup grab listeners
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.AddListener(OnReleased);
            if (updateOnDrag)
                grabInteractable.selectEntered.AddListener(OnGrabbed);
        }
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
        if (updateOnDrag)
            StartCoroutine(UpdateDuringDrag());
    }

    System.Collections.IEnumerator UpdateDuringDrag()
    {
        while (grabInteractable.isSelected)
        {
            ClampPositionAndUpdateTarget();
            yield return null;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        ClampPositionAndUpdateTarget();
        // Keep marker at release for portal use
    }

    void Update()
    {
        if (!updateOnDrag && !grabInteractable.isSelected)
            ClampPositionAndUpdateTarget();
    }

    void ClampPositionAndUpdateTarget()
    {
        if (minimapRect == null) return;

        Vector3 localPos = transform.localPosition;
        if (clampToBounds)
        {
            Rect rect = minimapRect.rect;
            float halfW = rect.width * 0.5f;
            float halfH = rect.height * 0.5f;
            localPos.x = Mathf.Clamp(localPos.x, -halfW, halfW);
            localPos.y = Mathf.Clamp(localPos.y, -halfH, halfH);
            transform.localPosition = localPos;
            lastValidPosition = localPos;
        }
        CalculateWorldPositionFromMarker();
    }

    void CalculateWorldPositionFromMarker()
    {
        if (portalController == null || minimapRect == null || minimapCamera == null)
            return;

        Vector2 localPos = transform.localPosition;
        Rect rect = minimapRect.rect;
        Vector2 normalizedPos = new Vector2(
            (localPos.x + rect.width * 0.5f) / rect.width,
            (localPos.y + rect.height * 0.5f) / rect.height
        );

        Ray ray = minimapCamera.ViewportPointToRay(new Vector3(normalizedPos.x, normalizedPos.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            if (portalController.teleportMarker != null)
                portalController.teleportMarker.transform.position = worldPos;
            portalController.SetTeleportTarget(worldPos);
        }
    }

    /// <summary>
    /// Reset marker to its initial position on minimap.
    /// </summary>
    public void ResetToInitial()
    {
        transform.localPosition = initialLocalPosition;
        lastValidPosition = initialLocalPosition;
        ClampPositionAndUpdateTarget();
    }
    
    public void setPortal(CrystalBallPortal portal)
    {
        portalController = portal;
    }
}