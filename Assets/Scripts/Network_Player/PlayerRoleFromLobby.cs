using Unity.Netcode;
using UnityEngine;

public class PlayerRoleFromLobby : NetworkBehaviour
{
    [Header("��˹����ҷ�����Ţ� Inspector")]
    [Tooltip("0 = Player A (Time Stopper), 1 = Player B (WallWaker)")]
    [Range(0, 1)]
    [SerializeField] private int roleIndex = 1;

    // Server �繤���˹���� ���Ƿء client ��ҹ������ǡѹ
    public NetworkVariable<bool> IsTimeStopper =
        new(writePerm: NetworkVariableWritePermission.Server);

    // helper ������ҡ�礽���Ť��
    public bool IsWallWakerLocal => roleIndex == 1;

    public override void OnNetworkSpawn()
    {
        // ��� Server �絤������ ���� sync 价ء����ͧ
        if (IsServer)
        {
            IsTimeStopper.Value = (roleIndex == 0); // 0 = A = TimeStopper
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // �ѹ�����ش��ǧ�� inspector
        roleIndex = Mathf.Clamp(roleIndex, 0, 1);
    }
#endif
}