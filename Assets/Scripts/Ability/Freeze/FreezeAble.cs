using System;
using UnityEngine;
using Unity.Netcode;

public abstract class FreezeAble : NetworkBehaviour
{
    [Header("Network State")]
    public NetworkVariable<FreezeAbleState> networkFreezeState = new NetworkVariable<FreezeAbleState>(
        FreezeAbleState.Normal, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public FreezeAbleState freezeAbleState => networkFreezeState.Value;

    [Header("Materials")]
    [SerializeField] private Material highlighMaterial;
    private Material instancedMaterial;

    protected virtual void Start()
    {
        // Subscribe to network state changes
        networkFreezeState.OnValueChanged += OnNetworkFreezeStateChanged;
    }

    private void OnEnable()
    {
        if (highlighMaterial != null)
        {
            instancedMaterial = new Material(highlighMaterial);
            GetComponent<Renderer>().material = instancedMaterial;
            ToggleHighlight(instancedMaterial, false);
        }
    }

    /// <summary>
    /// เมื่อสถานะ Network เปลี่ยน
    /// </summary>
    void OnNetworkFreezeStateChanged(FreezeAbleState oldState, FreezeAbleState newState)
    {
        Debug.Log($"Network State Changed: {oldState} -> {newState} on {gameObject.name}");
        
        if (newState == FreezeAbleState.Freezing)
        {
            OnFreeze();
        }
        else if (newState == FreezeAbleState.Normal)
        {
            StopFreeze();
        }
    }

    /// <summary>
    /// เรียกจาก Server เพื่อ Freeze Object
    /// </summary>
    public void NetworkFreeze()
    {
        if (IsServer)
        {
            networkFreezeState.Value = FreezeAbleState.Freezing;
            Debug.Log($"Server: Freezing {gameObject.name}");
        }
    }

    /// <summary>
    /// เรียกจาก Server เพื่อ Unfreeze Object
    /// </summary>
    public void NetworkUnfreeze()
    {
        if (IsServer)
        {
            networkFreezeState.Value = FreezeAbleState.Normal;
            Debug.Log($"Server: Unfreezing {gameObject.name}");
        }
    }

    /// <summary>
    /// Toggle Highlight Material เรียก Emission
    /// </summary>
    public void ToggleHighlight(Material instancedMaterial, bool show)
    {
        if (instancedMaterial == null) return;
        
        if (show)
        {
            instancedMaterial.EnableKeyword("_EMISSION");
            instancedMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            instancedMaterial.DisableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// ตอน Freeze (เรียกจาก Network State Change)
    /// </summary>
    public virtual void OnFreeze()
    {
        Debug.Log($"แช่ Obj: {gameObject.name} [Network Synced]");
    }

    /// <summary>
    /// ตอนหยุด (เรียกจาก Network State Change)
    /// </summary>
    public virtual void StopFreeze()
    {
        Debug.Log($"หยุดแช่ Obj: {gameObject.name} [Network Synced]");
    }

    public override void OnDestroy()
    {
        if (networkFreezeState != null)
            networkFreezeState.OnValueChanged -= OnNetworkFreezeStateChanged;
        
        if (instancedMaterial != null)
            Destroy(instancedMaterial);
            
        base.OnDestroy();
    }

    private void OnDisable()
    {
        if (instancedMaterial != null)
            Destroy(instancedMaterial);
    }
}

[Serializable]
public enum FreezeAbleState
{
    Normal,
    Freezing,
}