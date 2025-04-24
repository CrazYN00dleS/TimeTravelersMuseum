using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

public class SelfGuidingFire : MonoBehaviour
{

    public VisualEffect vfx;
    public GameObject m_Destination;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Destination = GameObject.FindGameObjectWithTag("GuideAttraction");
        UpdateGuidingDestination(m_Destination);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateGuidingDestination(GameObject newDestination)
    {
        if (vfx != null)
        {
            Vector3 new_destination = transform.InverseTransformPoint(newDestination.transform.position);
            vfx.SetVector3("AttractTarget", new_destination);
        }
    }
}
