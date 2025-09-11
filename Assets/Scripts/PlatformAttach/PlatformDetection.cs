using UnityEngine;

public class PlatformDetection : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 platformLastPosition;
    private PlatformObj currentPlatform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (currentPlatform != null)
        {
            Vector3 platformDelta = currentPlatform.transform.position - platformLastPosition;
            rb.MovePosition(rb.position + platformDelta);
            platformLastPosition = currentPlatform.transform.position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlatformObj platform = collision.collider.GetComponent<PlatformObj>();
        if (platform != null)
        {
            currentPlatform = platform;
            platformLastPosition = currentPlatform.transform.position;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.GetComponent<PlatformObj>() == currentPlatform)
        {
            currentPlatform = null;
        }
    }
}
