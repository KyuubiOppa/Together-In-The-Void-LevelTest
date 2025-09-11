using System;
using UnityEngine;

public abstract class FreezeAble : MonoBehaviour
{
    public FreezeAbleState freezeAbleState;

    [Header("Materials")]
    [SerializeField] private Material highlighMaterial;
    private Material instancedMaterial;

    private void OnEnable()
    {
        instancedMaterial = new Material(highlighMaterial);
        GetComponent<Renderer>().material = instancedMaterial;
    }

    /// <summary>
    /// Toggle Highlight Material เรียก Emission
    /// </summary>
    /// <param name="instancedMaterial"></param>
    /// <param name="show"></param>
    public void ToggleHighlight(Material instancedMaterial, bool show)
    {
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
    /// ตอน Freeze
    /// </summary>
    public virtual void OnFreeze()
    {
        Debug.Log("แช่ Obj");
    }

    /// <summary>
    /// ตอนหยุด
    /// </summary>
    public virtual void StopFreeze()
    {
        Debug.Log("หยุดแช่ Obj");
    }

    private void OnDestroy()
    {
        Destroy(instancedMaterial);
    }

    private void OnDisable()
    {
        Destroy(instancedMaterial);
    }
}
[Serializable]
public enum FreezeAbleState
{
    Normal,
    Freezing,
}