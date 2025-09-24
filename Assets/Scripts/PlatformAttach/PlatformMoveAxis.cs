using UnityEngine;
using DG.Tweening;
using Unity.Netcode;

[RequireComponent(typeof(PlatformObj))]
public class PlatformMoveAxis : NetworkBehaviour
{
    public enum Axis { X, Y, Z }
    public enum Direction { Positive, Negative }

    [Header("Platform Movement Settings")]
    [SerializeField] private Axis moveAxis = Axis.X;
    [SerializeField] private Direction direction = Direction.Positive;
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [SerializeField, Min(0f)] private float moveAmount = 3f;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private bool useLocalSpace = true;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    // Network synced position
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

    Vector3 startPosLocal, startPosWorld;
    Vector3 endPos;
    Sequence seq;

    void Awake()
    {
        startPosLocal = transform.localPosition;
        startPosWorld = transform.position;
        RecalculateEnd();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server เป็นผู้ควบคุมการเคลื่อนที่
            StartMoveLoop();
            networkPosition.Value = transform.position;
        }
        else
        {
            // Client ติดตามตำแหน่งจาก Server
            networkPosition.OnValueChanged += OnNetworkPositionChanged;
            // Set initial position
            transform.position = networkPosition.Value;
        }
    }

    void OnNetworkPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        // Smooth interpolation สำหรับ Client
        if (!IsServer)
        {
            transform.DOMove(newPos, 0.1f).SetEase(Ease.Linear);
        }
    }

    void Update()
    {
        // เฉพาะ Server ที่อัปเดตตำแหน่ง Network
        if (IsServer && networkPosition.Value != transform.position)
        {
            networkPosition.Value = transform.position;
        }
    }

    void RecalculateEnd()
    {
        Vector3 axis = moveAxis == Axis.X ? Vector3.right :
                       moveAxis == Axis.Y ? Vector3.up : Vector3.forward;

        float dir = (direction == Direction.Positive) ? 1f : -1f;

        if (useLocalSpace)
            endPos = startPosLocal + axis * (moveAmount * dir);
        else
            endPos = startPosWorld + axis * (moveAmount * dir);
    }

    void StartMoveLoop()
    {
        if (!IsServer) return; // เฉพาะ Server เท่านั้น

        seq?.Kill();
        seq = DOTween.Sequence()
             .SetId(gameObject)
             .SetTarget(transform)
             .SetLoops(-1, LoopType.Restart);

        if (useLocalSpace)
        {
            transform.localPosition = startPosLocal;
            seq.Append(transform.DOLocalMove(endPos, moveDuration).SetEase(moveEase));
            seq.Append(transform.DOLocalMove(startPosLocal, moveDuration).SetEase(moveEase));
        }
        else
        {
            transform.position = startPosWorld;
            seq.Append(transform.DOMove(endPos, moveDuration).SetEase(moveEase));
            seq.Append(transform.DOMove(startPosWorld, moveDuration).SetEase(moveEase));
        }
    }

    public override void OnNetworkDespawn()
    {
        seq?.Kill();
        networkPosition.OnValueChanged -= OnNetworkPositionChanged;
    }

    void OnDisable() 
    { 
        seq?.Kill(); 
    }

    void OnDestroy() 
    { 
        seq?.Kill(); 
    }

    void OnValidate()
    {
        if (Application.isPlaying) return;
        startPosLocal = transform.localPosition;
        startPosWorld = transform.position;
        RecalculateEnd();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 start = useLocalSpace
            ? (Application.isPlaying ? startPosLocal : transform.localPosition)
            : (Application.isPlaying ? startPosWorld : transform.position);

        Gizmos.color = Color.green; 
        Gizmos.DrawLine(start, endPos);
        Gizmos.color = Color.cyan; 
        Gizmos.DrawWireSphere(start, 0.15f);
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(endPos, 0.15f);
    }
}