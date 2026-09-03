using SpaceSim.Player;
using UnityEngine;

namespace SpaceSim.Environment
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class SpaceDustField : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShipMotor shipMotor;

        [Header("World-space Field")]
        [SerializeField, Range(128, 2600)] private int nearCount = 1600;
        [SerializeField, Min(10f)] private float nearHalfExtent = 90f;
        [SerializeField, Range(256, 5000)] private int farCount = 2600;
        [SerializeField, Min(20f)] private float farHalfExtent = 260f;

        [Header("Appearance")]
        [SerializeField, Min(0.001f)] private float nearParticleMinSize = 0.035f;
        [SerializeField, Min(0.001f)] private float nearParticleMaxSize = 0.105f;
        [SerializeField, Min(0.001f)] private float farParticleMinSize = 0.016f;
        [SerializeField, Min(0.001f)] private float farParticleMaxSize = 0.052f;
        [SerializeField, Range(0f, 1f)] private float nearMinAlpha = 0.22f;
        [SerializeField, Range(0f, 1f)] private float nearMaxAlpha = 0.72f;
        [SerializeField, Range(0f, 1f)] private float farMinAlpha = 0.10f;
        [SerializeField, Range(0f, 1f)] private float farMaxAlpha = 0.40f;

        [Header("WARP-only Readability")]
        [SerializeField, Min(0f)] private float warpStreakVelocityScale = 0.34f;
        [SerializeField, Min(0f)] private float cruiseStreakLength = 0.05f;
        [SerializeField, Min(0f)] private float warpStreakLength = 4.2f;
        [SerializeField, Min(0.0001f)] private float fieldMovementThreshold = 0.001f;

        [SerializeField, HideInInspector] private int visualTuningRevision;
        private const int CurrentVisualTuningRevision = 2;

        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;
        private ParticleSystemRenderer _renderer;
        private Vector3 _lastShipPosition;
        private Vector3 _lastVisualVelocity;

        private Transform ShipTransform => shipMotor != null ? shipMotor.transform : transform;
        private int TotalCount => nearCount + farCount;

        private void Awake()
        {
            ApplyVisualTuningRevision();
            _particleSystem = GetComponent<ParticleSystem>();
            _renderer = GetComponent<ParticleSystemRenderer>();

            if (shipMotor == null)
            {
                shipMotor = GetComponentInParent<ShipMotor>();
            }

            farHalfExtent = Mathf.Max(farHalfExtent, nearHalfExtent + 1f);
            _lastShipPosition = ShipTransform.position;

            ConfigureParticleSystem();
            CreateField();
        }

        private void OnValidate()
        {
            ApplyVisualTuningRevision();
        }

        private void LateUpdate()
        {
            if (_particles == null || _particles.Length == 0)
            {
                return;
            }

            Vector3 shipPosition = ShipTransform.position;
            Vector3 displacement = shipPosition - _lastShipPosition;
            bool moved = displacement.sqrMagnitude > fieldMovementThreshold * fieldMovementThreshold;
            bool particlesChanged = false;

            if (moved)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    float extent = i < nearCount ? nearHalfExtent : farHalfExtent;
                    Vector3 wrapped = WrapAroundShip(_particles[i].position, shipPosition, extent);

                    if ((wrapped - _particles[i].position).sqrMagnitude > 0.000001f)
                    {
                        _particles[i].position = wrapped;
                        particlesChanged = true;
                    }
                }
            }

            Vector3 motion = shipMotor != null
                ? shipMotor.Velocity
                : displacement / Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = motion.magnitude;
            float warpIntensity = shipMotor != null
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shipMotor.WarpIntensity))
                : 0f;

            Vector3 visualVelocity = warpIntensity > 0.002f && speed > 0.01f
                ? -motion.normalized * Mathf.Max(1f, speed * warpStreakVelocityScale * warpIntensity)
                : Vector3.zero;

            if ((visualVelocity - _lastVisualVelocity).sqrMagnitude > 0.0025f)
            {
                for (int i = 0; i < _particles.Length; i++)
                {
                    _particles[i].velocity = visualVelocity;
                }

                _lastVisualVelocity = visualVelocity;
                particlesChanged = true;
            }

            UpdateRenderer(warpIntensity);

            if (particlesChanged)
            {
                _particleSystem.SetParticles(_particles, _particles.Length);
            }

            _lastShipPosition = shipPosition;
        }


        private void ApplyVisualTuningRevision()
        {
            if (visualTuningRevision >= CurrentVisualTuningRevision)
            {
                return;
            }

            warpStreakVelocityScale = 0.34f;
            cruiseStreakLength = 0.05f;
            warpStreakLength = 4.2f;
            visualTuningRevision = CurrentVisualTuningRevision;
        }

        private Vector3 WrapAroundShip(Vector3 position, Vector3 center, float halfExtent)
        {
            Vector3 relative = position - center;
            relative.x = WrapCoordinate(relative.x, halfExtent);
            relative.y = WrapCoordinate(relative.y, halfExtent);
            relative.z = WrapCoordinate(relative.z, halfExtent);
            return center + relative;
        }

        private static float WrapCoordinate(float coordinate, float halfExtent)
        {
            float size = halfExtent * 2f;
            return Mathf.Repeat(coordinate + halfExtent, size) - halfExtent;
        }

        private void UpdateRenderer(float warpIntensity)
        {
            if (_renderer == null)
            {
                return;
            }

            bool shouldStretch = warpIntensity > 0.002f;
            _renderer.renderMode = shouldStretch
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            _renderer.velocityScale = Mathf.Lerp(0.04f, 0.10f, warpIntensity);
            _renderer.lengthScale = Mathf.Lerp(
                cruiseStreakLength,
                warpStreakLength,
                warpIntensity);
        }

        private void ConfigureParticleSystem()
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = TotalCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.simulationSpeed = 0f;
            main.startLifetime = Mathf.Infinity;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _particleSystem.shape;
            shape.enabled = false;

            if (_renderer != null)
            {
                _renderer.renderMode = ParticleSystemRenderMode.Billboard;
                _renderer.velocityScale = 0.07f;
                _renderer.lengthScale = cruiseStreakLength;

                float boundsSize = farHalfExtent * 2.6f;
                _renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * boundsSize);
            }
        }

        private void CreateField()
        {
            _particles = new ParticleSystem.Particle[TotalCount];
            Vector3 center = ShipTransform.position;

            for (int i = 0; i < _particles.Length; i++)
            {
                bool near = i < nearCount;
                int sequenceIndex = near ? i + 1 : i - nearCount + 1;
                float extent = near ? nearHalfExtent : farHalfExtent;

                _particles[i].position = center + HaltonPosition(sequenceIndex, extent);
                _particles[i].velocity = Vector3.zero;
                _particles[i].startSize = near
                    ? DeterministicRange(sequenceIndex, nearParticleMinSize, nearParticleMaxSize, 7)
                    : DeterministicRange(sequenceIndex, farParticleMinSize, farParticleMaxSize, 11);
                _particles[i].startColor = CreateColor(sequenceIndex, near);
                _particles[i].startLifetime = Mathf.Infinity;
                _particles[i].remainingLifetime = Mathf.Infinity;
            }

            _particleSystem.SetParticles(_particles, _particles.Length);
            _particleSystem.Play(false);
        }

        private static Vector3 HaltonPosition(int index, float extent)
        {
            return new Vector3(
                Halton(index, 2) * 2f - 1f,
                Halton(index, 3) * 2f - 1f,
                Halton(index, 5) * 2f - 1f) * extent;
        }

        private Color CreateColor(int index, bool near)
        {
            float brightness = DeterministicRange(index, near ? 0.76f : 0.62f, 1f, 13);
            float alpha = near
                ? DeterministicRange(index, nearMinAlpha, nearMaxAlpha, 17)
                : DeterministicRange(index, farMinAlpha, farMaxAlpha, 19);

            return new Color(brightness, brightness, brightness, alpha);
        }

        private static float DeterministicRange(int index, float min, float max, int salt)
        {
            float value = Halton(index + salt * 37, salt);
            return Mathf.Lerp(min, max, value);
        }

        private static float Halton(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / radix;

            while (index > 0)
            {
                result += fraction * (index % radix);
                index /= radix;
                fraction /= radix;
            }

            return result;
        }
    }
}
