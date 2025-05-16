using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

public class SnapToFrame : MonoBehaviour
{
    public GameObject imageEmptyObject;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //private void OnTriggerEnter(Collider other)
    //{

    //}

    private void OnCollisionEnter(Collision collision)
    {
        imageEmptyObject = collision.gameObject;

        if (imageEmptyObject != null && imageEmptyObject.CompareTag("InvisibleFrame"))
        {
            Debug.Log("Photo entered frame zone!");

            // Set parent
            imageEmptyObject.transform.parent = gameObject.transform;
            Transform parent = imageEmptyObject.transform.parent;
            Vector3 midpoint = new Vector3(0, -0.01f, parent.localScale.z / 4);
            imageEmptyObject.transform.localPosition = midpoint;
        }
    }
}
