using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using GaussianSplatting.Runtime;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class CrystalBallPortal : MonoBehaviour
{
    public GaussianSplatAsset myAsset;
    public GaussianSplatRenderer sharedRenderer;

    public Transform teleportTarget;
    public XROrigin xrOrigin; // Assign in inspector or auto-locate
    public DynamicMoveProvider moveProvider; // for disabling input

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Controller"))
        {
            StartCoroutine(TeleportSequence());
        }
    }

    private IEnumerator TeleportSequence()
    {
        // Disable input
        if (moveProvider != null)
            moveProvider.enabled = false;

        // Optional: fade screen out here
        // yield return StartCoroutine(ScreenFade.FadeOut());

        // Load the GS scene
        if (sharedRenderer != null && myAsset != null)
        {
            sharedRenderer.m_Asset = myAsset;
            sharedRenderer.gameObject.SetActive(true);
        }

        // Wait a moment for any GPU upload
        yield return new WaitForSeconds(0.2f);

        // Teleport the XR Rig
        if (xrOrigin != null && teleportTarget != null)
        {
            xrOrigin.transform.position = teleportTarget.position;
            xrOrigin.transform.rotation = teleportTarget.rotation; // optional
        }

        // Optional: fade back in
        // yield return StartCoroutine(ScreenFade.FadeIn());

        // Re-enable input
        yield return new WaitForSeconds(0.1f);
        if (moveProvider != null)
            moveProvider.enabled = true;
    }
}
