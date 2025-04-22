using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using GaussianSplatting.Runtime;
using Unity.XR.CoreUtils;
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

    [Header("Minimap + Teleport")]
    public GameObject minimapUI; // Minimap canvas or plane with RenderTexture
    public GameObject teleportMarker; // Visual marker prefab
    public LayerMask teleportLayer;
    public Transform rayOrigin; // Off-hand controller tip (raycast origin)

    private XRBaseInteractor heldBy = null;
    private Vector3? selectedTeleportPoint = null;

    private XRGrabInteractable grabInteractable;

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
        if (heldBy != null && rayOrigin != null)
        {
            if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, 100f, teleportLayer))
            {
                selectedTeleportPoint = hit.point;

                if (teleportMarker != null)
                {
                    teleportMarker.SetActive(true);
                    teleportMarker.transform.position = hit.point;
                }
            }
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        heldBy = args.interactorObject as XRBaseInteractor;

        // Load GS scene
        sharedRenderer.m_Asset = myAsset;
        sharedRenderer.gameObject.SetActive(true);

        // Show minimap and teleport UI
        if (minimapUI != null)
            minimapUI.SetActive(true);

        if (teleportMarker != null)
            teleportMarker.SetActive(false); // Hide until user points
    }

    void OnRelease(SelectExitEventArgs args)
    {
        heldBy = null;

        if (minimapUI != null)
            minimapUI.SetActive(false);

        if (teleportMarker != null)
            teleportMarker.SetActive(false);

        selectedTeleportPoint = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (heldBy == null) return;

        if (other.transform != heldBy.transform && (other.CompareTag("Controller") || other.CompareTag("Player")))
        {
            StartCoroutine(Teleport());
        }
    }

    private IEnumerator Teleport()
    {
        grabInteractable.enabled = false;
        if (moveProvider != null)
            moveProvider.enabled = false;

        yield return new WaitForSeconds(0.2f);

        Vector3 targetPos = selectedTeleportPoint ?? defaultTeleportTarget.position;

        if (xrOrigin != null)
            xrOrigin.MoveCameraToWorldLocation(targetPos);

        if (moveProvider != null)
            moveProvider.enabled = true;
        grabInteractable.enabled = true;
    }
}
