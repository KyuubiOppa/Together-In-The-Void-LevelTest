using Cinemachine;
using UnityEngine;
using Unity.Netcode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Ability_Fix : NetworkBehaviour
{
    [Header("Debug")]
    [SerializeField] private FixAble found_FixAbleObj; // FixAble ที่เจอ
    [Header("Refs")]
    [SerializeField] CinemachineVirtualCamera vcam;  
    [SerializeField] private Canvas crosshairCanvas;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator animator;
    
    [Header("Aim Settings")]
    [SerializeField] private float aimMaxDistance = 25f;
    [SerializeField] private Vector3 shoulderOffsetWhenAiming = new Vector3(3f, 0.5f, 0f);
    [SerializeField] private float shoulderLerp = 10f;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask fixAbleLayerMask = -1;
    [SerializeField] private bool showDebugRay = true; // แสดง Debug Ray ใน Scene View
    [Header("Animator Fix Layer")]
    [SerializeField] private string fixLayerName = "FixAbility"; 
    [SerializeField] private float fixLayerLerpSpeed = 5f;
    private int fixLayerIndex;
    private float targetFixWeight = 0f;
    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private Vector3 originalShoulderOffset;
    private bool isAiming = false;
    private FixAble currentTargetedObj;
    
#if ENABLE_INPUT_SYSTEM
    private InputAction aimAction;
    private InputAction fireAction;
#endif

    void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        var pi = GetComponent<PlayerInput>();
        if (pi)
        {
            aimAction = pi.actions["Aim"];   // Input Action สำหรับ Right Mouse (Hold)
            fireAction = pi.actions["Fire"];  // Input Action สำหรับ Left Mouse (Press)
        }
#endif
        if (crosshairCanvas) crosshairCanvas.gameObject.SetActive(false);

        // Get the 3rd Person Follow component from vcam
        if (vcam != null)
        {
            thirdPersonFollow = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (thirdPersonFollow != null)
            {
                originalShoulderOffset = thirdPersonFollow.ShoulderOffset;
            }
        }
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        if (animator != null)
            fixLayerIndex = animator.GetLayerIndex(fixLayerName);
    }

    void Update()
    {
        // เฉพาะ owner เท่านั้นที่จะควบคุม input
        if (!IsOwner) return;
        
        HandleAiming();
        UpdateAimTarget();

        if (animator != null && fixLayerIndex >= 0)
        {
            float currentWeight = animator.GetLayerWeight(fixLayerIndex);
            float newWeight = Mathf.Lerp(currentWeight, targetFixWeight, Time.deltaTime * fixLayerLerpSpeed);
            animator.SetLayerWeight(fixLayerIndex, newWeight);
        }
    }

#region Methods คลิกซ้าย
    /// <summary>
    /// แช่/หยุดแช่ 
    /// </summary>
    public void FireFix()
    {
        if (found_FixAbleObj != null && isAiming && IsOwner)
        {
            // ส่ง RPC ไปยัง server เพื่อให้ทุกคนเห็น
            ulong fixAbleNetworkId = found_FixAbleObj.GetComponent<NetworkObject>().NetworkObjectId;
            bool shouldFix = found_FixAbleObj.fixAbleState == FixAbleState.Break;
            
            RequestFixActionServerRpc(fixAbleNetworkId, shouldFix);
            
            // เรียก StartMagic ในตัวเองทันที (สำหรับ local player)
            StartMagic();
        }
    }
    
    [ServerRpc(RequireOwnership = true)]
    void RequestFixActionServerRpc(ulong fixAbleNetworkId, bool shouldFix)
    {
        // Server ประมวลผลและส่งต่อให้ทุก client
        ExecuteFixActionClientRpc(fixAbleNetworkId, shouldFix);
    }
    
    [ClientRpc]
    void ExecuteFixActionClientRpc(ulong fixAbleNetworkId, bool shouldFix)
    {
        // หา FixAble object จาก NetworkObjectId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(fixAbleNetworkId, out NetworkObject networkObj))
        {
            FixAble fixAbleObj = networkObj.GetComponent<FixAble>();
            if (fixAbleObj != null)
            {
                if (shouldFix)
                {
                    fixAbleObj.OnFix();
                    fixAbleObj.fixAbleState = FixAbleState.Fixing;
                }
                else
                {
                    fixAbleObj.StopFix();
                    fixAbleObj.fixAbleState = FixAbleState.Break;
                }
            }
        }
    }
    
    void StartMagic()
    {
        if (animator != null && fixLayerIndex >= 0)
        {
            animator.SetLayerWeight(fixLayerIndex, 1f);
            animator.SetTrigger("Fix");
        }
    }
