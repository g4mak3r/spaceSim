using UnityEngine;

namespace SpaceSim.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShipMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float normalSpeed = 20f;
        [SerializeField, Min(0f)] private float warpSpeed = 500f;
        [SerializeField, Min(0f)] private float acceleration = 2f;
        [SerializeField, Min(0f)] private float deceleration = 1f;

        [Header("Rotation")]
        [SerializeField, Min(0f)] private float rollRecoverySpeed = 2f;

        private Rigidbody _rigidbody;
        private float _throttleInput;
        private bool _warpInput;
        private Vector2 _pendingLookDelta;

        public float CurrentSpeed { get; private set; }
        public bool IsWarping { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            UpdateSpeed();
            ApplyMovement();
            ApplyRotation();
        }

        public void Move(float throttleInput, bool warpInput)
        {
            _throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
            _warpInput = warpInput;
        }

        public void Rotate(float pitch, float yaw)
        {
            _pendingLookDelta += new Vector2(pitch, yaw);
        }

        public float GetSpeedPercentage()
        {
            float maxSpeed = IsWarping ? warpSpeed : normalSpeed;
            return maxSpeed <= Mathf.Epsilon ? 0f : Mathf.Clamp01(CurrentSpeed / maxSpeed);
        }

        private void UpdateSpeed()
        {
            IsWarping = _warpInput && _throttleInput > 0f;

            float targetSpeed = _throttleInput > 0f
                ? (IsWarping ? warpSpeed : normalSpeed)
                : 0f;

            float response = targetSpeed > CurrentSpeed ? acceleration : deceleration;
            float blend = 1f - Mathf.Exp(-response * Time.fixedDeltaTime);
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, blend);
        }

        private void ApplyMovement()
        {
            Vector3 nextPosition = _rigidbody.position
                + transform.forward * (CurrentSpeed * Time.fixedDeltaTime);

            _rigidbody.MovePosition(nextPosition);
        }

        private void ApplyRotation()
        {
            Quaternion inputRotation = Quaternion.Euler(_pendingLookDelta.x, _pendingLookDelta.y, 0f);
            Quaternion targetRotation = _rigidbody.rotation * inputRotation;

            Vector3 euler = targetRotation.eulerAngles;
            float rollBlend = 1f - Mathf.Exp(-rollRecoverySpeed * Time.fixedDeltaTime);
            euler.z = Mathf.LerpAngle(euler.z, 0f, rollBlend);

            _rigidbody.MoveRotation(Quaternion.Euler(euler));
            _pendingLookDelta = Vector2.zero;
        }
    }
}
