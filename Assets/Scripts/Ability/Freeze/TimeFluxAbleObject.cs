using UnityEngine;
using DG.Tweening;

public class TimeFluxAbleObject : FreezeAble
{
    public override void OnFreeze()
    {
        // ไม่ต้องเช็คหรือแก้ไข freezeAbleState เพราะมันจะถูกจัดการโดย NetworkVariable แล้ว
        Debug.Log($"Freezing TimeFlux Object: {gameObject.name}");
        DOTween.Pause(gameObject); // หยุด Tween บนทุก Client
    }

    public override void StopFreeze()
    {
        // ไม่ต้องเช็คหรือแก้ไข freezeAbleState เพราะมันจะถูกจัดการโดย NetworkVariable แล้ว
        Debug.Log($"Unfreezing TimeFlux Object: {gameObject.name}");
        DOTween.Play(gameObject); // เล่นต่อ Tween บนทุก Client
    }
}