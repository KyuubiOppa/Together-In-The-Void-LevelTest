using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/*
 * ThirdPersonController_Rigidbody (Clean)
 * - Movement/Sprint
 * - Jump: นับจำนวนครั้งตาม MaxJumps (Double Jump เสถียร)
 * - Dash: สลับได้ระหว่าง Impulse / FixedDistance
 * - Air Dash: 1 ครั้งหลังกระโดดครั้งที่สอง
 * - GroundedCheck: ลด false-positive ตอน “เพิ่งกระโดดขึ้น” โดยไม่ตัด grounded ตอนไต่เนิน
 * - Wall Slide: project ทิศวิ่งบนผนัง
 */
namespace StarterAssets
{
    [RequireComponent(typeof(Rigidbody))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    [RequireComponent(typeof(CapsuleCollider))]
    public class ThirdPersonController_Rigidbody : MonoBehaviour
    {
        // ===== Movement =====
        [Header("Movement")]
        [Tooltip("ความเร็วเดิน")]
        public float MoveSpeed = 2.0f;
        [Tooltip("ความเร็วสปรินต์")]
        public float SprintSpeed = 5.335f;
        [Range(0.0f, 0.3f), Tooltip("ความหนืดการหมุน")]
        public float RotationSmoothTime = 0.12f;
        [Tooltip("อัตราเปลี่ยนความเร็วสำหรับ lerp")]
        public float SpeedChangeRate = 10.0f;

        // ===== Jump =====
        [Header("Jump")]
        [Tooltip("ความสูงกระโดด (เมตร)")]
        public float JumpHeight = 1.2f;
        [Tooltip("กันอินพุตเด้ง: หน่วงขั้นต่ำระหว่างกระโดดสองครั้ง (วิ)")]
        public float DoubleJumpMinDelay = 0.08f;
        [Tooltip("เวลาที่ต้องยืนพื้นนิ่งเพื่อรีเซ็ตจำนวนกระโดด (วิ)")]
        public float GroundSettleTime = 0.04f;
        [Tooltip("จำนวนครั้งสูงสุดของการกระโดด (2 = Double Jump)")]
        public int MaxJumps = 2;

        [Tooltip("ดีเลย์ภายในก่อนอนุญาต Jump ใหม่ (เผื่อใช้กับแอนิเมชัน)")]
        public float JumpTimeout = 0.20f;
        [Tooltip("หน่วงก่อนเข้า FreeFall")]
        public float FallTimeout = 0.15f;

        // ===== Dash =====
        public enum DashMode { Impulse, FixedDistance }

        [Header("Dash")]
        [Tooltip("โหมดดาช")]
        public DashMode dashMode = DashMode.FixedDistance;
        [Tooltip("คูลดาวน์ดาช (วิ)")]
        public float DashCooldown = 0.75f;
        [Tooltip("แรงพุ่งแนวนอน (ใช้กับ Impulse)")]
        public float DashImpulse = 10f;
        [Tooltip("ระยะดาช (เมตร) ใช้กับ FixedDistance")]
        public float DashDistance = 5f;
        [Tooltip("เวลาที่ใช้ดาช (วิ) ใช้กับ FixedDistance")]
        public float DashDuration = 0.25f;

        // ===== Ground Check =====
        [Header("Ground Check")]
        public bool Grounded = true;
        [Tooltip("ออฟเซ็ตแสดง gizmo เฉยๆ")]
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        [Tooltip("เลเยอร์พื้น (Default = Layer 0)")]
        public LayerMask GroundLayers = 1 << 0;

        [Header("Ground Check (Advanced)")]
        [Tooltip("ระยะ cast หาพื้น (แนะนำ 0.22–0.30)")]
        public float GroundCheckDistance = 0.24f;       // ดีฟอลต์ปรับให้ง่ายต่อไต่เนิน
        [Range(0f, 1f), Tooltip("y ของ normal ต่ำสุดที่นับว่าเป็นพื้น (0.60–0.70)")]
        public float GroundNormalMinY = 0.65f;          // รองรับเนินมากขึ้น
        [Range(0.5f, 1f), Tooltip("ตัวคูณรัศมี probe (0.90–0.95)")]
        public float GroundProbeRadiusMul = 0.93f;      // ไม่ชนตัวเองง่าย

        [Header("Grounded Anti-Flicker (หลังเพิ่งกระโดด)")]
        [Tooltip("ช่วงเวลาหลังจากกระโดดที่ยังถือว่า 'เพิ่งกระโดด' (วิ)")]
        public float RecentJumpWindow = 0.12f;
        [Tooltip("เพดานความเร็วแกน Y ระหว่าง 'เพิ่งกระโดด' ที่ให้ตัด grounded")]
        public float UpwardVelThreshold = 1.5f;

        // ===== Camera =====
        [Header("Camera / Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        // ===== Audio (optional) =====
        [Header("Audio (optional)")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        // ===== Rigidbody =====
        [Header("Rigidbody Tuning")]
        public float AngularDragOnBody = 5f;
        public CollisionDetectionMode CollisionMode = CollisionDetectionMode.Continuous;

        // ===== Collision Slide =====
        [Header("Collision Slide")]
        [Tooltip("ระยะตรวจผนังเพื่อเลื่อนตามผนัง")]
        public float WallCheckDistance = 0.3f;

        // ===== Private refs =====
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private Rigidbody _rb;
        private CapsuleCollider _capsule;

        // Camera state
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // Movement state
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0f;
        private float _rotationVelocity;

        // Timers
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // Jump/Dash state
        private int _jumpCount = 0;
        private bool _airDashAvailable = false;   // 1 ครั้งหลัง Double Jump
        private float _lastDashTime = -999f;
        private float _lastJumpTime = -999f;
        private float _groundedStableSince = -999f;

        // Animator IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private bool _hasAnimator;
        private const float _threshold = 0.01f;
        private const float _terminalHorizontalSpeed = 100f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        // ===== Unity Lifecycle =====
        private void Awake()
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionMode;
            _rb.angularDamping = AngularDragOnBody;
            _rb.maxAngularVelocity = 0.01f;

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();
            HandleJump();
            HandleDash();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            HandleMove();
            _rb.angularVelocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            HandleCameraRotation();
        }

        // ===== Core =====
        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            bool wasGrounded = Grounded;
            Grounded = false;

            if (_capsule != null)
            {
                float scaleY = Mathf.Abs(transform.lossyScale.y);
                float scaleX = Mathf.Abs(transform.lossyScale.x);
                float radius = _capsule.radius * scaleX * GroundProbeRadiusMul;
                float height = Mathf.Max(_capsule.height * scaleY, radius * 2f);

                Vector3 centerWorld = transform.TransformPoint(_capsule.center);
                Vector3 up = transform.up;

                Vector3 pTop = centerWorld + up * (height * 0.5f - radius);
                Vector3 pBot = centerWorld - up * (height * 0.5f - radius);

                Vector3 castStartTop = pTop + up * 0.02f;
                Vector3 castStartBot = pBot + up * 0.02f;

                float dist = Mathf.Max(0.02f, GroundCheckDistance);

                bool hitGround = false;

                if (Physics.CapsuleCast(castStartTop, castStartBot, radius, -up, out RaycastHit hit, dist, GroundLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.normal.y >= GroundNormalMinY) hitGround = true;
                }
                else
                {
                    float overlapDist = dist * 0.5f;
                    Vector3 oTop = pTop - up * overlapDist;
                    Vector3 oBot = pBot - up * overlapDist;
                    var cols = Physics.OverlapCapsule(oTop, oBot, radius, GroundLayers, QueryTriggerInteraction.Ignore);
                    if (cols != null && cols.Length > 0) hitGround = true;
                }

                // ตัด grounded เฉพาะกรณี "เพิ่งกระโดดขึ้น" ด้วยความเร็วแกน Y สูง
                bool justJumped = (Time.time - _lastJumpTime) <= RecentJumpWindow;
                if (hitGround && justJumped && _rb.linearVelocity.y > UpwardVelThreshold)
                    hitGround = false;

                Grounded = hitGround;
            }

            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);

            if (Grounded)
            {
                if (!wasGrounded && _hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                _fallTimeoutDelta = FallTimeout;

                if (_groundedStableSince < 0f) _groundedStableSince = Time.time;

                if (Time.time - _groundedStableSince >= GroundSettleTime)
                {
                    _jumpCount = 0;
                    _airDashAvailable = false;
                }
            }
            else
            {
                _groundedStableSince = -999f;

                if (_fallTimeoutDelta >= 0f) _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
            }
        }

        private void HandleCameraRotation()
        {
            if (CinemachineCameraTarget == null || LockCameraPosition) return;

            if (_input.look.sqrMagnitude >= _threshold)
            {
                float dtMul = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * dtMul;
                _cinemachineTargetPitch += _input.look.y * dtMul;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f
            );
        }

        private void HandleMove()
        {
            float targetSpeed = (_input.sprint ? SprintSpeed : MoveSpeed);
            if (_input.move == Vector2.zero) targetSpeed = 0f;

            Vector3 vel = _rb.linearVelocity;
            float currentHorSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;

            float speedOffset = 0.1f;
            float inputMag = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorSpeed < targetSpeed - speedOffset || currentHorSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorSpeed, targetSpeed * inputMag, Time.fixedDeltaTime * SpeedChangeRate);
                _speed = Mathf.Min(_speed, _terminalHorizontalSpeed);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.fixedDeltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDir = new Vector3(_input.move.x, 0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                float camYaw = (_mainCamera ? _mainCamera.transform.eulerAngles.y : 0f);
                _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camYaw;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                _rb.MoveRotation(Quaternion.Euler(0f, rotation, 0f));
            }

            Vector3 fwd = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
            Vector3 desiredHorizontal = fwd.normalized * _speed;

            // Wall Slide
            Vector3 moveDir = desiredHorizontal.normalized;
            if (moveDir.sqrMagnitude > 0.0001f && WallCheckDistance > 0f)
            {
                float radius = _capsule ? _capsule.radius * Mathf.Abs(transform.lossyScale.x) : GroundedRadius;
                float halfBody = _capsule
                    ? Mathf.Max(0.01f, (_capsule.height * Mathf.Abs(transform.lossyScale.y)) * 0.5f - radius)
                    : 0.9f;

                Vector3 p1 = transform.position + Vector3.up * (halfBody + radius);
                Vector3 p2 = transform.position + Vector3.up * radius;

                float castDist = Mathf.Max(WallCheckDistance, (_speed * Time.fixedDeltaTime) + radius * 0.5f);

                if (Physics.CapsuleCast(p1, p2, radius * 0.95f, moveDir, out RaycastHit hit, castDist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.normal.y < 0.5f)
                    {
                        desiredHorizontal = Vector3.ProjectOnPlane(desiredHorizontal, hit.normal);
                        Vector3 curH = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                        curH = Vector3.ProjectOnPlane(curH, hit.normal);
                        Vector3 deltaSlide = desiredHorizontal - curH;
                        _rb.AddForce(new Vector3(deltaSlide.x, 0f, deltaSlide.z), ForceMode.VelocityChange);
                        return;
                    }
                }
            }

            Vector3 currentH = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 delta = desiredHorizontal - currentH;
            _rb.AddForce(new Vector3(delta.x, 0f, delta.z), ForceMode.VelocityChange);
        }

        private void HandleJump()
        {
            bool jumpPressed = _input.ConsumeJumpDown();
            bool jumpDelayOK = (Time.time - _lastJumpTime) >= DoubleJumpMinDelay;

            if (jumpPressed && jumpDelayOK)
            {
                if (Grounded) _jumpCount = 0;

                if (_jumpCount < MaxJumps)
                {
                    DoJump();
                    _jumpCount++;
                    _lastJumpTime = Time.time;

                    if (_jumpCount == 1)
                    {
                        _airDashAvailable = false;
                        Debug.Log("[Jump] Jump 1");
                    }
                    else if (_jumpCount == 2)
                    {
                        _airDashAvailable = true; // เปิด Air Dash 1 ครั้ง
                        Debug.Log("[Jump] Double Jump -> AirDash available (1x).");
                    }
                    else
                    {
                        Debug.Log($"[Jump] Jump {_jumpCount}");
                    }
                }
            }
        }

        private void DoJump()
        {
            float jumpVel = Mathf.Sqrt(2f * -Physics.gravity.y * JumpHeight);
            Vector3 v = _rb.linearVelocity;
            v.y = jumpVel;
            _rb.linearVelocity = v;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
                _animator.SetBool(_animIDFreeFall, false);
            }

            _jumpTimeoutDelta = JumpTimeout;
        }

        private void HandleDash()
        {
            if (!_input.ConsumeDash()) return;

            if (Time.time < _lastDashTime + DashCooldown)
            {
                Debug.Log("[Dash] On cooldown.");
                return;
            }

            bool canDash = Grounded || _airDashAvailable;
            if (!canDash)
            {
                Debug.Log("[Dash] Not allowed (no air-dash right).");
                return;
            }

            if (!Grounded) _airDashAvailable = false;

            Vector3 horVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 dashDir = horVel.sqrMagnitude > 0.01f ? horVel.normalized : transform.forward;

            if (dashMode == DashMode.FixedDistance)
            {
                float dashSpeed = DashDistance / Mathf.Max(0.01f, DashDuration);
                Vector3 targetVel = dashDir * dashSpeed;
                _rb.linearVelocity = new Vector3(targetVel.x, _rb.linearVelocity.y, targetVel.z);
            }
            else // Impulse
            {
                Vector3 add = dashDir * DashImpulse;
                _rb.AddForce(new Vector3(add.x, 0f, add.z), ForceMode.VelocityChange);
            }

            _lastDashTime = Time.time;
            Debug.Log(Grounded ? "[Dash] Ground Dash!" : "[Dash] Air Dash!");
        }

        private void UpdateAnimator()
        {
            if (!_hasAnimator) return;
            float inputMag = _input.analogMovement ? _input.move.magnitude : 1f;
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMag);
        }

        // ===== Utils =====
        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void OnDrawGizmosSelected()
        {
            Color g = new Color(0f, 1f, 0f, 0.35f);
            Color r = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.color = Grounded ? g : r;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius
            );
        }

        private void OnFootstep(AnimationEvent e)
        {
            if (e.animatorClipInfo.weight > 0.5f && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.position, FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent e)
        {
            if (e.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, FootstepAudioVolume);
            }
        }
    }
}