#endregion

    #region Methods คลิกขวา
    void HandleAiming()
    {
#if ENABLE_INPUT_SYSTEM
        if (aimAction != null)
        {
            bool currentlyAiming = aimAction.IsPressed();
            if (currentlyAiming != isAiming)
            {
                isAiming = currentlyAiming;   
                if (isAiming)
                    StartAiming();
                else
                    StopAiming();
            }
            // ปรับ Offset กล้อง
            if (thirdPersonFollow != null)
            {
                Vector3 targetOffset = isAiming ? shoulderOffsetWhenAiming : originalShoulderOffset;
                thirdPersonFollow.ShoulderOffset = Vector3.Lerp(
                    thirdPersonFollow.ShoulderOffset, 
                    targetOffset, 
                    shoulderLerp * Time.deltaTime
                );
            }
        }
#endif
    }

    void StartAiming()
    {
        // เปิด Crosshair
        if (crosshairCanvas)
            crosshairCanvas.gameObject.SetActive(true);
            
        targetFixWeight = 1f;
    }

    void StopAiming()
    {
        // ปิด Crosshair
        if (crosshairCanvas)
            crosshairCanvas.gameObject.SetActive(false);

        if (currentTargetedObj != null)
        {
            ToggleObjectHighlight(currentTargetedObj, false);
            currentTargetedObj = null;
            found_FixAbleObj = null;
        }
        
        targetFixWeight = 0f;
    }
    
    void UpdateAimTarget()
    {
        if (!isAiming || playerCamera == null) 
            return;

        // สร้าง Ray จากจุดกลางหน้าจอ
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        // แสดง Debug Ray ใน Scene View
        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * aimMaxDistance, Color.red, 0.1f);
        
        RaycastHit hit;
        FixAble detectedFreezeAble = null;
        // Raycast หา FixAble objects
        if (Physics.Raycast(ray, out hit, aimMaxDistance, fixAbleLayerMask))
        {
            detectedFreezeAble = hit.collider.GetComponent<FixAble>();
            if (detectedFreezeAble == null)
                detectedFreezeAble = hit.collider.GetComponentInParent<FixAble>();
        }
        // จัดการ Highlight
        HandleTargetHighlight(detectedFreezeAble);
    }
#endregion  

#region Show Highlight Obj
    void HandleTargetHighlight(FixAble newTarget)
    {
        if (currentTargetedObj != newTarget) // ถ้า target เปลี่ยน
        {
            // ปิด highlight ของ object เก่า
            if (currentTargetedObj != null)
                ToggleObjectHighlight(currentTargetedObj, false);

            // เปิด highlight ของ object ใหม่
            if (newTarget != null)
                ToggleObjectHighlight(newTarget, true);

            currentTargetedObj = newTarget;
            found_FixAbleObj = newTarget;
        }
    }
    
    void ToggleObjectHighlight(FixAble fixAbleObj, bool show)
    {
        if (fixAbleObj != null)
        {
            // เรียก ToggleHighlightAll แทนการเลือก material ตัวเดียว
            fixAbleObj.ToggleHighlightAll(show);
        }
    }
#endregion
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
#if ENABLE_INPUT_SYSTEM
        // เฉพาะ owner เท่านั้นที่จะลง subscribe input events
        if (IsOwner && fireAction != null)
        {
            fireAction.performed += OnFirePerformed;
        }
#endif
    }
    
    public override void OnNetworkDespawn()
    {
#if ENABLE_INPUT_SYSTEM
        if (IsOwner && fireAction != null)
        {
            fireAction.performed -= OnFirePerformed;
        }
#endif
        
        base.OnNetworkDespawn();
    }
    
#if ENABLE_INPUT_SYSTEM
    void OnFirePerformed(InputAction.CallbackContext context)
    {
        FireFix();
    }
#endif
}