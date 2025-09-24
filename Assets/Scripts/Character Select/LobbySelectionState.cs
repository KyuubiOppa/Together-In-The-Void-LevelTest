using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class LobbySelectionState : NetworkBehaviour
{
    public static LobbySelectionState I { get; private set; }

    // ใช้โชว์ JoinCode ได้ทันที ไม่ต้องรอ NetworkVar
    public static string CachedJoinCode = "";

    public NetworkVariable<ulong> slot0Owner = new(ulong.MaxValue);
    public NetworkVariable<ulong> slot1Owner = new(ulong.MaxValue);

    public NetworkVariable<byte> hostSlot = new(255);
    public NetworkVariable<byte> clientSlot = new(255);

    public NetworkVariable<bool> hostReady = new(false);
    public NetworkVariable<bool> clientReady = new(false);

    public NetworkVariable<bool> gameStarted = new(false);

    // JoinCode sync: ทุกคนอ่านได้ เขียนได้เฉพาะ Host
    public NetworkVariable<FixedString32Bytes> joinCode =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        if (transform.parent != null)
        {
            Debug.LogError("[LobbySelectionState] ต้องเป็น Root GameObject เท่านั้น (อย่าเป็นลูกของใคร).");
        }
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        var no = GetComponent<NetworkObject>();
        if (no && !no.IsSpawned)
        {
            no.Spawn(true);
            Debug.Log("[LobbyState] Spawned NetworkObject.");
        }

        if (!string.IsNullOrEmpty(CachedJoinCode))
        {
            joinCode.Value = CachedJoinCode;
            Debug.Log($"[LobbyState] Applied CachedJoinCode to NetworkVar: {joinCode.Value}");
        }
    }

    public bool IsTaken(int slot, out ulong owner)
    {
        owner = slot == 0 ? slot0Owner.Value : slot1Owner.Value;
        return owner != ulong.MaxValue;
    }

    public void ClearReadyOf(ulong cid)
    {
        if (cid == NetworkManager.ServerClientId) hostReady.Value = false;
        else clientReady.Value = false;
    }

    public void SetReady(ulong cid, bool v)
    {
        if (cid == NetworkManager.ServerClientId) hostReady.Value = v;
        else clientReady.Value = v;
    }

    // ✅ ไม่ใช้ slot0Owner/slot1Owner แล้ว → ใช้ hostSlot / clientSlot แทน
    public bool EveryoneReady()
    {
        bool hostPicked = hostSlot.Value != 255;
        bool clientPicked = clientSlot.Value != 255;
        bool ready = hostPicked && clientPicked && hostReady.Value && clientReady.Value;

        Debug.Log($"[LobbyState] EveryoneReady? hostPicked={hostPicked}, clientPicked={clientPicked}, hostReady={hostReady.Value}, clientReady={clientReady.Value} => {ready}");
        return ready;
    }
}
