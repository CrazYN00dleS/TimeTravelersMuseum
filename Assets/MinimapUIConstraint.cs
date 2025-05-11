using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// This component forces a grabbed object to stay confined to a UI RawImage
public class MinimapUIConstraint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private MinimapTeleporter teleporter;
    
    [Header("Settings")]
    [SerializeField] private bool enforceConstraints = true;
    [SerializeField] private float zPosition = 0f;
    
    private XRGrabInteractable grabInteractable;
    private Vector3 originalLocalPosition;
    private RectTransform minimapRect;
    
    private void Start()
    {
        // Record initial position
        originalLocalPosition = transform.localPosition;
        
        // Get minimap rect reference
        if (minimapImage != null)
        {
            minimapRect = minimapImage.rectTransform;
        }
        
        // Get grab interactable
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        // Find teleporter if needed
        if (teleporter == null)
        {
            teleporter = FindObjectOfType<MinimapTeleporter>();
        }
    }
    
    private void LateUpdate()
    {
        if (!enforceConstraints) return;
        
        // Only constrain if we have the minimap rect
        if (minimapRect == null || minimapImage == null) return;
        
        // Keep z position fixed to the original z position
        Vector3 position = transform.localPosition;
        position.z = zPosition;
        
        // Constrain to minimap bounds
        if (minimapRect != null)
        {
            Rect rect = minimapRect.rect;
            
            // Convert from minimap coordinates to local coordinates
            float minX = rect.xMin;
            float maxX = rect.xMax;
            float minY = rect.yMin;
            float maxY = rect.yMax;
            
            // Clamp the marker position to the minimap bounds
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }
        
        // Apply the constrained position
        transform.localPosition = position;
        
        // Update the teleporter
        if (grabInteractable != null && 
            grabInteractable.isSelected && 
            teleporter != null)
        {
            teleporter.UpdateDestinationFromMarker();
        }
    }
    
    public void ResetPosition()
    {
        transform.localPosition = originalLocalPosition;
    }
} 