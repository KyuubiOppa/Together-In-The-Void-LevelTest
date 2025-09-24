using System;
using UnityEngine;

public abstract class FixAble : MonoBehaviour
{
    public FixAbleState fixAbleState;

    [Header("Materials")]
    [SerializeField] private Material highlightMaterial;
    [Header("ใส่ MeshRenderer ของ Object ที่ต้องการเปลี่ยน Material")]
    [SerializeField] private MeshRenderer[] targetRenderers;

    // เก็บ material instance ของแต่ละ renderer
    private Material[][] instancedMaterials;

    private void OnEnable()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            instancedMaterials = new Material[targetRenderers.Length][];

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] != null)
                {
                    int matCount = targetRenderers[i].materials.Length;
                    instancedMaterials[i] = new Material[matCount];

                    for (int j = 0; j < matCount; j++)
                    {
                        instancedMaterials[i][j] = new Material(highlightMaterial);
                    }

                    targetRenderers[i].materials = instancedMaterials[i];

                    // ปิด highlight เริ่มต้น
                    ToggleHighlight(targetRenderers[i], false);
                }
            }
        }
    }

    // Toggle highlight ของ MeshRenderer ตัวเดียว
    public void ToggleHighlight(MeshRenderer renderer, bool show)
    {
        if (renderer == null) return;

        foreach (var mat in renderer.materials)
        {
            if (mat == null) continue;

            if (show)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    // Toggle highlight ของทุก MeshRenderer
    public void ToggleHighlightAll(bool show)
    {
        if (targetRenderers == null) return;

        foreach (var renderer in targetRenderers)
        {
            ToggleHighlight(renderer, show);
        }
    }

    public virtual void OnFix()
    {
        Debug.Log("ซ่อม Obj");
    }

    public virtual void StopFix()
    {
        Debug.Log("พัง Obj");
    }

    private void OnDestroy()
    {
        if (instancedMaterials != null)
        {
            foreach (var matArray in instancedMaterials)
            {
                if (matArray == null) continue;

                foreach (var mat in matArray)
                {
                    if (mat != null)
                        Destroy(mat);
                }
            }
        }
    }

    private void OnDisable()
    {
        OnDestroy();
    }
}

[Serializable]
public enum FixAbleState
{
    Break,
    Fixing,
}


[Serializable]
public class FixObjects
{
    public Transform objToFix;
    public MoveObjectPoint moveObjectPoint;
    public StartObjectPoint startObjectPoint;
}


[Serializable]
public class MoveObjectPoint
{
    public Transform objToMove;
    [HideInInspector] public Vector3 objToMovePos;
    [HideInInspector] public Quaternion objToMoveRotation;
}
[Serializable]
public class StartObjectPoint
{
    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public Quaternion rotationStart;
}