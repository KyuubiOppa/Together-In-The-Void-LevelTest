using UnityEngine;
using DG.Tweening;
public class TimeFluxAbleObject : FreezeAble
{
    public override void OnFreeze()
    {
        if (freezeAbleState == FreezeAbleState.Freezing) return;
        freezeAbleState = FreezeAbleState.Freezing;
        DOTween.Pause(gameObject); // หยุด Tween
    }

    public override void StopFreeze()
    {
        if (freezeAbleState == FreezeAbleState.Normal) return;
        freezeAbleState = FreezeAbleState.Normal;
        DOTween.Play(gameObject); // เล่นต่อ Tween ต่อที่ Pause ไว้
    }
}
