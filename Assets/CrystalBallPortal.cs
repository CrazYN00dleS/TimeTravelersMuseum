// CrystalBallPortal.cs �C Updated: scene persists while holding, deactivates on release
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections;
using GaussianSplatting.Runtime;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class CrystalBallPortal : MonoBehaviour
{
    [Header("Setup")]
    public GaussianSplatAsset myAsset;
    public GaussianSplatRenderer sharedRenderer;
    public Transform defaultTeleportTarget;
    public XROrigin xrOrigin;
    public DynamicMoveProvider moveProvider;

    [Header("Minimap & Teleport")]
    public GameObject minimapUI;
    public RawImage minimapImage;
    public Camera minimapCamera;
    public GameObject teleportMarker;
    public GameObject minimapMarker;

    [Header("Controllers & Tags")]
    public string leftControllerTag = "LeftController";
    public string rightControllerTag = "RightController";

    [Header("Teleport Timing")]
    public float teleportDelay = 0.5f;

    private XRGrabInteractable grabInteractable;
    private XRBaseInteractor heldBy;
    private bool teleportEnabled;
    private Vector3 selectedTeleportPoint;

    private bool isInPortal = false;
    private Vector3 originalRigPosition;
    private Vector3 originalCameraPosition;

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

    private void OnGrab(SelectEnterEventArgs args)
    {
        heldBy = args.interactorObject as XRBaseInteractor;
        minimapMarker.GetComponent<MinimapMarkerBounds>().setPortal(this);
        // Capture original position on first grab
        if (!isInPortal)
            originalCameraPosition = Camera.main.transform.position;
            //originalRigPosition = xrOrigin.transform.position;
        selectedTeleportPoint = defaultTeleportTarget.transform.position;
        // Always show scene and UI while holding
        sharedRenderer.m_Asset = myAsset;
        sharedRenderer.gameObject.SetActive(true);
        if (minimapUI) minimapUI.SetActive(true);
        if (teleportMarker) teleportMarker.SetActive(false);

        teleportEnabled = false;
        StartCoroutine(EnableTeleportAfterDelay());
    }

    private IEnumerator EnableTeleportAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        teleportEnabled = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (!isInPortal)
        {
            // On release, disable scene and UI
            heldBy = null;
            teleportEnabled = false;

            // Reset portal state
            isInPortal = false;

            // Hide scene and UI
            //sharedRenderer.gameObject.SetActive(false);
            sharedRenderer.m_Asset = null;
            if (minimapUI) minimapUI.SetActive(false);
            if (teleportMarker) teleportMarker.SetActive(false);
            if (minimapMarker) minimapMarker.GetComponent<MinimapMarkerBounds>().ResetToInitial();
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!teleportEnabled) return;
        if (other.CompareTag(leftControllerTag) || other.CompareTag(rightControllerTag) || other.CompareTag("Player"))
        {
            StartCoroutine(TeleportSequence());
        }
    }

    private IEnumerator TeleportSequence()
    {
        teleportEnabled = false;
        yield return new WaitForSeconds(teleportDelay);

        if (!isInPortal)
        {
            //xrOrigin.transform.position = selectedTeleportPoint;
            xrOrigin.MoveCameraToWorldLocation(selectedTeleportPoint);
            teleportMarker.SetActive(false);//hide the in-scene marker to avoid blocking view
            isInPortal = true;
        }
        else
        {
            // Exit portal: return to original position
            //xrOrigin.transform.position = originalRigPosition;
            xrOrigin.MoveCameraToWorldLocation(originalCameraPosition);
            teleportMarker.SetActive(false);
            isInPortal = false;
        }

        teleportEnabled = true;
    }

    /// <summary>
    /// Called by minimap UI to update the teleport destination.
    /// Persisted until changed again.
    /// </summary>
    public void SetTeleportTarget(Vector3 worldPos)
    {
        selectedTeleportPoint = worldPos;
        if (teleportMarker)
        {
            teleportMarker.transform.position = worldPos;
            teleportMarker.SetActive(true);
        }
    }
}