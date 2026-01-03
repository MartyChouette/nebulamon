using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerShipController2D : MonoBehaviour
    {
        private enum ActiveMode { Keyboard, KeyboardMouse, Gamepad }

        [Header("Aim camera (for Keyboard&Mouse)")]
        public Camera aimCamera;

        [Header("Movement")]
        public float thrustForce = 14f;
        public float strafeForce = 10f;          // used in Keyboard&Mouse + Gamepad
        public float turnSpeedDegPerSec = 240f;  // used in Keyboard + rotate-toward
        public float maxSpeed = 16f;
        public bool allowReverseThrust = true;

        [Header("Drag")]
        public float linearDrag = 0.05f;
        public float angularDrag = 0.6f;

        [Header("Brake (optional action)")]
        public bool enableBrake = true;
        public float brakeDragMultiplier = 6f;

        [Header("Outputs (for other scripts)")]
        public bool fireHeld;
        public bool interactPressedThisFrame;

        // Input actions
        private InputAction _move;
        private InputAction _look;     // mouse position OR right stick, depending on scheme
        private InputAction _fire;
        private InputAction _interact;
        private InputAction _brake;    // optional

        private Rigidbody2D _rb;
        private PlayerInput _playerInput;

        private Vector2 _moveValue;
        private Vector2 _lookValue;
        private bool _brakeHeld;

        private ActiveMode _mode;

        private const float StickDeadzone = 0.25f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerInput = GetComponent<PlayerInput>();

            _rb.gravityScale = 0f;
            _rb.linearDamping = linearDrag;
            _rb.angularDamping = angularDrag;

            var actions = _playerInput.actions;
            _move = actions.FindAction("Move", true);
            _look = actions.FindAction("Look", true);
            _fire = actions.FindAction("Fire", true);
            _interact = actions.FindAction("Interact", true);
            _brake = actions.FindAction("Brake", false);

            if (aimCamera == null) aimCamera = Camera.main;

            UpdateModeFromControlScheme();
        }

        private void OnEnable()
        {
            _move.Enable();
            _look.Enable();
            _fire.Enable();
            _interact.Enable();
            _brake?.Enable();

            _playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDisable()
        {
            _playerInput.onControlsChanged -= OnControlsChanged;

            _move.Disable();
            _look.Disable();
            _fire.Disable();
            _interact.Disable();
            _brake?.Disable();
        }

        private void OnControlsChanged(PlayerInput pi)
        {
            UpdateModeFromControlScheme();
        }

        private void UpdateModeFromControlScheme()
        {
            // These names MUST match your Control Scheme names in the Input Actions asset.
            string scheme = _playerInput.currentControlScheme;

            if (scheme == "Gamepad") _mode = ActiveMode.Gamepad;
            else if (scheme == "Keyboard&Mouse") _mode = ActiveMode.KeyboardMouse;
            else _mode = ActiveMode.Keyboard;

            // Debug.Log($"Control scheme: {scheme} => mode: {_mode}");
        }

        private void Update()
        {
            interactPressedThisFrame = false;

            _moveValue = _move.ReadValue<Vector2>();
            _lookValue = _look.ReadValue<Vector2>();

            fireHeld = _fire.IsPressed();

            if (_interact.WasPressedThisFrame())
                interactPressedThisFrame = true;

            _brakeHeld = (_brake != null) && _brake.IsPressed();
        }

        private void FixedUpdate()
        {
            ApplyRotation(Time.fixedDeltaTime);
            ApplyMovement();
            ApplyBrake();
            ClampSpeed();
        }

        private void ApplyRotation(float dt)
        {
            if (_mode == ActiveMode.Keyboard)
            {
                // Classic Asteroids turning: A/D (Move.x)
                float turn = _moveValue.x;
                float turnDelta = -turn * turnSpeedDegPerSec * dt;
                _rb.MoveRotation(_rb.rotation + turnDelta);
                return;
            }

            float desiredAngle = _rb.rotation;

            if (_mode == ActiveMode.KeyboardMouse)
            {
                desiredAngle = DesiredAngleFromMousePosition();
            }
            else // Gamepad
            {
                // In Gamepad scheme, Look is rightStick direction (Vector2)
                if (_lookValue.magnitude > StickDeadzone)
                {
                    Vector2 dir = _lookValue.normalized;
                    desiredAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                }
                else
                {
                    // No aim input: keep current rotation
                    desiredAngle = _rb.rotation;
                }
            }

            float next = Mathf.MoveTowardsAngle(_rb.rotation, desiredAngle, turnSpeedDegPerSec * dt);
            _rb.MoveRotation(next);
        }

        private float DesiredAngleFromMousePosition()
        {
            if (aimCamera == null) aimCamera = Camera.main;

            // In Keyboard&Mouse scheme, Look is Pointer.position (screen coords)
            Vector3 screen = new Vector3(_lookValue.x, _lookValue.y, 0f);
            Vector3 world = aimCamera.ScreenToWorldPoint(screen);
            Vector2 to = (Vector2)world - _rb.position;

            if (to.sqrMagnitude < 0.0001f)
                return _rb.rotation;

            return Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg - 90f;
        }

        private void ApplyMovement()
        {
            float forward = _moveValue.y;
            float strafe = _moveValue.x;

            if (!allowReverseThrust)
                forward = Mathf.Max(0f, forward);

            if (_mode == ActiveMode.Keyboard)
            {
                // Classic: forward/back only, strafe is reserved for turning
                if (Mathf.Abs(forward) > 0.01f)
                    _rb.AddForce((Vector2)transform.up * (thrustForce * forward), ForceMode2D.Force);

                return;
            }

            // Keyboard&Mouse or Gamepad: forward + strafe
            Vector2 force = Vector2.zero;

            if (Mathf.Abs(forward) > 0.01f)
                force += (Vector2)transform.up * (thrustForce * forward);

            if (Mathf.Abs(strafe) > 0.01f)
                force += (Vector2)transform.right * (strafeForce * strafe);

            if (force != Vector2.zero)
                _rb.AddForce(force, ForceMode2D.Force);
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
