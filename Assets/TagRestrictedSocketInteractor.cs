using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[AddComponentMenu("XR/Interactors/Tag Restricted Socket Interactor")]
public class TagRestrictedSocketInteractor : XRSocketInteractor
{
    [Tooltip("Only objects with one of these tags will be accepted.")]
    public string[] allowedTags;

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        if (!base.CanSelect(interactable))
            return false;

        // Check if tag is allowed
        var go = interactable.transform.gameObject;
        foreach (var tag in allowedTags)
        {
            if (go.CompareTag(tag))
                return true;
        }

        return false;
    }
}
