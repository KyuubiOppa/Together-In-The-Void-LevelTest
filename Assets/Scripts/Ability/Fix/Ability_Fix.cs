using Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Ability_Fix : MonoBehaviour
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
            
        fixLayerIndex = animator.GetLayerIndex(fixLayerName);
    }

    void Update()
    {
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
    public void FireFreeze()
    {
        if (found_FixAbleObj != null && isAiming)
        {
            StartMagic();
            // ตรวจสอบสถานะปัจจุบันและสลับ
            if (found_FixAbleObj.fixAbleState == FixAbleState.Break)
            {
                found_FixAbleObj.OnFix();
                found_FixAbleObj.fixAbleState = FixAbleState.Fixing;
            }
            else if (found_FixAbleObj.fixAbleState == FixAbleState.Fixing)
            {
                found_FixAbleObj.StopFix();
                found_FixAbleObj.fixAbleState = FixAbleState.Break;
            }
        }
    }
    
    void StartMagic()
    {
        animator.SetLayerWeight(fixLayerIndex, 1f);
        animator.SetTrigger("Fix");
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
    
    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        if (fireAction != null)
        {
            fireAction.performed += OnFirePerformed;
        }
#endif
    }
    
    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (fireAction != null)
        {
            fireAction.performed -= OnFirePerformed;
        }
#endif
    }
    
#if ENABLE_INPUT_SYSTEM
    void OnFirePerformed(InputAction.CallbackContext context)
    {
        FireFreeze();
    }
#endif
}