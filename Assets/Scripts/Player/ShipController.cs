using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSim.Player
{
    [RequireComponent(typeof(ShipMotor))]
    public sealed class ShipController : MonoBehaviour
    {
        private const float LegacyMouseAxisScale = 0.1f;

        [Header("Controls")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 2f;

        private ShipMotor _motor;

        private void Awake()
        {
            _motor = GetComponent<ShipMotor>();
        }

        private void OnEnable()
        {
            LockCursor();
        }

        private void Update()
        {
            HandleInput();

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleCursorLock();
            }
        }

        private void HandleInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            float throttle = 0f;
            bool isWarping = false;

            if (keyboard != null)
            {
                bool forward = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
                bool backward = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
                throttle = (forward ? 1f : 0f) - (backward ? 1f : 0f);
                isWarping = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }

            Vector2 lookDelta = mouse != null && Cursor.lockState == CursorLockMode.Locked
                ? mouse.delta.ReadValue()
                : Vector2.zero;
            float mouseX = lookDelta.x * LegacyMouseAxisScale * mouseSensitivity;
            float mouseY = -lookDelta.y * LegacyMouseAxisScale * mouseSensitivity;

            _motor.Move(throttle, isWarping);
            _motor.Rotate(mouseY, mouseX);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ToggleCursorLock()
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }
}
