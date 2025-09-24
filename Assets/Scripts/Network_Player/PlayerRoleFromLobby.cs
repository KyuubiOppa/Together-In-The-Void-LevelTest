using Unity.Netcode;
using UnityEngine;

public class PlayerRoleFromLobby : NetworkBehaviour
{
    [Header("กำหนดบทบาทด้วยเลขใน Inspector")]
    [Tooltip("0 = Player A (Time Stopper), 1 = Player B (WallWaker)")]
    [Range(0, 1)]
    [SerializeField] private int roleIndex = 1;

    // Server เป็นคนกำหนดค่า แล้วทุก client อ่านค่าเดียวกัน
    public NetworkVariable<bool> IsTimeStopper =
        new(writePerm: NetworkVariableWritePermission.Server);

    // helper เผื่ออยากเช็คฝั่งโลคัล
    public bool IsWallWakerLocal => roleIndex == 1;

    public override void OnNetworkSpawn()
    {
        // ให้ Server เซ็ตค่าเดียว แล้ว sync ไปทุกเครื่อง
        if (IsServer)
        {
            IsTimeStopper.Value = (roleIndex == 0); // 0 = A = TimeStopper
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // กันค่าหลุดช่วงแก้ inspector
        roleIndex = Mathf.Clamp(roleIndex, 0, 1);
    }
#endif
}