using UnityEngine;
using DG.Tweening;
using NUnit.Framework;

public class TimeFluxAbleObjectTemp : FreezeAble
{
    [SerializeField] private float freezeDuration = 2f;
    private float freezeTimer = 0f;
    private bool isFrozen = false;

    void Update()
    {
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            DOTween.Pause(gameObject); // หยุด Tween บนทุก Client
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
                StopFreeze();
                if (IsServer)
                    NetworkUnfreeze();
                Debug.Log($"Server: Unfreezing {gameObject.name} after duration");
            }
        }
    }

    public override void OnFreeze()
    {
        if (isFrozen) return;
        freezeTimer = freezeDuration;
        isFrozen = true;
        // ไม่ต้องเช็คหรือแก้ไข freezeAbleState เพราะมันจะถูกจัดการโดย NetworkVariable แล้ว
        Debug.Log($"Freezing TimeFlux Object: {gameObject.name}");
    }

    public override void StopFreeze()
    {
        if (isFrozen) return;
        freezeTimer = 0f;
        DOTween.Play(gameObject); // เล่นต่อ Tween บนทุก Client
        // ไม่ต้องเช็คหรือแก้ไข freezeAbleState เพราะมันจะถูกจัดการโดย NetworkVariable แล้ว
        Debug.Log($"Unfreezing TimeFlux Object: {gameObject.name}");
    }
}