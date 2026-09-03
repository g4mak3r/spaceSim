using System;
using UnityEngine;

namespace SpaceSim.Player
{
    public enum ShipDriveMode
    {
        Rifting,
        Vector,
        Warp
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class ShipMotor : MonoBehaviour
    {
        [Header("Rifting Flight")]
        [SerializeField, Min(1f)] private float riftingMaxSpeed = 85f;
        [SerializeField, Min(0f)] private float riftingAcceleration = 11.5f;
        [Tooltip("Passive flight-assist damping. Absolute deceleration naturally becomes weaker as speed approaches zero.")]
        [SerializeField, Min(0f)] private float coastDragPerSecond = 0.30f;
        [SerializeField, Min(0f)] private float coastStopEpsilon = 0.025f;
        [SerializeField, Min(0f)] private float lateralAssist = 0.16f;

        [Header("Vector Boost (hold Shift)")]
        [SerializeField, Min(1f)] private float vectorMaxSpeed = 200f;
        [SerializeField, Min(0f)] private float vectorAcceleration = 32f;
        [SerializeField, Min(0f)] private float vectorReleaseDeceleration = 30f;

        [Header("Reverse / Braking")]
        [SerializeField, Min(1f)] private float reverseMaxSpeed = 32f;
        [SerializeField, Min(0f)] private float reverseAcceleration = 8f;
        [Tooltip("Active S-brake damping. Braking is exponential, so absolute deceleration fades naturally near zero.")]
        [SerializeField, Min(0f)] private float brakeDragPerSecond = 1.35f;
        [SerializeField, Min(0f)] private float highSpeedBrakeDragPerSecond = 2.25f;
        [SerializeField, Min(0f)] private float reverseEntrySpeed = 0.30f;
        [SerializeField, Min(0f)] private float reverseLateralTolerance = 0.50f;

        [Header("Warp")]
        [SerializeField, Min(0.5f)] private float warpChargeDuration = 3f;
        [SerializeField, Min(0f)] private float warpChargeMaxSpeed = 1.5f;
        [SerializeField, Min(0f)] private float warpChargeStabilization = 4f;
        [SerializeField, Min(1f)] private float warpMaxSpeed = 1800f;
        [SerializeField, Min(0f)] private float warpAcceleration = 560f;
        [Tooltip("Exponential decay of speed above the normal cruise envelope after SPACE is released.")]
        [SerializeField, Min(0f)] private float warpExitDragPerSecond = 1.70f;
        [Tooltip("Emergency WARP braking when S is held. Still exponential instead of snapping to zero.")]
        [SerializeField, Min(0f)] private float warpEmergencyBrakeDragPerSecond = 2.60f;
        [SerializeField, Range(0f, 90f)] private float warpVelocityAlignment = 18f;

        [Header("Unified WARP Presentation")]
        [SerializeField, Min(0f)] private float warpVisualStartSpeed = 140f;
        [SerializeField, Min(1f)] private float warpVisualFullSpeed = 520f;
        [SerializeField, Min(0.1f)] private float warpVisualResponse = 6.0f;

        [Header("Rotational Inertia")]
        [SerializeField, Min(0f)] private float rotationImpulse = 7.5f;
        [SerializeField, Min(1f)] private float maxPitchRate = 42f;
        [SerializeField, Min(1f)] private float maxYawRate = 55f;
        [SerializeField, Min(0f)] private float turnInputDamping = 1.15f;
        [SerializeField, Min(0f)] private float turnReleaseDamping = 2.8f;
        [SerializeField, Range(0.1f, 1f)] private float warpTurnRateMultiplier = 0.55f;

        [Header("High-speed Presentation")]
        [SerializeField, Min(0f)] private float highSpeedEffectStart = 135f;
        [SerializeField, Min(1f)] private float highSpeedEffectFull = 200f;
        [SerializeField, Min(0.05f)] private float presentationResponseTime = 0.28f;

        [SerializeField, HideInInspector] private int tuningRevision;
        private const int CurrentTuningRevision = 4;

        private Rigidbody _rigidbody;
        private Vector3 _velocity;
        private Vector2 _pendingLookDelta;
        private Vector2 _angularRate;
        private float _throttleInput;
        private bool _vectorBoostHeld;
        private bool _warpHeld;
        private bool _warpActive;
        private bool _warpExitActive;
        private float _warpChargeProgress;
        private float _effectsVelocity;
        private float _highSpeedVisualVelocity;

        public event Action<ShipDriveMode> DriveModeChanged;

        public float CurrentSpeed { get; private set; }
        public float RiftingMaxSpeed => riftingMaxSpeed;
        public float VectorMaxSpeed => vectorMaxSpeed;
        public float WarpMaxSpeed => warpMaxSpeed;
        public float SignedForwardSpeed => Vector3.Dot(_velocity, transform.forward);
        public Vector3 Velocity => _velocity;
        public ShipDriveMode CurrentDriveMode { get; private set; } = ShipDriveMode.Rifting;
        public float WarpIntensity { get; private set; }
        public float FlightEffectIntensity { get; private set; }
        public float HighSpeedVisualIntensity { get; private set; }
        public bool IsWarping => _warpActive;
        public bool IsWarpCharging => !_warpActive && _warpChargeProgress > 0f;
        public bool IsWarpDisengaging => _warpExitActive;
        public bool IsVectorBoosting => !_warpActive && !_warpExitActive && _vectorBoostHeld;
        public float WarpChargeRemaining => Mathf.Max(0f, warpChargeDuration - _warpChargeProgress);
        public int WarpCountdown => Mathf.Clamp(Mathf.CeilToInt(WarpChargeRemaining), 1, Mathf.CeilToInt(warpChargeDuration));
        public bool CanChargeWarp => !_warpActive
            && !_warpExitActive
            && CurrentSpeed <= warpChargeMaxSpeed
            && Mathf.Abs(_throttleInput) < 0.05f;

        private void Awake()
        {
            ApplyCurrentTuningRevision();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void OnValidate()
        {
            ApplyCurrentTuningRevision();
        }

        private void FixedUpdate()
        {
            UpdateWarpState();
            UpdateDriveMode();
            UpdateLinearMotion();
            UpdateRotation();
            UpdatePresentationState();
            ApplyMovement();
        }

        public void Move(float throttleInput)
        {
            _throttleInput = Mathf.Clamp(throttleInput, -1f, 1f);
        }

        public void SetVectorBoost(bool held)
        {
            _vectorBoostHeld = held;
        }

        public void SetWarpHeld(bool held)
        {
            _warpHeld = held;
        }

        public void Rotate(float pitch, float yaw)
        {
            _pendingLookDelta += new Vector2(pitch, yaw);
        }

        public float GetSpeedPercentage()
        {
            float referenceSpeed = CurrentDriveMode switch
            {
                ShipDriveMode.Vector => vectorMaxSpeed,
                ShipDriveMode.Warp => warpMaxSpeed,
                _ => riftingMaxSpeed
            };

            return Mathf.Clamp01(CurrentSpeed / Mathf.Max(0.001f, referenceSpeed));
        }

        public float GetAbsoluteSpeedPercentage()
        {
            return Mathf.Clamp01(CurrentSpeed / Mathf.Max(0.001f, warpMaxSpeed));
        }

        private void UpdateWarpState()
        {
            bool braking = _throttleInput < -0.01f;

            if (_warpActive)
            {
                if (!_warpHeld || braking)
                {
                    _warpActive = false;
                    _warpExitActive = true;
                    _warpChargeProgress = 0f;
                }

                return;
            }

            if (_warpExitActive)
            {
                _warpChargeProgress = 0f;
                return;
            }

            if (_warpHeld && CanChargeWarp)
            {
                _warpChargeProgress += Time.fixedDeltaTime;

                if (_velocity.sqrMagnitude > 0.000001f)
                {
                    _velocity = Vector3.MoveTowards(
                        _velocity,
                        Vector3.zero,
                        warpChargeStabilization * Time.fixedDeltaTime);
                }

                if (_warpChargeProgress >= warpChargeDuration)
                {
                    _warpChargeProgress = 0f;
                    _warpActive = true;
                    _warpExitActive = false;
                }
            }
            else
            {
                _warpChargeProgress = 0f;
            }
        }

        private void UpdateDriveMode()
        {
            ShipDriveMode nextMode = _warpActive
                ? ShipDriveMode.Warp
                : _vectorBoostHeld && !_warpExitActive
                    ? ShipDriveMode.Vector
                    : ShipDriveMode.Rifting;

            if (nextMode == CurrentDriveMode)
            {
                return;
            }

            CurrentDriveMode = nextMode;
            DriveModeChanged?.Invoke(CurrentDriveMode);
        }

        private void UpdateLinearMotion()
        {
            if (_warpActive)
            {
                ApplyWarpThrust();
                CurrentSpeed = _velocity.magnitude;
                return;
            }

            if (_warpExitActive)
            {
                ApplyWarpExit();
                CurrentSpeed = _velocity.magnitude;
                return;
            }

            if (_throttleInput > 0.01f)
            {
                ApplyForwardThrust();
            }
            else if (_throttleInput < -0.01f)
            {
                ApplyBrakeOrReverse();
            }
            else
            {
                ApplyPassiveCoast();
            }

            ApplyLateralAssist();
            CurrentSpeed = _velocity.magnitude;
        }

        private void ApplyForwardThrust()
        {
            if (_vectorBoostHeld)
            {
                float forwardSpeed = Vector3.Dot(_velocity, transform.forward);
                if (forwardSpeed < vectorMaxSpeed)
                {
                    _velocity += transform.forward * (vectorAcceleration * Time.fixedDeltaTime);
                }

                ClampForwardOverspeed(vectorMaxSpeed, vectorReleaseDeceleration * 0.45f);
                return;
            }

            // Releasing Shift must actually leave VECTOR. Even while W is still held,
            // the flight computer bleeds excess VECTOR velocity back into the RIFTING envelope.
            if (_velocity.magnitude > riftingMaxSpeed + 0.25f)
            {
                ClampMagnitudeTowards(riftingMaxSpeed, vectorReleaseDeceleration);
                return;
            }

            float riftingForwardSpeed = Vector3.Dot(_velocity, transform.forward);
            if (riftingForwardSpeed < riftingMaxSpeed)
            {
                _velocity += transform.forward * (riftingAcceleration * Time.fixedDeltaTime);
            }

            ClampForwardOverspeed(riftingMaxSpeed, vectorReleaseDeceleration);
        }

        private void ApplyBrakeOrReverse()
        {
            Vector3 forward = transform.forward;
            float forwardSpeed = Vector3.Dot(_velocity, forward);
            Vector3 lateralVelocity = _velocity - forward * forwardSpeed;

            bool needsBraking = forwardSpeed > reverseEntrySpeed
                || lateralVelocity.magnitude > reverseLateralTolerance;

            if (needsBraking)
            {
                float speed = _velocity.magnitude;
                float drag = speed > riftingMaxSpeed
                    ? highSpeedBrakeDragPerSecond
                    : brakeDragPerSecond;

                // Active braking is deliberately exponential. It bites hard at
                // speed, but the last few units take progressively longer instead
                // of the ship being linearly dragged into an artificial zero.
                float retain = Mathf.Exp(-drag * Time.fixedDeltaTime);
                _velocity *= retain;
                return;
            }

            if (forwardSpeed > -reverseMaxSpeed)
            {
                _velocity -= forward * (reverseAcceleration * Time.fixedDeltaTime);
            }
        }

        private void ApplyPassiveCoast()
        {
            float speed = _velocity.magnitude;
            if (speed <= coastStopEpsilon)
            {
                _velocity = Vector3.zero;
                return;
            }

            // Exponential damping gives the ship the desired long inertial tail:
            // at high speed the absolute loss per second is strong, while close to
            // zero the same damping becomes progressively weaker instead of snapping.
            float retain = Mathf.Exp(-coastDragPerSecond * Time.fixedDeltaTime);
            _velocity *= retain;

            if (_velocity.magnitude <= coastStopEpsilon)
            {
                _velocity = Vector3.zero;
            }
        }

        private void ApplyLateralAssist()
        {
            if (lateralAssist <= 0f || _velocity.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 forward = transform.forward;
            float forwardSpeed = Vector3.Dot(_velocity, forward);
            Vector3 longitudinal = forward * forwardSpeed;
            Vector3 lateral = _velocity - longitudinal;
            float retain = Mathf.Exp(-lateralAssist * Time.fixedDeltaTime);
            _velocity = longitudinal + lateral * retain;
        }

        private void ApplyWarpThrust()
        {
            float speed = _velocity.magnitude;
            Vector3 currentDirection = speed > 0.05f ? _velocity / speed : transform.forward;
            float maxRadians = warpVelocityAlignment * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 direction = Vector3.RotateTowards(currentDirection, transform.forward, maxRadians, 0f).normalized;
            float nextSpeed = Mathf.MoveTowards(speed, warpMaxSpeed, warpAcceleration * Time.fixedDeltaTime);
            _velocity = direction * nextSpeed;
        }

        private void ApplyWarpExit()
        {
            float speed = _velocity.magnitude;
            bool braking = _throttleInput < -0.01f;
            float cruiseTarget = _vectorBoostHeld ? vectorMaxSpeed : riftingMaxSpeed;

            if (speed <= 0.0001f)
            {
                _warpExitActive = false;
                return;
            }

            float nextSpeed;

            if (braking)
            {
                // Emergency braking is also exponential. Once WARP speed has
                // collapsed back into the normal envelope, regular S braking takes
                // over and continues the long approach toward zero / reverse.
                float retain = Mathf.Exp(-warpEmergencyBrakeDragPerSecond * Time.fixedDeltaTime);
                nextSpeed = speed * retain;

                if (nextSpeed <= riftingMaxSpeed)
                {
                    _warpExitActive = false;
                }
            }
            else
            {
                if (speed <= cruiseTarget + 0.5f)
                {
                    _warpExitActive = false;
                    return;
                }

                float excess = Mathf.Max(0f, speed - cruiseTarget);
                float retainedExcess = excess * Mathf.Exp(-warpExitDragPerSecond * Time.fixedDeltaTime);
                nextSpeed = cruiseTarget + retainedExcess;

                if (nextSpeed <= cruiseTarget + 0.5f)
                {
                    _warpExitActive = false;
                }
            }

            _velocity = _velocity.normalized * Mathf.Max(0f, nextSpeed);
        }

        private void ClampMagnitudeTowards(float targetSpeed, float deceleration)
        {
            float speed = _velocity.magnitude;
            if (speed <= targetSpeed || speed <= 0.0001f)
            {
                return;
            }

            float nextSpeed = Mathf.MoveTowards(speed, targetSpeed, deceleration * Time.fixedDeltaTime);
            _velocity = _velocity.normalized * nextSpeed;
        }

        private void ClampForwardOverspeed(float targetForwardSpeed, float deceleration)
        {
            Vector3 forward = transform.forward;
            float forwardSpeed = Vector3.Dot(_velocity, forward);
            if (forwardSpeed <= targetForwardSpeed)
            {
                return;
            }

            Vector3 lateral = _velocity - forward * forwardSpeed;
            float correctedForward = Mathf.MoveTowards(
                forwardSpeed,
                targetForwardSpeed,
                deceleration * Time.fixedDeltaTime);
            _velocity = lateral + forward * correctedForward;
        }

        private void UpdateRotation()
        {
            bool hasInput = _pendingLookDelta.sqrMagnitude > 0.000001f;
            _angularRate += _pendingLookDelta * rotationImpulse;

            float turnMultiplier = _warpActive ? warpTurnRateMultiplier : 1f;
            float pitchLimit = maxPitchRate * turnMultiplier;
            float yawLimit = maxYawRate * turnMultiplier;
            _angularRate.x = Mathf.Clamp(_angularRate.x, -pitchLimit, pitchLimit);
            _angularRate.y = Mathf.Clamp(_angularRate.y, -yawLimit, yawLimit);

            float damping = hasInput ? turnInputDamping : turnReleaseDamping;
            float retain = Mathf.Exp(-damping * Time.fixedDeltaTime);
            _angularRate *= retain;

            Quaternion deltaRotation = Quaternion.Euler(
                _angularRate.x * Time.fixedDeltaTime,
                _angularRate.y * Time.fixedDeltaTime,
                0f);

            _rigidbody.MoveRotation(_rigidbody.rotation * deltaRotation);
            _pendingLookDelta = Vector2.zero;
        }

        private void UpdatePresentationState()
        {
            // One WARP intensity drives every presentation system. No consumer
            // gets its own speed threshold or temporal response; this prevents
            // dust/stars from stretching before the cockpit and HUD.
            float targetWarp = (_warpActive || _warpExitActive)
                ? Mathf.InverseLerp(warpVisualStartSpeed, warpVisualFullSpeed, CurrentSpeed)
                : 0f;

            float warpBlend = 1f - Mathf.Exp(-warpVisualResponse * Time.fixedDeltaTime);
            WarpIntensity = Mathf.Lerp(WarpIntensity, targetWarp, warpBlend);

            // Kept for compatibility with existing UI / debug code. Normal
            // RIFTING/VECTOR travel no longer owns any warp-like presentation.
            HighSpeedVisualIntensity = 0f;
            FlightEffectIntensity = WarpIntensity;
        }

        private void ApplyCurrentTuningRevision()
        {
            if (tuningRevision >= CurrentTuningRevision)
            {
                return;
            }

            // v5.6: unify WARP presentation and make every form of braking
            // asymptotic near zero. Normal flight remains visually neutral.
            riftingMaxSpeed = 85f;
            riftingAcceleration = 11.5f;
            coastDragPerSecond = 0.30f;
            coastStopEpsilon = 0.025f;
            vectorMaxSpeed = 200f;
            vectorAcceleration = 32f;
            vectorReleaseDeceleration = 30f;
            reverseMaxSpeed = 32f;
            reverseAcceleration = 8f;
            reverseEntrySpeed = 0.30f;
            reverseLateralTolerance = 0.50f;
            brakeDragPerSecond = 1.35f;
            highSpeedBrakeDragPerSecond = 2.25f;
            warpMaxSpeed = 1800f;
            warpAcceleration = 560f;
            warpExitDragPerSecond = 1.70f;
            warpEmergencyBrakeDragPerSecond = 2.60f;
            warpVisualStartSpeed = 140f;
            warpVisualFullSpeed = 520f;
            warpVisualResponse = 6.0f;
            highSpeedEffectStart = 135f;
            highSpeedEffectFull = 200f;
            presentationResponseTime = 0.28f;
            tuningRevision = CurrentTuningRevision;
        }

        private void ApplyMovement()
        {
            _rigidbody.MovePosition(_rigidbody.position + _velocity * Time.fixedDeltaTime);
        }
    }
}
