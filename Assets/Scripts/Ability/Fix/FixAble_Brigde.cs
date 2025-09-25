using UnityEngine;
using DG.Tweening;
using Unity.Netcode;

public class FixAble_Brigde : FixAble
{
    [SerializeField] private FixObjects[] fixObjects;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 1f; // ระยะเวลาในการขยับ

    void Start()
    {
        // เก็บตำแหน่งและการหมุนเริ่มต้น
        foreach (var fo in fixObjects)
        {
            if (fo.objToFix != null)
            {
                if (fo.startObjectPoint == null) fo.startObjectPoint = new StartObjectPoint();
                fo.startObjectPoint.startPos = fo.objToFix.position;
                fo.startObjectPoint.rotationStart = fo.objToFix.rotation;
            }

            if (fo.moveObjectPoint != null && fo.moveObjectPoint.objToMove != null)
            {
                fo.moveObjectPoint.objToMovePos = fo.moveObjectPoint.objToMove.position;
                fo.moveObjectPoint.objToMoveRotation = fo.moveObjectPoint.objToMove.rotation;
            }
        }
    }

    public override void OnFix()
    {
        foreach (var fo in fixObjects)
        {
            if (fo.objToFix == null || fo.moveObjectPoint == null || fo.moveObjectPoint.objToMove == null) continue;

            Vector3 targetPos = fo.moveObjectPoint.objToMovePos;
            Quaternion targetRot = fo.moveObjectPoint.objToMoveRotation;

            // ปิด Collider ก่อนเริ่มขยับ
            SetCollidersEnabled(fo.objToFix, false);

            Sequence animSequence = DOTween.Sequence();
            animSequence.Join(fo.objToFix.DOMove(targetPos, moveDuration).SetEase(Ease.InOutSine));
            animSequence.Join(fo.objToFix.DORotateQuaternion(targetRot, moveDuration).SetEase(Ease.InOutSine));
            
            // เปิด Collider หลังจากเสร็จสิ้นการขยับ
            animSequence.OnComplete(() => {
                SetCollidersEnabled(fo.objToFix, true);
            });
            
            animSequence.Play();
        }
    }

    public override void StopFix()
    {
        foreach (var fo in fixObjects)
        {
            if (fo.objToFix == null || fo.startObjectPoint == null) continue;

            Vector3 targetPos = fo.startObjectPoint.startPos;
            Quaternion targetRot = fo.startObjectPoint.rotationStart;

            // ปิด Collider ก่อนเริ่มขยับ
            SetCollidersEnabled(fo.objToFix, false);

            Sequence animSequence = DOTween.Sequence();
            animSequence.Join(fo.objToFix.DOMove(targetPos, moveDuration).SetEase(Ease.InOutSine));
            animSequence.Join(fo.objToFix.DORotateQuaternion(targetRot, moveDuration).SetEase(Ease.InOutSine));
            
            // เปิด Collider หลังจากเสร็จสิ้นการขยับ
            animSequence.OnComplete(() => {
                SetCollidersEnabled(fo.objToFix, true);
            });
            
            animSequence.Play();
        }
    }

    /// <summary>
    /// ปิด/เปิด Collider ทั้งหมดใน GameObject และ children
    /// </summary>
    /// <param name="obj">GameObject ที่ต้องการจัดการ Collider</param>
    /// <param name="enabled">true = เปิด, false = ปิด</param>
    private void SetCollidersEnabled(Transform obj, bool enabled)
    {
        if (obj == null) return;

        // จัดการ Collider ใน GameObject หลัก
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }
}