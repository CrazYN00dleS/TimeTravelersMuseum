using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections;
using GaussianSplatting.Runtime;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CrystalBallPortal : MonoBehaviour
{
    [Header("Setup")]
    public GaussianSplatAsset myAsset;
    public GaussianSplatRenderer sharedRenderer;
    public Transform defaultTeleportTarget;
    public XROrigin xrOrigin;
    public DynamicMoveProvider moveProvider;

    [Header("Minimap + UI Raycast")]
    public GameObject minimapUI; // RawImage canvas or plane with RenderTexture
    public RawImage minimapImage; // Assign RawImage directly
    public Camera minimapCamera; // Orthographic camera rendering the GS scene
    public GameObject teleportMarker; // Prefab placed in real GS scene

    public Transform rayOrigin; // Controller tip doing the UI raycast
    public LayerMask minimapLayer; // Layer for ray to hit RawImage

    [Header("Controller Tags")]
    public string leftControllerTag = "LeftController";
    public string rightControllerTag = "RightController";

    [Header("Teleport Settings")]
    public float teleportDelay = 0.5f; // Time to wait before teleport triggers
    public bool requireSecondControllerTrigger = true; // If true, need other controller to trigger teleport

    private XRBaseInteractor heldBy = null;
    private string grabbingControllerTag = ""; // Store which controller is grabbing
    private Vector3? selectedTeleportPoint = null;
    private XRGrabInteractable grabInteractable;
    private Collider grabberCollider; // Store the collider of the grabbing controller
    private bool teleportEnabled = false; // Only allow teleport after a short delay from grabbing

    [Header("Input")]
    public InputActionReference triggerAction; // Assign in Inspector (e.g. XRI LeftHand/Select or RightHand/Select)
    private bool wasButtonPressed = false;
    public TrackedDeviceGraphicRaycaster xrUIRaycaster; // Assign in Inspector
    public Canvas minimapCanvas; // Assign your UI canvas in Inspector


    private float personheight = 0;
    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
    }

    void Update()
    {
        if (heldBy == null || rayOrigin == null) return;

        

        // Check if trigger is pressed using the Input System
        bool isButtonPressed = false;
        if (triggerAction != null && triggerAction.action.enabled)
        {
            isButtonPressed = triggerAction.action.ReadValue<float>() > 0.5f;
        }

        //// Only perform raycast when button is newly pressed
        //if (isButtonPressed && !wasButtonPressed)
        //{
        //    Debug.Log("Trigger pressed! Attempting raycast...");
        //    PerformRaycast();
        //}
        //Debug.Log(selectedTeleportPoint);
        // Update the button state
        wasButtonPressed = isButtonPressed;
    }
    //void PerformRaycast()
    //{
    //    Debug.Log($"Raycasting from {rayOrigin.position} in direction {rayOrigin.forward}");
    //    // Draw a longer visible ray for debugging in the scene view
    //    Debug.DrawRay(rayOrigin.position, rayOrigin.forward * 20f, Color.red, 1f);
    //    // First try physics raycast
    //    if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, 10f, minimapLayer))
    //    {
    //        Debug.Log($"Hit object: {hit.collider.gameObject.name} at point {hit.point}");
    //        HandleMinimapClick(hit);
    //    }
    //    else
    //    {
    //        Debug.Log("No physics hit detected, trying UI raycast");

    //        // Try UI raycast for Canvas elements
    //        RaycastUI();
    //    }
    //}

    //void RaycastUI()
    //{
    //    if (minimapCanvas == null || EventSystem.current == null)
    //    {
    //        Debug.LogError("Missing Canvas or EventSystem references!");
    //        return;
    //    }

    //    // Create ray for UI raycasting
    //    Vector3 rayOriginPos = rayOrigin.position;
    //    Vector3 rayDirection = rayOrigin.forward;

    //    // Create a list to store results
    //    List<RaycastResult> results = new List<RaycastResult>();

    //    // Create pointer event data for the raycast
    //    PointerEventData eventData = new PointerEventData(EventSystem.current);
    //    eventData.position = Camera.main.WorldToScreenPoint(rayOriginPos + rayDirection * 5f);

    //    // Raycast against UI
    //    EventSystem.current.RaycastAll(eventData, results);

    //    // Debug all results
    //    Debug.Log($"UI Raycast found {results.Count} hits");

    //    foreach (RaycastResult result in results)
    //    {
    //        Debug.Log($"Hit UI: {result.gameObject.name}");

    //        // Check if this is our minimap image
    //        RawImage rawImage = result.gameObject.GetComponent<RawImage>();
    //        if (rawImage != null && rawImage == minimapImage)
    //        {
    //            Debug.Log("Found our minimap image!");

    //            // Convert screen point to local point in RawImage
    //            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //                minimapImage.rectTransform,
    //                result.screenPosition,
    //                result.module.eventCamera,
    //                out Vector2 localPoint))
    //            {
    //                // Calculate normalized coordinates within the image
    //                Rect rect = minimapImage.rectTransform.rect;
    //                Vector2 normalizedPoint = new Vector2(
    //                    (localPoint.x - rect.x) / rect.width,
    //                    (localPoint.y - rect.y) / rect.height
    //                );

    //                // Convert to viewport coordinates (0-1)
    //                Vector2 viewportPoint = new Vector2(
    //                    Mathf.Clamp01(normalizedPoint.x),
    //                    Mathf.Clamp01(normalizedPoint.y)
    //                );

    //                Debug.Log($"Normalized point: {normalizedPoint}, Viewport point: {viewportPoint}");
    //                PlaceTeleportMarkerFromViewport(viewportPoint);
    //                return;
    //            }
    //        }
    //    }

    //    Debug.Log("No valid UI hits on minimap");
    //}


    public void SetTeleportTarget(Vector3 position)
    {
        // Store the selected teleport point
        selectedTeleportPoint = position;

        // Update the marker's position
        if (teleportMarker != null)
        {
            teleportMarker.transform.position = position;
            teleportMarker.SetActive(true);
        }
    }

    //void HandleMinimapClick(RaycastHit hit)
    //{
    //    // Check if the hit object contains our RawImage
       
    //    if (hit.collider.gameObject.GetComponentInParent<RawImage>() == minimapImage ||
    //        hit.collider.gameObject == minimapImage.gameObject)
    //    {
    //        Debug.Log("contains RawImage");
    //        // Convert world hit point to screen space
    //        Vector2 screenPoint = Camera.main.WorldToScreenPoint(hit.point);

    //        // Convert screen point to UI local point
    //        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
    //            minimapImage.rectTransform,
    //            screenPoint,
    //            null, // No camera needed for screen space coordinates
    //            out Vector2 localPoint))
    //        {
    //            // Calculate normalized coordinates (0-1) within the RawImage
    //            Rect rect = minimapImage.rectTransform.rect;
    //            Vector2 normalizedPoint = new Vector2(
    //                (localPoint.x - rect.x) / rect.width,
    //                (localPoint.y - rect.y) / rect.height
    //            );

    //            // Convert normalized coordinates to viewport coordinates
    //            // Clamp to ensure we stay within bounds
    //            Vector2 viewportPoint = new Vector2(
    //                Mathf.Clamp01(normalizedPoint.x),
    //                Mathf.Clamp01(normalizedPoint.y)
    //            );

    //            PlaceTeleportMarkerFromViewport(viewportPoint);
    //        }
    //    }
    //}

    //void PlaceTeleportMarkerFromViewport(Vector2 viewportPoint)
    //{
    //    // Convert viewport point to world position using the minimap camera
    //    // We want a point on the ground plane, so use a ray from the camera
    //    Ray ray = minimapCamera.ViewportPointToRay(new Vector3(viewportPoint.x, viewportPoint.y, 0));

    //    // Define the ground plane (assuming Y is up and ground is at Y=0)
    //    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

    //    // Cast ray against ground plane
    //    if (groundPlane.Raycast(ray, out float distance))
    //    {
    //        Vector3 worldPos = ray.GetPoint(distance);

    //        // Store the selected teleport point
    //        selectedTeleportPoint = worldPos;

    //        // Place the marker at the target location
    //        if (teleportMarker != null)
    //        {
    //            teleportMarker.transform.position = worldPos;
    //            teleportMarker.SetActive(true);

    //            Debug.Log($"Teleport marker placed at: {worldPos}");
    //        }
    //    }
    //}

    void OnGrab(SelectEnterEventArgs args)
    {
        heldBy = args.interactorObject as XRBaseInteractor;

        // Get the collider of the grabbing controller to ignore it in trigger events
        grabberCollider = heldBy.GetComponentInChildren<Collider>();

        // Get the parent controller GameObject that has the tag
        // Try to find the parent GameObject with the appropriate tag
        Transform parent = heldBy.transform;
        while (parent != null)
        {
            if (parent.CompareTag(leftControllerTag) || parent.CompareTag(rightControllerTag))
            {
                grabbingControllerTag = parent.tag;
                Debug.Log("Ball grabbed by controller with tag: " + grabbingControllerTag);
                break;
            }
            parent = parent.parent;
        }

        // If we didn't find a tagged parent, log the issue
        if (string.IsNullOrEmpty(grabbingControllerTag))
        {
            Debug.LogWarning("Could not find a tagged controller parent. Interactor: " + heldBy.name);
            // Try to determine left/right from the name or other property
            if (heldBy.name.ToLower().Contains("left"))
            {
                grabbingControllerTag = leftControllerTag;
                Debug.Log("Using left controller tag based on name");
            }
            else if (heldBy.name.ToLower().Contains("right"))
            {
                grabbingControllerTag = rightControllerTag;
                Debug.Log("Using right controller tag based on name");
            }
            else
            {
                // Default to something so we have a value
                Debug.LogError("Unable to determine controller type. Set an explicit tag. Interactor: " + heldBy.name);
            }
        }

        // Load GS scene
        sharedRenderer.m_Asset = myAsset;
        sharedRenderer.gameObject.SetActive(true);

        // Show minimap UI
        if (minimapUI != null)
            minimapUI.SetActive(true);

        if (teleportMarker != null)
            teleportMarker.SetActive(false);

        // Important: Disable teleport initially to prevent immediate collision from triggering
        teleportEnabled = false;
        StartCoroutine(EnableTeleportAfterDelay(0.2f)); // Short delay to prevent immediate collision
    }

    private IEnumerator EnableTeleportAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        teleportEnabled = true;
        Debug.Log("Teleport now enabled - touch ball with other controller to trigger");
    }

    void OnRelease(SelectExitEventArgs args)
    {
        heldBy = null;
        grabberCollider = null;
        grabbingControllerTag = "";
        teleportEnabled = false;

        if (minimapUI != null)
            minimapUI.SetActive(false);

        if (teleportMarker != null)
            teleportMarker.SetActive(false);

        //selectedTeleportPoint = null;
    }

    void OnTriggerEnter(Collider other)
    {
        // Only proceed if:
        // 1. We're holding the object
        // 2. Teleport is enabled (after delay)
        if (heldBy == null || !teleportEnabled) return;

        // Ignore the grabbing controller's collider
        if (grabberCollider != null && other == grabberCollider) return;

        // Check if the trigger is from the opposite controller using tags
        string otherTag = other.gameObject.tag;

        // If the other controller has the opposite tag of the grabbing controller
        bool isOppositeController = false;

        if (grabbingControllerTag == leftControllerTag && otherTag == rightControllerTag)
        {
            isOppositeController = true;
            Debug.Log("Right controller touched the ball");
        }
        else if (grabbingControllerTag == rightControllerTag && otherTag == leftControllerTag)
        {
            isOppositeController = true;
            Debug.Log("Left controller touched the ball");
        }

        // Start teleport if it's the opposite controller or a player body part
        if (isOppositeController || other.CompareTag("Player"))
        {
            Debug.Log("Triggering teleport from secondary controller touch");
            StartCoroutine(Teleport());
        }
    }

    private IEnumerator Teleport()
    {
        // Prevent multiple teleports
        teleportEnabled = false;

        // Disable grab interaction during teleport TODO!!!!!!!!!
        grabInteractable.enabled = false;
        if (moveProvider != null)
            moveProvider.enabled = false;

        // Optional visual effect or feedback before teleport
        Debug.Log("Teleporting in " + teleportDelay + " seconds...");
        yield return new WaitForSeconds(teleportDelay);
        Debug.Log(selectedTeleportPoint);
        Vector3 targetPos;
        if (selectedTeleportPoint!=null)
        {
            targetPos = selectedTeleportPoint.Value;
            Debug.Log("Teleporting to selected point: " + targetPos);
        }
        else if (defaultTeleportTarget != null)
        {
            targetPos = defaultTeleportTarget.position;
            Debug.Log("Teleporting to default target: " + targetPos);
        }
        else
        {
            Debug.LogWarning("No teleport target available");
            yield break;
        }

        if (xrOrigin != null)
        {
            // Teleport the XR Origin to the target location
            //xrOrigin.MoveCameraToWorldLocation(targetPos);
            xrOrigin.transform.position = targetPos;
        }

        // Re-enable movement after teleport
        if (moveProvider != null)
            moveProvider.enabled = true;

        grabInteractable.enabled = true;
    }
}