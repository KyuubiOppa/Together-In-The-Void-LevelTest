// PlayerRespawn.cs
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform respawnPointPlayer;

    // เมื่อชนกับ Ground
    private void OnCollisionEnter(Collision collision)
    {
        Respawn_Ground ground = collision.collider.GetComponent<Respawn_Ground>();
        if (ground != null)
        {
            respawnPointPlayer = ground.respawnPoint;
        }
    }

    // ฟังก์ชันวาร์ปกลับไปยัง respawnPointPlayer
    public void Respawn()
    {
        if (respawnPointPlayer != null)
        {
            transform.position = respawnPointPlayer.position;
        }
    }
}
