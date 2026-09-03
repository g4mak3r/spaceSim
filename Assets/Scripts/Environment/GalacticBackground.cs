using SpaceSim.Player;
using UnityEngine;

namespace SpaceSim.Environment
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class GalacticBackground : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform targetCamera;
        [SerializeField] private ShipMotor shipMotor;
        [SerializeField, Min(1)] private int starCount = 6000;
        [SerializeField, Min(1f)] private float fieldRadius = 2500f;

        [Header("WARP-only Star Pull")]
        [SerializeField, Min(0f)] private float warpRadialVelocity = 120f;
        [SerializeField, Min(0f)] private float warpLengthScale = 5.5f;
        [SerializeField, Range(0f, 1f)] private float warpVelocityScale = 0.085f;

        private ParticleSystem _particleSystem;
        private ParticleSystemRenderer _renderer;
        private ParticleSystem.Particle[] _particles;
        private float _lastWarpIntensity = -1f;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _renderer = GetComponent<ParticleSystemRenderer>();

            if (shipMotor == null)
            {
                shipMotor = FindFirstObjectByType<ShipMotor>();
            }

            ConfigureParticleSystem();
            GenerateStars();
        }

        private void LateUpdate()
        {
            if (targetCamera != null)
            {
                transform.position = targetCamera.position;
            }

            UpdateWarpPresentation();
        }

        private void ConfigureParticleSystem()
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = starCount;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.simulationSpeed = 0f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            emission.enabled = false;

            if (_renderer != null)
            {
                _renderer.renderMode = ParticleSystemRenderMode.Billboard;
                _renderer.velocityScale = 0f;
                _renderer.lengthScale = 0f;
                _renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * fieldRadius * 2.4f);
            }
        }

        private void GenerateStars()
        {
            _particles = new ParticleSystem.Particle[starCount];

            for (int i = 0; i < _particles.Length; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                float noise = Mathf.PerlinNoise(direction.x * 1.8f + 12.3f, direction.y * 1.8f + 42.7f);
                float radius = fieldRadius * Mathf.Lerp(0.92f, 1f, noise);

                _particles[i].position = direction * radius;
                _particles[i].velocity = Vector3.zero;
                _particles[i].startSize = GenerateStarSize();
                _particles[i].startColor = GenerateStarColor();
                _particles[i].startLifetime = Mathf.Infinity;
                _particles[i].remainingLifetime = Mathf.Infinity;
            }

            _particleSystem.SetParticles(_particles, _particles.Length);
        }

        private void UpdateWarpPresentation()
        {
            if (_particles == null || _renderer == null)
            {
                return;
            }

            float warp = shipMotor != null
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shipMotor.WarpIntensity))
                : 0f;

            if (Mathf.Abs(warp - _lastWarpIntensity) < 0.002f)
            {
                return;
            }

            bool active = warp > 0.002f;
            _renderer.renderMode = active
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            _renderer.velocityScale = Mathf.Lerp(0f, warpVelocityScale, warp);
            _renderer.lengthScale = Mathf.Lerp(0f, warpLengthScale, warp);

            // Local-space stars radiate out from the camera centre while WARP is
            // active. Particle simulation is paused, so velocity is purely a render
            // vector for stretched billboards rather than actual star movement.
            for (int i = 0; i < _particles.Length; i++)
            {
                Vector3 radial = _particles[i].position.sqrMagnitude > 0.0001f
                    ? _particles[i].position.normalized
                    : Vector3.forward;
                _particles[i].velocity = radial * (warpRadialVelocity * warp);
            }

            _particleSystem.SetParticles(_particles, _particles.Length);
            _lastWarpIntensity = warp;
        }

        private static float GenerateStarSize()
        {
            float roll = Random.value;

            if (roll < 0.75f)
            {
                return Random.Range(0.0015f, 0.0035f);
            }

            if (roll < 0.95f)
            {
                return Random.Range(0.0035f, 0.007f);
            }

            return Random.Range(0.01f, 0.03f);
        }

        private static Color GenerateStarColor()
        {
            float roll = Random.value;
            Color color;

            if (roll < 0.78f)
            {
                color = new Color(0.75f, 0.75f, 0.75f);
            }
            else if (roll < 0.95f)
            {
                color = new Color(0.9f, 0.9f, 0.9f);
            }
            else
            {
                float colorType = Random.value;
                color = colorType < 0.33f
                    ? new Color(0.6f, 0.75f, 1f)
                    : colorType < 0.66f
                        ? new Color(1f, 0.55f, 0.35f)
                        : Color.white;
            }

            color.a = Random.Range(0.01f, 0.05f);
            return color;
        }
    }
}
