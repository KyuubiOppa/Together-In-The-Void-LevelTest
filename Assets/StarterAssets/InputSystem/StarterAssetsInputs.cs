using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("ค่า Input ของตัวละคร")]
        public Vector2 move;
        public Vector2 look;

        // ตัวแปร bool มาตรฐานที่เข้ากันได้กับระบบ StarterAssets
        public bool jump;
        public bool sprint;

        // --- ตัวกระตุ้นแบบใช้ครั้งเดียวสำหรับ Double Jump / Dash ---
        private bool _jumpDownThisFrame;
        private bool _dashThisFrame;

        [Header("การตั้งค่าการเคลื่อนที่")]
        public bool analogMovement;

        [Header("การตั้งค่า Mouse Cursor")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        // ---------- เชื่อมต่อกับ PlayerInput (Behavior: Send Messages) ----------
        public void OnMove(InputValue value) => MoveInput(value.Get<Vector2>());

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook) LookInput(value.Get<Vector2>());
        }

        // Jump: ใช้ edge trigger + รักษา jump bool แบบเดิมไว้
        public void OnJump(InputValue value)
        {
            if (value.isPressed) _jumpDownThisFrame = true; // ใช้ครั้งเดียว
            JumpInput(value.isPressed);                      // bool แบบเดิม
        }

        // Sprint: สลับโหมด Sprint เมื่อกดปุ่ม (CapsLock) แทนการกดค้าง
        public void OnSprint(InputValue value)
        {
            if (value.isPressed)
            {
                sprint = !sprint;
                Debug.Log($"[Inputs] Sprint Toggle => {(sprint ? "ON" : "OFF")}");
            }
        }

        // Dash/Dodge: รองรับทั้ง input action "Dash/Dodge" และ "Dash"
        public void OnDashDodge(InputValue value)
        {
            if (value.isPressed) { _dashThisFrame = true; Debug.Log("[Inputs] Dash Triggered (Dash/Dodge)"); }
        }
        public void OnDash(InputValue value)
        {
            if (value.isPressed) { _dashThisFrame = true; Debug.Log("[Inputs] Dash Triggered (Dash)"); }
        }
#endif

        // ---------- ฟังก์ชันสาธารณะสำหรับ UICanvasControllerInput.cs ----------
        public void MoveInput(Vector2 newMoveDirection) => move = newMoveDirection;
        public void LookInput(Vector2 newLookDirection) => look = newLookDirection;

        // สำหรับ UI input: ตรวจจับการเปลี่ยนจาก false->true เพื่อกระตุ้นแบบครั้งเดียว
        public void JumpInput(bool newJumpState)
        {
            if (newJumpState && !jump) _jumpDownThisFrame = true; // rising edge
            jump = newJumpState;
        }

        // UI สามารถตั้งค่า sprint ตรงๆ ได้ (ไม่เหมือน OnSprint ที่ใช้ toggle)
        public void SprintInput(bool newSprintState) => sprint = newSprintState;

        // ---------- ตัวใช้งานแบบครั้งเดียว (ใช้ครั้งเดียวต่อเฟรม) ----------
        public bool ConsumeJumpDown()
        {
            if (_jumpDownThisFrame)
            {
                _jumpDownThisFrame = false;
                return true;
            }
            return false;
        }

        public bool ConsumeDash()
        {
            if (_dashThisFrame)
            {
                _dashThisFrame = false;
                return true;
            }
            return false;
        }

        private void OnApplicationFocus(bool hasFocus) => SetCursorState(cursorLocked);
        private void SetCursorState(bool newState) =>
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}