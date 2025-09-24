// RespawnZone.cs
using UnityEngine;

public class RespawnZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerRespawn player = other.GetComponent<PlayerRespawn>();
        if (player != null)
        {
            player.Respawn(); // วาร์ปกลับไปยัง respawnPointPlayer ของตัวเอง
        }
    }
}
