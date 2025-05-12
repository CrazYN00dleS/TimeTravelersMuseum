using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections;
using GaussianSplatting.Runtime;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
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

    [Header("Teleport Offset")]
    public float verticalOffset = 0.5f;

    private XRGrabInteractable grabInteractable;
    private XRBaseInteractor heldBy;
    private bool teleportEnabled;
    private Vector3 selectedTeleportPoint;

    private bool isInPortal = false;
    private Vector3 originalCameraPosition;
    private string heldControllerTag;

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
        heldControllerTag = heldBy.transform.tag;

        minimapMarker.GetComponent<MinimapMarkerBounds>().setPortal(this);

        if (!isInPortal)
            originalCameraPosition = Camera.main.transform.position;

        selectedTeleportPoint = defaultTeleportTarget.transform.position;

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
            heldBy = null;
            heldControllerTag = "";
            teleportEnabled = false;

            isInPortal = false;

            sharedRenderer.m_Asset = null;
            if (minimapUI) minimapUI.SetActive(false);
            if (teleportMarker) teleportMarker.SetActive(false);
            if (minimapMarker) minimapMarker.GetComponent<MinimapMarkerBounds>().ResetToInitial();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!teleportEnabled) return;

        // Only trigger if the OTHER controller enters the trigger
        if ((other.CompareTag(leftControllerTag) && heldControllerTag != leftControllerTag) ||
            (other.CompareTag(rightControllerTag) && heldControllerTag != rightControllerTag))
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
            Vector3 adjustedTarget = selectedTeleportPoint + Vector3.up * verticalOffset;
            xrOrigin.MoveCameraToWorldLocation(adjustedTarget);
            teleportMarker.SetActive(false);
            isInPortal = true;
        }
        else
        {
            Vector3 adjustedReturn = originalCameraPosition;
            xrOrigin.MoveCameraToWorldLocation(adjustedReturn);
            teleportMarker.SetActive(false);
            isInPortal = false;
        }

        teleportEnabled = true;
    }

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
