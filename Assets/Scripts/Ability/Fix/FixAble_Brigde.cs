using UnityEngine;
using DG.Tweening;

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

            Sequence animSequence = DOTween.Sequence();
            animSequence.Join(fo.objToFix.DOMove(targetPos, moveDuration).SetEase(Ease.InOutSine));
            animSequence.Join(fo.objToFix.DORotateQuaternion(targetRot, moveDuration).SetEase(Ease.InOutSine));
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

            Sequence animSequence = DOTween.Sequence();
            animSequence.Join(fo.objToFix.DOMove(targetPos, moveDuration).SetEase(Ease.InOutSine));
            animSequence.Join(fo.objToFix.DORotateQuaternion(targetRot, moveDuration).SetEase(Ease.InOutSine));
            animSequence.Play();
        }
    }
}
