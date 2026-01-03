using UnityEngine;

namespace SpaceGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class WorldBoundsClamp2D : MonoBehaviour
    {
        [Header("Bounds in world units")]
        public Vector2 min = new Vector2(-200, -120);
        public Vector2 max = new Vector2(200, 120);

        [Header("Behavior")]
        public bool zeroVelocityWhenClamped = true;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void LateUpdate()
        {
            Vector2 p = _rb.position;
            Vector2 clamped = new Vector2(
                Mathf.Clamp(p.x, min.x, max.x),
                Mathf.Clamp(p.y, min.y, max.y)
            );

            if (clamped != p)
            {
                _rb.position = clamped;

                if (zeroVelocityWhenClamped)
                {
                    // Remove velocity pushing outward (simple + stable)
                    Vector2 v = _rb.linearVelocity;

                    if (p.x < min.x && v.x < 0) v.x = 0;
                    if (p.x > max.x && v.x > 0) v.x = 0;
                    if (p.y < min.y && v.y < 0) v.y = 0;
                    if (p.y > max.y && v.y > 0) v.y = 0;

                    _rb.linearVelocity = v;
                }
            }
        }
    }
}
