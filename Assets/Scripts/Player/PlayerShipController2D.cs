
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nebula
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerShipController2D : MonoBehaviour
    {
        private enum ActiveMode { Keyboard, KeyboardMouse, Gamepad }

        [Header("Camera (for Keyboard+Mouse aiming)")]
        public Camera aimCamera;

        [Header("World-Space Movement (top-down 2D style)")]
        public float moveForce = 18f;          // acceleration force in world X/Y
        public float maxSpeed = 16f;
        public bool allowDiagonal = true;      // if false, clamps to 4-dir

        [Header("Drag")]
        public float linearDrag = 0.05f;
        public float angularDrag = 0.6f;

        [Header("Rotation")]
        public float turnSpeedDegPerSec = 480f;
        public bool faceMoveDirectionInKeyboard = true;
        public bool faceMoveDirectionWhenNoAim = true; // for KB+Mouse and Gamepad when no aim input

        [Header("Brake (optional action)")]
        public bool enableBrake = true;
        public float brakeDragMultiplier = 6f;

        [Header("Outputs (for other scripts)")]
        public bool fireHeld;
        public bool interactPressedThisFrame;

        // Input actions
        private InputAction _move;   // Vector2 (WASD / left stick)
        private InputAction _look;   // Mouse position (screen) OR right stick (Vector2)
        private InputAction _fire;
        private InputAction _interact;
        private InputAction _brake;

        private Rigidbody2D _rb;
        private PlayerInput _playerInput;

        private Vector2 _moveValue;
        private Vector2 _lookValue;
        private bool _brakeHeld;

        private ActiveMode _mode = ActiveMode.Keyboard;

        // last-used device gating
        private InputDevice _lastDevice;
        private float _lastDeviceTime;

        private const float StickDeadzone = 0.25f;
        private const float MouseDeltaUseThresholdSqr = 0.10f * 0.10f;
        private const float DeviceSwitchCooldown = 0.15f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerInput = GetComponent<PlayerInput>();

            _rb.gravityScale = 0f;
            _rb.linearDamping = linearDrag;
            _rb.angularDamping = angularDrag;

            var actions = _playerInput != null ? _playerInput.actions : null;
            if (actions == null)
            {
                Debug.LogError($"{nameof(PlayerShipController2D)}: PlayerInput has no Actions asset assigned.");
                enabled = false;
                return;
            }

            _move = actions.FindAction("Move", true);
            _look = actions.FindAction("Look", true);
            _fire = actions.FindAction("Fire", true);
            _interact = actions.FindAction("Interact", true);
            _brake = actions.FindAction("Brake", false);

            if (aimCamera == null) aimCamera = Camera.main;

            // default mode
            if (Keyboard.current != null) SetActiveDevice(Keyboard.current, force: true);
            else if (Gamepad.current != null) SetActiveDevice(Gamepad.current, force: true);
            else if (Mouse.current != null) SetActiveDevice(Mouse.current, force: true);
        }

        private void OnEnable()
        {
            _move?.Enable();
            _look?.Enable();
            _fire?.Enable();
            _interact?.Enable();
            _brake?.Enable();
        }

        private void OnDisable()
        {
            _move?.Disable();
            _look?.Disable();
            _fire?.Disable();
            _interact?.Disable();
            _brake?.Disable();
        }

        private void Update()
        {
            interactPressedThisFrame = false;

            _moveValue = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            _lookValue = _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;

            // Optional 4-dir clamp
            if (!allowDiagonal)
            {
                if (Mathf.Abs(_moveValue.x) > Mathf.Abs(_moveValue.y)) _moveValue.y = 0f;
                else _moveValue.x = 0f;
                _moveValue = new Vector2(Mathf.Sign(_moveValue.x) * Mathf.Abs(_moveValue.x), Mathf.Sign(_moveValue.y) * Mathf.Abs(_moveValue.y));
            }

            // --- AUTO MODE SELECTION (no control schemes) ---

            // 1) Gamepad activity
            if (Gamepad.current != null)
            {
                Vector2 ls = Gamepad.current.leftStick.ReadValue();
                Vector2 rs = Gamepad.current.rightStick.ReadValue();
                bool gamepadButtons =
                    Gamepad.current.buttonSouth.wasPressedThisFrame ||
                    Gamepad.current.buttonEast.wasPressedThisFrame ||
                    Gamepad.current.buttonWest.wasPressedThisFrame ||
                    Gamepad.current.buttonNorth.wasPressedThisFrame ||
                    Gamepad.current.startButton.wasPressedThisFrame ||
                    Gamepad.current.selectButton.wasPressedThisFrame ||
                    Gamepad.current.leftShoulder.wasPressedThisFrame ||
                    Gamepad.current.rightShoulder.wasPressedThisFrame ||
                    Gamepad.current.leftTrigger.ReadValue() > 0.25f ||
                    Gamepad.current.rightTrigger.ReadValue() > 0.25f;

                if (ls.sqrMagnitude > 0.0001f ||
                    rs.sqrMagnitude > (StickDeadzone * StickDeadzone) ||
                    gamepadButtons)
                {
                    SetActiveDevice(Gamepad.current);
                }
            }

            // 2) Mouse activity
            if (_mode != ActiveMode.Gamepad && Mouse.current != null)
            {
                Vector2 md = Mouse.current.delta.ReadValue();
                bool mouseUsed =
                    md.sqrMagnitude > MouseDeltaUseThresholdSqr ||
                    Mouse.current.leftButton.wasPressedThisFrame ||
                    Mouse.current.rightButton.wasPressedThisFrame ||
                    Mouse.current.middleButton.wasPressedThisFrame;

                if (mouseUsed)
                    SetActiveDevice(Mouse.current);
            }

            // 3) Keyboard activity
            if (_mode != ActiveMode.Gamepad && _mode != ActiveMode.KeyboardMouse && Keyboard.current != null)
            {
                if (Keyboard.current.anyKey.wasPressedThisFrame)
                    SetActiveDevice(Keyboard.current);
            }

            // --- Actions ---
            fireHeld = _fire != null && _fire.IsPressed();

            if (_interact != null && _interact.WasPressedThisFrame())
                interactPressedThisFrame = true;

            _brakeHeld = (_brake != null) && _brake.IsPressed();
        }

        private void FixedUpdate()
        {
            ApplyRotation(Time.fixedDeltaTime);
            ApplyMovementWorldSpace();
            ApplyBrake();
            ClampSpeed();
        }

        private void SetActiveDevice(InputDevice device, bool force = false)
        {
            if (device == null) return;

            if (!force)
            {
                if (Time.unscaledTime - _lastDeviceTime < DeviceSwitchCooldown && _lastDevice == device)
                    return;

                if (Time.unscaledTime - _lastDeviceTime < DeviceSwitchCooldown && _lastDevice != null && _lastDevice != device)
                    return;
            }

            _lastDevice = device;
            _lastDeviceTime = Time.unscaledTime;

            if (device is Gamepad) _mode = ActiveMode.Gamepad;
            else if (device is Mouse) _mode = ActiveMode.KeyboardMouse;
            else if (device is Keyboard) _mode = ActiveMode.Keyboard;
        }

        // WORLD-SPACE MOVEMENT (not ship-relative)
        private void ApplyMovementWorldSpace()
        {
            if (_moveValue.sqrMagnitude < 0.0001f)
                return;

            Vector2 desiredDir = _moveValue.normalized; // world x/y direction
            _rb.AddForce(desiredDir * moveForce, ForceMode2D.Force);
        }

        private void ApplyRotation(float dt)
        {
            // Decide what direction we want to face (world-space direction)
            bool hasMove = _moveValue.sqrMagnitude > 0.0001f;
            Vector2 desiredFacing = Vector2.zero;

            if (_mode == ActiveMode.Keyboard)
            {
                if (faceMoveDirectionInKeyboard && hasMove)
                    desiredFacing = _moveValue.normalized;
                else
                    return; // keep current facing
            }
            else if (_mode == ActiveMode.KeyboardMouse)
            {
                // Aim at mouse; fallback to move direction if no camera or no meaningful delta
                Vector2 aimDir = AimDirFromMouse();
                if (aimDir.sqrMagnitude > 0.0001f) desiredFacing = aimDir.normalized;
                else if (faceMoveDirectionWhenNoAim && hasMove) desiredFacing = _moveValue.normalized;
                else return;
            }
            else // Gamepad
            {
                // Aim from right stick (Look action should be rightStick)
                if (_lookValue.sqrMagnitude > (StickDeadzone * StickDeadzone))
                    desiredFacing = _lookValue.normalized;
                else if (faceMoveDirectionWhenNoAim && hasMove)
                    desiredFacing = _moveValue.normalized;
                else
                    return;
            }

            // Convert facing vector -> angle. Here we treat "up" as forward (sprite faces up).
            float desiredAngle = Mathf.Atan2(desiredFacing.y, desiredFacing.x) * Mathf.Rad2Deg - 90f;
            float next = Mathf.MoveTowardsAngle(_rb.rotation, desiredAngle, turnSpeedDegPerSec * dt);
            _rb.MoveRotation(next);
        }

        private Vector2 AimDirFromMouse()
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera == null) return Vector2.zero;

            // Look action in KB+Mouse should be Pointer.position (screen coords)
            Vector3 screen = new Vector3(_lookValue.x, _lookValue.y, 0f);
            Vector3 world = aimCamera.ScreenToWorldPoint(screen);
            Vector2 to = (Vector2)world - _rb.position;
            return to;
        }

        private void ApplyBrake()
        {
            if (!enableBrake || !_brakeHeld)
            {
                _rb.linearDamping = linearDrag;
                return;
            }

            _rb.linearDamping = linearDrag * brakeDragMultiplier;
        }

        private void ClampSpeed()
        {
            float speed = _rb.linearVelocity.magnitude;
            if (speed > maxSpeed)
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }
    }
}
