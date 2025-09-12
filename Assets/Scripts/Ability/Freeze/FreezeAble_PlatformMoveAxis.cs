using UnityEngine;
using DG.Tweening;

public class FreezeAble_PlatformMoveAxis : FreezeAble
{
    public enum Axis { X, Y, Z }

    [Header("Platform Movement Settings")]
    [SerializeField] private Axis moveAxis = Axis.Y;
    [SerializeField] private float moveAmount = 3f;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private Vector3 startPosition;
    private Vector3 endPosition;

    private void Awake()
    {
        startPosition = transform.position;
        CalculateEndPosition();
    }

    private void Start()
    {
        if (freezeAbleState == FreezeAbleState.Normal)
        {
            StartMoveLooping();
        }
    }

    private void CalculateEndPosition()
    {
        endPosition = startPosition;
        switch (moveAxis)
        {
            case Axis.X: endPosition.x += moveAmount; break;
            case Axis.Y: endPosition.y += moveAmount; break;
            case Axis.Z: endPosition.z += moveAmount; break;
        }
    }

    private void StartMoveLooping()
    {
        // สร้างลูปการเคลื่อนที่ (ไม่ต้องเก็บตัวแปร Tween แล้ว)
        DOTween.Sequence()
            .Append(transform.DOMove(endPosition, moveDuration).SetEase(moveEase))
            .Append(transform.DOMove(startPosition, moveDuration).SetEase(moveEase))
            .SetLoops(-1, LoopType.Restart)
            .SetId(this);
    }

    public override void OnFreeze()
    {
        if (freezeAbleState == FreezeAbleState.Freezing) return;
        freezeAbleState = FreezeAbleState.Freezing;
        DOTween.Pause(this); // หยุด Tween
    }

    public override void StopFreeze()
    {
        if (freezeAbleState == FreezeAbleState.Normal) return;
        freezeAbleState = FreezeAbleState.Normal;
        DOTween.Play(this); // เล่นต่อ Tween ต่อที่ Pause ไว้
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
    }

    private void OnEnable()
    {
        DOTween.Kill(this);
        if (freezeAbleState == FreezeAbleState.Normal)
        {
            DOVirtual.DelayedCall(0.1f, () =>
            {
                if (this != null && gameObject.activeInHierarchy)
                    DOTween.Play(this);
            });
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Vector3 end = start;

        switch (moveAxis)
        {
            case Axis.X: end.x += moveAmount; break;
            case Axis.Y: end.y += moveAmount; break;
            case Axis.Z: end.z += moveAmount; break;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(start, 0.2f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(end, 0.2f);

        Gizmos.color = freezeAbleState == FreezeAbleState.Freezing ? Color.cyan : Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
    }
}
