using Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Ability_Freeeze : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private FreezeAble found_FreezeAbleObj; // FreezeAble ที่เจอ
    
    [Header("Refs")]
    [SerializeField] CinemachineVirtualCamera vcam;  
    [SerializeField] private Canvas crosshairCanvas;
    [SerializeField] private Camera playerCamera;
    
    [Header("Aim Settings")]
    [SerializeField] private float aimMaxDistance = 25f;
    [SerializeField] private Vector3 shoulderOffsetWhenAiming = new Vector3(3f, 0.5f, 0f);
    [SerializeField] private float shoulderLerp = 10f;
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask freezeableLayerMask = -1;
    [SerializeField] private bool showDebugRay = true; // แสดง Debug Ray ใน Scene View

    private Cinemachine3rdPersonFollow thirdPersonFollow;
    private Vector3 originalShoulderOffset;
    private bool isAiming = false;
    private FreezeAble currentTargetedObj;
    
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
    }

    void Update()
    {
        HandleAiming();
        UpdateAimTarget();
    }

#region Methods คลิกซ้าย
    /// <summary>
    /// แช่/หยุดแช่ 
    /// </summary>
    public void FireFreeze()
    {
        if (found_FreezeAbleObj != null && isAiming)
        {
            // ตรวจสอบสถานะปัจจุบันและสลับ
            if (found_FreezeAbleObj.freezeAbleState == FreezeAbleState.Normal)
            {
                found_FreezeAbleObj.OnFreeze();
                found_FreezeAbleObj.freezeAbleState = FreezeAbleState.Freezing;
            }
            else if (found_FreezeAbleObj.freezeAbleState == FreezeAbleState.Freezing)
            {
                found_FreezeAbleObj.StopFreeze();
                found_FreezeAbleObj.freezeAbleState = FreezeAbleState.Normal;
            }
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
            found_FreezeAbleObj = null;
        }
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
        FreezeAble detectedFreezeAble = null;
        // Raycast หา FreezeAble objects
        if (Physics.Raycast(ray, out hit, aimMaxDistance, freezeableLayerMask))
        {
            detectedFreezeAble = hit.collider.GetComponent<FreezeAble>();
            if (detectedFreezeAble == null)
                detectedFreezeAble = hit.collider.GetComponentInParent<FreezeAble>();
        }
        // จัดการ Highlight
        HandleTargetHighlight(detectedFreezeAble);
    }
#endregion  

#region Show Highlight Obj
    void HandleTargetHighlight(FreezeAble newTarget)
    {
        if (currentTargetedObj != newTarget) // ถ้า Target เปลี่ยน
        {
            // ปิด Highlight ของ Object ก่อนหน้าและเปิดของอันใหม่
            if (currentTargetedObj != null)
                ToggleObjectHighlight(currentTargetedObj, false);
            if (newTarget != null)
                ToggleObjectHighlight(newTarget, true);
            
            // อัพเดท reference
            currentTargetedObj = newTarget;
            found_FreezeAbleObj = newTarget;
        }
    }
    
    void ToggleObjectHighlight(FreezeAble freezeAbleObj, bool show)
    {
        if (freezeAbleObj != null)
        {
            // ดึง Material instance จาก Renderer
            Renderer objRenderer = freezeAbleObj.GetComponent<Renderer>();
            if (objRenderer != null && objRenderer.material != null)
            {
                freezeAbleObj.ToggleHighlight(objRenderer.material, show);
            }
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