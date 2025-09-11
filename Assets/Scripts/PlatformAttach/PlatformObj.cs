using UnityEngine;
using System.Collections.Generic;

public class PlatformObj : MonoBehaviour
{
    private enum DetectionShape
    {
        Box,
        Sphere,
        Capsule,
        Mesh
    }

    [Header("Detection Settings")]
    [SerializeField] private Vector3 detectPosition = Vector3.zero; 
    [SerializeField] private DetectionShape detectionShape = DetectionShape.Box;
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private float sphereRadius = 0.5f;
    [SerializeField] private float capsuleRadius = 0.5f;
    [SerializeField] private float capsuleHeight = 2f;
    [SerializeField] private Mesh meshPreview;

    [Header("Gizmos Settings")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.25f);

    private HashSet<Transform> trackedObjects = new HashSet<Transform>();

    private void Update()
    {
        DetectAndParent();
    }

    private void DetectAndParent()
    {
        Collider[] hits = new Collider[20]; // รองรับสูงสุด 20 object
        int count = 0;

        switch (detectionShape)
        {
            case DetectionShape.Box:
                count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(detectPosition), boxSize * 0.5f, hits, transform.rotation);
                break;
            case DetectionShape.Sphere:
                count = Physics.OverlapSphereNonAlloc(transform.TransformPoint(detectPosition), sphereRadius, hits);
                break;
            case DetectionShape.Capsule:
                Vector3 up = transform.TransformPoint(detectPosition) + transform.up * Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
                Vector3 down = transform.TransformPoint(detectPosition) - transform.up * Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
                count = Physics.OverlapCapsuleNonAlloc(up, down, capsuleRadius, hits);
                break;
            case DetectionShape.Mesh:
                if (meshPreview != null)
                {
                    Bounds bounds = meshPreview.bounds;
                    count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(detectPosition + bounds.center), bounds.extents, hits, transform.rotation);
                }
                break;
        }

        HashSet<Transform> currentDetected = new HashSet<Transform>();

        for (int i = 0; i < count; i++)
        {
            if (hits[i] != null)
            {
                PlatformDetection pd = hits[i].GetComponent<PlatformDetection>();
                if (pd != null)
                {
                    currentDetected.Add(pd.transform);
                    if (!trackedObjects.Contains(pd.transform))
                    {
                        // ใส่ Parent ให้ติดตาม
                        pd.transform.SetParent(transform);
                        trackedObjects.Add(pd.transform);
                    }
                }
            }
        }

        // ตรวจสอบ Object ที่ออกจาก Detection แล้ว
        HashSet<Transform> toRemove = new HashSet<Transform>();
        foreach (var obj in trackedObjects)
        {
            if (!currentDetected.Contains(obj))
            {
                obj.SetParent(null); // ปลด Parent
                toRemove.Add(obj);
            }
        }

        foreach (var obj in toRemove)
        {
            trackedObjects.Remove(obj);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;

        switch (detectionShape)
        {
            case DetectionShape.Box:
                Gizmos.DrawCube(detectPosition, boxSize);
                Gizmos.DrawWireCube(detectPosition, boxSize);
                break;
            case DetectionShape.Sphere:
                Gizmos.DrawSphere(detectPosition, sphereRadius);
                Gizmos.DrawWireSphere(detectPosition, sphereRadius);
                break;
            case DetectionShape.Capsule:
                float halfHeight = Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
                Vector3 up = Vector3.up * halfHeight;
                Gizmos.DrawWireSphere(detectPosition + up, capsuleRadius);
                Gizmos.DrawWireSphere(detectPosition - up, capsuleRadius);
                Gizmos.DrawLine(detectPosition + up + Vector3.forward * capsuleRadius, detectPosition - up + Vector3.forward * capsuleRadius);
                Gizmos.DrawLine(detectPosition + up - Vector3.forward * capsuleRadius, detectPosition - up - Vector3.forward * capsuleRadius);
                Gizmos.DrawLine(detectPosition + up + Vector3.right * capsuleRadius, detectPosition - up + Vector3.right * capsuleRadius);
                Gizmos.DrawLine(detectPosition + up - Vector3.right * capsuleRadius, detectPosition - up - Vector3.right * capsuleRadius);
                break;
            case DetectionShape.Mesh:
                if (meshPreview != null)
                {
                    Gizmos.DrawMesh(meshPreview, detectPosition, Quaternion.identity, Vector3.one);
                    Gizmos.DrawWireMesh(meshPreview, detectPosition, Quaternion.identity, Vector3.one);
                }
                break;
        }
    }
}
