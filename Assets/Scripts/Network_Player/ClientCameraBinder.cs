using Unity.Netcode;
using UnityEngine;
using Cinemachine;

public class ClientCameraBinder : NetworkBehaviour
{
    [SerializeField] Transform cameraTarget; // ใส่ PlayerCameraRoot

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; // ให้เฉพาะตัวที่เราเป็นเจ้าของ

        var vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (!vcam)
        {
            Debug.LogError("No CinemachineVirtualCamera in scene.");
            return;
        }

        vcam.Follow = cameraTarget;
        vcam.LookAt = cameraTarget; // ไม่อยากล็อกการมอง เอาออกได้
        vcam.Priority = 100;        // ให้ชนะ vcam อื่น ๆ ถ้ามี
    }
}
