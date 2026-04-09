using UnityEngine;
using UnityEngine.InputSystem;

namespace GravityPrototype.Character
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _moveActionPath = "Player/Move";
        [SerializeField] private string _jumpActionPath = "Player/Jump";

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 6.5f;

        [Header("Jump")]
        [SerializeField] private float _jumpImpulse = 10.5f;

        [Header("Grounding")]
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField, Min(0f)] private float _groundCheckDistance = 0.15f;

        private Rigidbody2D _rb;
        private Collider2D _col;

        private InputAction _moveAction;
        private InputAction _jumpAction;

        private float _moveX;
        private bool _jumpPressed;
        private bool _grounded;

        public bool IsGrounded => _grounded;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            _rb.gravityScale = 1f;
            _rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            if (_moveAction == null || _jumpAction == null)
            {
                if (_inputActions == null)
                    _inputActions = Object.FindAnyObjectByType<PlayerInput>()?.actions;

                _moveAction = _inputActions != null ? _inputActions.FindAction(_moveActionPath, throwIfNotFound: false) : null;
                _jumpAction = _inputActions != null ? _inputActions.FindAction(_jumpActionPath, throwIfNotFound: false) : null;
            }

            _moveAction?.Enable();
            _jumpAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
        }

        private void Update()
        {
            if (_moveAction != null)
                _moveX = Mathf.Clamp(_moveAction.ReadValue<Vector2>().x, -1f, 1f);

            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
                _jumpPressed = true;
        }

        private void FixedUpdate()
        {
            _grounded = IsGroundedNow();

            var v = _rb.linearVelocity;
            v.x = _moveX * _moveSpeed;

            if (_grounded && _jumpPressed)
            {
                _jumpPressed = false;

                if (v.y < 0f)
                    v.y = 0f;
                _rb.linearVelocity = v;

                _rb.AddForce(Vector2.up * _jumpImpulse, ForceMode2D.Impulse);
            }
            else
            {
                _jumpPressed = false;
                _rb.linearVelocity = v;
            }
        }

        private bool IsGroundedNow()
        {
            var bounds = _col.bounds;
            var origin = new Vector2(bounds.center.x, bounds.min.y + 0.02f);
            var hit = Physics2D.Raycast(origin, Vector2.down, _groundCheckDistance, _groundMask);
            return hit.collider != null;
        }
    }
}

