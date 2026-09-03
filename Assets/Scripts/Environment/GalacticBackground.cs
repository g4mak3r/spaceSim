using UnityEngine;

namespace SpaceSim.Environment
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class GalacticBackground : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform targetCamera;
        [SerializeField, Min(1)] private int starCount = 6000;
        [SerializeField, Min(1f)] private float fieldRadius = 2500f;

        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            ConfigureParticleSystem();
            GenerateStars();
        }

        private void LateUpdate()
        {
            if (targetCamera != null)
            {
                transform.position = targetCamera.position;
            }
        }

        private void ConfigureParticleSystem()
        {
            ParticleSystem.MainModule main = _particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = starCount;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            emission.enabled = false;
        }

        private void GenerateStars()
        {
            var particles = new ParticleSystem.Particle[starCount];

            for (int i = 0; i < particles.Length; i++)
            {
                Vector3 direction = Random.onUnitSphere;
                float noise = Mathf.PerlinNoise(direction.x * 1.8f + 12.3f, direction.y * 1.8f + 42.7f);
                float radius = fieldRadius * Mathf.Lerp(0.92f, 1f, noise);

                particles[i].position = direction * radius;
                particles[i].startSize = GenerateStarSize();
                particles[i].startColor = GenerateStarColor();
                particles[i].startLifetime = Mathf.Infinity;
                particles[i].remainingLifetime = Mathf.Infinity;
            }

            _particleSystem.SetParticles(particles, particles.Length);
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
