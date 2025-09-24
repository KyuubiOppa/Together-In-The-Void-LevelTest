// RespawnZone.cs
using UnityEngine;

public class RespawnZone : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayerMask;

    private void OnTriggerEnter(Collider other)
    {
        // เช็คว่าตัวที่ชนอยู่ใน LayerMask หรือไม่
        if (((1 << other.gameObject.layer) & playerLayerMask) == 0)
            return; // ถ้าไม่ใช่ Layer ที่กำหนด ก็ออก

        PlayerRespawn player = other.GetComponent<PlayerRespawn>();
        if (player != null)
        {
            player.Respawn(); // วาร์ปกลับไปยัง respawnPointPlayer ของตัวเอง
        }
    }
}
