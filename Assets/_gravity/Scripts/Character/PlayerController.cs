using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace GravityPrototype.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _moveActionPath = "Player/Move";
        [SerializeField] private string _jumpActionPath = "Player/Jump";

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 6.5f;
        [SerializeField] private float _acceleration = 60f;

        [Header("Jump")]
        [SerializeField] private float _jumpImpulse = 10.5f;

        [Header("Grounding")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _groundCastDistance = 0.15f;
        [SerializeField] private float _groundSnapDistance = 0.05f;

        [FormerlySerializedAs("drawGizmos")]
        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;

        private InputAction _moveAction;
        private InputAction _jumpAction;

        private float _moveX;
        private bool _isJumping;
        private bool _isGrounded;
        private RaycastHit2D lastGroundHit;

        public bool IsGrounded => _isGrounded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            _rigidbody.gravityScale = 1f;
            _rigidbody.freezeRotation = true;
        }

        private void OnEnable()
        {
            ResolveActions();
            _moveAction?.Enable();
            _jumpAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }

        private void ResolveActions()
        {
            if (_inputActions == null)
                _inputActions = Object.FindAnyObjectByType<PlayerInput>()?.actions;

            _moveAction = _inputActions != null ? _inputActions.FindAction(_moveActionPath, throwIfNotFound: false) : null;
            _jumpAction = _inputActions != null ? _inputActions.FindAction(_jumpActionPath, throwIfNotFound: false) : null;
        }

        private void Update()
        {
            if (_moveAction != null)
            {
                _moveX = Mathf.Clamp(_moveAction.ReadValue<Vector2>().x, -1f, 1f);
            }

            if (_jumpAction != null)
            {
                if (_jumpAction.WasPressedThisFrame())
                {
                    _isJumping = true;
                }
            }
        }

        private void FixedUpdate()
        {
            UpdateGrounded(Vector2.down);

            if (_isGrounded && _isJumping)
            {
                _isJumping = false;
                _isGrounded = false;

                var v = _rigidbody.linearVelocity;
                if (v.y < 0f)
                    v.y = 0f;
                _rigidbody.linearVelocity = v;

                _rigidbody.AddForce(Vector2.up * _jumpImpulse, ForceMode2D.Impulse);
            }

            ApplyHorizontalMovement();
            ApplyGroundSnap();
        }

        private void ApplyHorizontalMovement()
        {
            var dt = Time.fixedDeltaTime;
            var v = _rigidbody.linearVelocity;
            var target = _moveX * _moveSpeed;
            v.x = Mathf.MoveTowards(v.x, target, _acceleration * dt);
            _rigidbody.linearVelocity = v;
        }

        private void UpdateGrounded(Vector2 downDir)
        {
            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundMask,
                useTriggers = false
            };

            var hits = new RaycastHit2D[8];
            var hitCount = _collider.Cast(downDir, filter, hits, _groundCastDistance);

            _isGrounded = false;
            lastGroundHit = default;

            if (hitCount <= 0)
                return;

            var best = hits[0];
            for (var i = 1; i < hitCount; i++)
            {
                if (hits[i].distance < best.distance)
                    best = hits[i];
            }

            lastGroundHit = best;
            _isGrounded = best.collider != null;
        }

        private void ApplyGroundSnap()
        {
            if (!_isGrounded)
                return;

            if (lastGroundHit.collider == null)
                return;

            var dist = lastGroundHit.distance;
            if (dist <= 0f || dist > _groundSnapDistance)
                return;

            _rigidbody.position += Vector2.down * dist;

            var v = _rigidbody.linearVelocity;
            if (v.y > 0f)
                v.y = 0f;
            _rigidbody.linearVelocity = v;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos)
                return;

            var rb2d = GetComponent<Rigidbody2D>();
            var collider2d = GetComponent<Collider2D>();
            if (rb2d == null || collider2d == null)
                return;

            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(rb2d.position, rb2d.position + Vector2.down * _groundCastDistance);
        }
    }
}

