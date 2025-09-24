using Cinemachine;
using UnityEngine;
using Unity.Netcode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Ability_Freeeze : NetworkBehaviour
{
    [Header("Debug")]
    [SerializeField] private FreezeAble found_FreezeAbleObj;
    
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
    [SerializeField] private LayerMask freezeableLayerMask = -1;
    [SerializeField] private bool showDebugRay = true;
    [Header("Animator Freeze Layer")]
    [SerializeField] private string freezeLayerName = "FreezeAbility"; 
    [SerializeField] private float freezeLayerLerpSpeed = 5f;
    private int freezeLayerIndex;
    private float targetFreezeWeight = 0f;
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
            aimAction = pi.actions["Aim"];
            fireAction = pi.actions["Fire"];
        }
#endif
        if (crosshairCanvas) crosshairCanvas.gameObject.SetActive(false);

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
            
        freezeLayerIndex = animator.GetLayerIndex(freezeLayerName);
    }

    void Update()
    {
        if (!IsOwner) return; // เฉพาะ Owner เท่านั้น
        
        HandleAiming();
        UpdateAimTarget();

        if (animator != null && freezeLayerIndex >= 0)
        {
            float currentWeight = animator.GetLayerWeight(freezeLayerIndex);
            float newWeight = Mathf.Lerp(currentWeight, targetFreezeWeight, Time.deltaTime * freezeLayerLerpSpeed);
            animator.SetLayerWeight(freezeLayerIndex, newWeight);
        }
    }

    #region Network Freeze Methods
    public void FireFreeze()
    {
        if (!IsOwner) return;
        
        if (found_FreezeAbleObj != null && isAiming)
        {
            StartMagic();
            
            // ส่งคำสั่งไป Server
            ulong targetNetworkId = found_FreezeAbleObj.GetComponent<NetworkObject>().NetworkObjectId;
            RequestFreezeServerRpc(targetNetworkId);
        }
    }
    
    [ServerRpc]
    void RequestFreezeServerRpc(ulong targetNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out NetworkObject networkObj))
        {
            FreezeAble freezeAbleObj = networkObj.GetComponent<FreezeAble>();
            if (freezeAbleObj != null)
            {
                if (freezeAbleObj.freezeAbleState == FreezeAbleState.Normal)
                {
                    freezeAbleObj.NetworkFreeze();
                }
                else if (freezeAbleObj.freezeAbleState == FreezeAbleState.Freezing)
                {
                    freezeAbleObj.NetworkUnfreeze();
                }
            }
        }
    }
    #endregion

    void StartMagic()
    {
        animator.SetLayerWeight(freezeLayerIndex, 1f);
        animator.SetTrigger("Freeze");
    }

    #region Aiming Methods
    void HandleAiming()
    {
        if (!IsOwner) return;
        
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
        if (crosshairCanvas)
            crosshairCanvas.gameObject.SetActive(true);
            
        targetFreezeWeight = 1f;
    }

    void StopAiming()
    {
        if (crosshairCanvas)
            crosshairCanvas.gameObject.SetActive(false);

        if (currentTargetedObj != null)
        {
            ToggleObjectHighlight(currentTargetedObj, false);
            currentTargetedObj = null;
            found_FreezeAbleObj = null;
        }
        
        targetFreezeWeight = 0f;
    }
    
    void UpdateAimTarget()
    {
        if (!isAiming || playerCamera == null) 
            return;

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * aimMaxDistance, Color.red, 0.1f);
        
        RaycastHit hit;
        FreezeAble detectedFreezeAble = null;
        
        if (Physics.Raycast(ray, out hit, aimMaxDistance, freezeableLayerMask))
        {
            detectedFreezeAble = hit.collider.GetComponent<FreezeAble>();
            if (detectedFreezeAble == null)
                detectedFreezeAble = hit.collider.GetComponentInParent<FreezeAble>();
        }
        
        HandleTargetHighlight(detectedFreezeAble);
    }
    #endregion

    #region Highlight Methods
    void HandleTargetHighlight(FreezeAble newTarget)
    {
        if (currentTargetedObj != newTarget)
        {
            if (currentTargetedObj != null)
                ToggleObjectHighlight(currentTargetedObj, false);
            if (newTarget != null)
                ToggleObjectHighlight(newTarget, true);
            
            currentTargetedObj = newTarget;
            found_FreezeAbleObj = newTarget;
        }
    }
    
    void ToggleObjectHighlight(FreezeAble freezeAbleObj, bool show)
    {
        if (freezeAbleObj != null)
        {
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