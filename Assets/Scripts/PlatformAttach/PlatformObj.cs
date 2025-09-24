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
    
    [Header("Pivot Settings")]
    [SerializeField] private Transform pivot;
    
    [Header("Detection Settings")]
    [SerializeField] private Vector3 detectPosition = Vector3.zero;
    [SerializeField] private DetectionShape detectionShape = DetectionShape.Box;
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private float sphereRadius = 0.5f;
    [SerializeField] private float capsuleRadius = 0.5f;
    [SerializeField] private float capsuleHeight = 2f;
    [SerializeField] private Mesh meshPreview;

    [Header("Performance Settings")]
    [SerializeField] private float detectionInterval = 0.1f; // ตรวจจับทุก 0.1 วินาที
    [SerializeField] private LayerMask detectionLayerMask = -1; // Layer ที่ต้องการตรวจจับ

    [Header("Gizmos Settings")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.25f);

    [SerializeField] private List<Rigidbody> trackedRigidbodies = new List<Rigidbody>();
    private Vector3 lastPosition;
    private float lastDetectionTime;

    private void Start()
    {
        lastPosition = transform.position;
        lastDetectionTime = Time.time;
    }

    private void Update()
    {
        // ลดการตรวจจับเพื่อประสิทธิภาพ
        if (Time.time - lastDetectionTime >= detectionInterval)
        {
            DetectAndMoveRigidbodies();
            lastDetectionTime = Time.time;
        }
        
        MoveTrackedRigidbodies();
        lastPosition = transform.position;
    }

    private void DetectAndMoveRigidbodies()
    {
        if (pivot == null) pivot = transform;

        Collider[] detectedColliders = GetDetectedColliders();
        
        // Remove rigidbodies ที่ออกจาก Detection
        RemoveUntrackedRigidbodies(detectedColliders);
        
        // Add rigidbodies ใหม่ที่เข้ามาใน Detection
        AddNewRigidbodies(detectedColliders);
    }

    private Collider[] GetDetectedColliders()
    {
        Vector3 worldPos = transform.position + detectPosition;
        Quaternion worldRot = pivot.rotation;

        switch (detectionShape)
        {
            case DetectionShape.Box:
                return Physics.OverlapBox(worldPos, boxSize * 0.5f, worldRot, detectionLayerMask);
            
            case DetectionShape.Sphere:
                return Physics.OverlapSphere(worldPos, sphereRadius, detectionLayerMask);
            
            case DetectionShape.Capsule:
                Vector3 point1 = worldPos + pivot.up * Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
                Vector3 point2 = worldPos - pivot.up * Mathf.Max(0, (capsuleHeight * 0.5f) - capsuleRadius);
                return Physics.OverlapCapsule(point1, point2, capsuleRadius, detectionLayerMask);
            
            case DetectionShape.Mesh:
                return new Collider[0]; // Mesh detection ไม่ support ใน built-in Physics
            
            default:
                return new Collider[0];
        }
    }

    private void RemoveUntrackedRigidbodies(Collider[] detectedColliders)
    {
        for (int i = trackedRigidbodies.Count - 1; i >= 0; i--)
        {
            bool stillDetected = false;
            if (detectedColliders != null)
            {
                foreach (var col in detectedColliders)
                {
                    if (col.attachedRigidbody == trackedRigidbodies[i])
                    {
                        stillDetected = true;
                        break;
                    }
                }
            }
            
            if (!stillDetected)
            {
                trackedRigidbodies.RemoveAt(i);
            }
        }
    }

    private void AddNewRigidbodies(Collider[] detectedColliders)
    {
        if (detectedColliders == null) return;

        foreach (var col in detectedColliders)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !trackedRigidbodies.Contains(rb))
            {
                // เช็คว่าเป็น Player หรือ Object ที่ต้องการติดตาม
                if (ShouldTrackRigidbody(rb))
                {
                    trackedRigidbodies.Add(rb);
                }
            }
        }
    }

    private bool ShouldTrackRigidbody(Rigidbody rb)
    {
        // เพิ่มเงื่อนไขกรองที่นี่ เช่น:
        // - ไม่ติดตาม Kinematic Rigidbodies
        // - ไม่ติดตาม Objects ที่มี Tag ที่ไม่ต้องการ
        
        if (rb.isKinematic) return false;
        if (rb.gameObject.CompareTag("NoTrack")) return false;
        
        return true;
    }

    private void MoveTrackedRigidbodies()
    {
        Vector3 delta = transform.position - lastPosition;
        if (delta.sqrMagnitude < 0.0001f) return; // ใช้ sqrMagnitude เพื่อประสิทธิภาพ

        foreach (var rb in trackedRigidbodies)
        {
            if (rb != null) // Safety check
            {
                // MovePosition ทำให้ Rigidbody เคลื่อนที่ตาม PlatformObj
                rb.MovePosition(rb.position + delta);
            }
        }
        
        // ทำความสะอาด null references
        trackedRigidbodies.RemoveAll(rb => rb == null);
    }

    private void OnDrawGizmos()
    {
        if (pivot == null) pivot = transform;

        Gizmos.color = gizmoColor;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(pivot.position, pivot.rotation, Vector3.one);
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

        Gizmos.matrix = Matrix4x4.identity;
    }

    // Debug Info
    [Header("Debug Info (Runtime)")]
    [SerializeField] private bool showDebugInfo = false;
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Platform: {gameObject.name}");
        GUILayout.Label($"Tracked Objects: {trackedRigidbodies.Count}");
        
        foreach (var rb in trackedRigidbodies)
        {
            if (rb != null)
                GUILayout.Label($"- {rb.gameObject.name}");
        }
        
        GUILayout.EndArea();
    }
}