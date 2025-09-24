using Unity.Netcode;
using UnityEngine;

public class GamePlayerSpawnerSameScene : NetworkBehaviour
{
    [Header("Prefabs for slot 0 / slot 1")]
    [SerializeField] private GameObject prefabSlot0;  // ตัวละคร A
    [SerializeField] private GameObject prefabSlot1;  // ตัวละคร B

    [Header("Spawn Points for slot 0 / slot 1")]
    [SerializeField] private Transform spawn0;
    [SerializeField] private Transform spawn1;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
            Debug.Log("[Spawner] Subscribed to OnLoadComplete");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
    }

    private void OnSceneLoaded(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[Spawner] OnSceneLoaded -> scene={sceneName}, fired for client={clientId}");

        if (!IsServer) return;

        var S = LobbySelectionState.I;
        if (S == null)
        {
            Debug.LogError("[Spawner] LobbySelectionState missing!");
            return;
        }

        Debug.Log($"[Spawner] LobbySelectionState -> hostSlot={S.hostSlot.Value}, clientSlot={S.clientSlot.Value}");

        // ✅ spawn ให้ทุก client ที่เชื่อมต่อ ไม่ใช่เฉพาะ clientId เดียว
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnForClient(c.ClientId, S);
        }
    }

    private void SpawnForClient(ulong clientId, LobbySelectionState S)
    {
        int slot = (clientId == NetworkManager.ServerClientId) ? S.hostSlot.Value : S.clientSlot.Value;
        Debug.Log($"[Spawner] SpawnForClient -> clientId={clientId}, slot={slot}");

        if (slot == 255)
        {
            Debug.LogError($"[Spawner] Client {clientId} has no slot selected!");
            return;
        }

        GameObject prefab = (slot == 0) ? prefabSlot0 : prefabSlot1;
        Transform spawnPoint = (slot == 0) ? spawn0 : spawn1;

        if (prefab == null || spawnPoint == null)
        {
            Debug.LogError($"[Spawner] Missing prefab or spawn point for slot {slot}");
            return;
        }

        var obj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var netObj = obj.GetComponent<NetworkObject>();
        if (!netObj)
        {
            Debug.LogError($"[Spawner] Prefab {prefab.name} missing NetworkObject!");
            return;
        }

        netObj.SpawnAsPlayerObject(clientId);
        Debug.Log($"[Spawner] Spawned {prefab.name} for client {clientId} on slot {slot}");
    }
}
