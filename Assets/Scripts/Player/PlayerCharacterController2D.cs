using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nebula
{
    public enum SurfaceType
    {
        Default,
        Metal,
        Metal2,
        Grass,
        Bush,
        Wood,
        Space,
        Moon
    }

    [System.Serializable]
    public struct SurfaceClips
    {
        public SurfaceType surface;
        public AudioClip[] footstepClips;
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerCharacterController2D : MonoBehaviour
    {
        [Header("Input System")]
        [SerializeField] private InputActionReference moveAction; // Vector2
        [Tooltip("Hold direction to continuously step. If false, only steps on initial press.")]
        [SerializeField] private bool allowHoldToWalk = true;

        [Tooltip("Deadzone for treating input as 'released'.")]
        [SerializeField] private float inputDeadzone = 0.35f;

        [Header("Grid")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float stepDuration = 0.12f;
        [SerializeField] private bool snapToGridOnStart = true;

        [Header("Collision (2D)")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float wallCheckRadius = 0.18f;

        [Header("Feet / Surface Detection (2D)")]
        [SerializeField] private Transform feetPoint; // optional; if null uses rb position
        [SerializeField] private LayerMask surfaceLayer;
        [SerializeField] private float surfaceCheckRadius = 0.10f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip wallBumpClip;
        [SerializeField] private float wallBumpVolume = 0.8f;
        [SerializeField] private float footstepVolume = 0.7f;
        [SerializeField] private SurfaceClips[] surfaceFootsteps;

        [Header("Bump Feedback (optional)")]
        [SerializeField] private float bumpNudgeDistance = 0.06f;
        [SerializeField] private float bumpNudgeDuration = 0.06f;

        private Rigidbody2D _rb;

        private bool _isStepping;

        // Input state
        private Vector2 _currentCardinal;   // current held direction (clears on release!)
        private Vector2 _queuedCardinal;    // buffered direction during a step
        private bool _hasQueued;

        // For tap-only mode
        private Vector2 _prevRaw;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
        }

        private void Start()
        {
            if (snapToGridOnStart)
            {
                Vector2 snapped = SnapToCell(_rb.position);
                _rb.position = snapped;
                transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);
            }
        }

        private void Update()
        {
            if (moveAction == null) return;

            Vector2 raw = moveAction.action.ReadValue<Vector2>();

            // Treat small input as released.
            bool inputHeld = raw.sqrMagnitude >= (inputDeadzone * inputDeadzone);
            Vector2 cardinal = inputHeld ? ToCardinal(raw) : Vector2.zero;

            // IMPORTANT FIX:
            // When input is released, clear current direction so you don't "auto-step" an extra tile.
            _currentCardinal = cardinal;

            if (_isStepping)
            {
                // Buffer direction changes during movement (classic Pokémon feel)
                if (cardinal != Vector2.zero)
                {
                    _queuedCardinal = cardinal;
                    _hasQueued = true;
                }
                _prevRaw = raw;
                return;
            }

            if (allowHoldToWalk)
            {
                if (_currentCardinal != Vector2.zero)
                    TryStep(_currentCardinal);
            }
            else
            {
                // Tap-only: step only on "new press"
                bool wasHeld = _prevRaw.sqrMagnitude >= (inputDeadzone * inputDeadzone);
                if (!wasHeld && inputHeld && cardinal != Vector2.zero)
                    TryStep(cardinal);
            }

            _prevRaw = raw;
        }

        private void TryStep(Vector2 dir)
        {
            if (dir == Vector2.zero) return;
            if (_isStepping) return;

            Vector2 current = SnapToCell(_rb.position);
            Vector2 target = current + dir * cellSize;

            // Wall check at target cell center
            if (Physics2D.OverlapCircle(target, wallCheckRadius, wallLayer) != null)
            {
                OnBump(dir);
                return;
            }

            StartCoroutine(StepRoutine(current, target));
        }

        private IEnumerator StepRoutine(Vector2 from, Vector2 to)
        {
            _isStepping = true;

            PlayFootstepForCurrentSurface();

            float t = 0f;
            float inv = 1f / Mathf.Max(0.0001f, stepDuration);

            while (t < 1f)
            {
                t += Time.deltaTime * inv;
                float eased = Mathf.SmoothStep(0f, 1f, t);

                Vector2 next = Vector2.Lerp(from, to, eased);
                _rb.MovePosition(next);
                yield return null;
            }

            _rb.MovePosition(to);
            _isStepping = false;

            // If player buffered a direction mid-step and is still holding some direction, step again.
            // (If input was released, _currentCardinal will be zero now -> no extra step.)
            if (_hasQueued)
            {
                _hasQueued = false;

                // Prefer queued direction if still held
                if (_currentCardinal != Vector2.zero)
                    TryStep(_queuedCardinal);
            }
            else
            {
                // Hold-to-walk: continue stepping only if still held
                if (allowHoldToWalk && _currentCardinal != Vector2.zero)
                    TryStep(_currentCardinal);
            }
        }

        private void OnBump(Vector2 dir)
        {
            if (audioSource != null && wallBumpClip != null)
                audioSource.PlayOneShot(wallBumpClip, wallBumpVolume);

            if (bumpNudgeDistance > 0f && bumpNudgeDuration > 0f)
                StartCoroutine(BumpNudgeRoutine(dir));
        }

        private IEnumerator BumpNudgeRoutine(Vector2 dir)
        {
            Vector3 start = transform.position;
            Vector3 nudge = start + (Vector3)(dir * bumpNudgeDistance);

            float half = bumpNudgeDuration * 0.5f;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, half);
                transform.position = Vector3.Lerp(start, nudge, t);
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, half);
                transform.position = Vector3.Lerp(nudge, start, t);
                yield return null;
            }

            transform.position = start;
        }

        private void PlayFootstepForCurrentSurface()
        {
            if (audioSource == null) return;

            SurfaceType surface = DetectSurfaceUnderFeet();
            AudioClip clip = PickFootstep(surface);
            if (clip != null)
                audioSource.PlayOneShot(clip, footstepVolume);
        }

        private SurfaceType DetectSurfaceUnderFeet()
        {
            Vector2 p = feetPoint != null ? (Vector2)feetPoint.position : _rb.position;

            Collider2D c = Physics2D.OverlapCircle(p, surfaceCheckRadius, surfaceLayer);
            if (c == null) return SurfaceType.Default;

            var provider = c.GetComponent<SurfaceTypeProvider>();
            if (provider != null) return provider.surfaceType;

            switch (c.tag)
            {
                case "Metal": return SurfaceType.Metal;
                case "Metal2": return SurfaceType.Metal2;
                case "Grass": return SurfaceType.Grass;
                case "Bush": return SurfaceType.Bush;
                case "Wood": return SurfaceType.Wood;
                case "Space": return SurfaceType.Space;
                case "Moon": return SurfaceType.Moon;
                default: return SurfaceType.Default;
            }
        }

        private AudioClip PickFootstep(SurfaceType surface)
        {
            for (int i = 0; i < surfaceFootsteps.Length; i++)
            {
                if (surfaceFootsteps[i].surface != surface) continue;
                var arr = surfaceFootsteps[i].footstepClips;
                if (arr == null || arr.Length == 0) return null;
                return arr[Random.Range(0, arr.Length)];
            }

            for (int i = 0; i < surfaceFootsteps.Length; i++)
            {
                if (surfaceFootsteps[i].surface != SurfaceType.Default) continue;
                var arr = surfaceFootsteps[i].footstepClips;
                if (arr == null || arr.Length == 0) return null;
                return arr[Random.Range(0, arr.Length)];
            }

            return null;
        }

        private Vector2 SnapToCell(Vector2 world)
        {
            float x = Mathf.Round(world.x / cellSize) * cellSize;
            float y = Mathf.Round(world.y / cellSize) * cellSize;
            return new Vector2(x, y);
        }

        private Vector2 ToCardinal(Vector2 v)
        {
            if (v.sqrMagnitude < 0.01f) return Vector2.zero;

            if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
                return new Vector2(Mathf.Sign(v.x), 0f);
            else
                return new Vector2(0f, Mathf.Sign(v.y));
        }
    }
}
