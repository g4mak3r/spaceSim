using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceSim.Physics
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleMagnet : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Magnet")]
        [SerializeField, Min(0f)] private float magnetStrength = 10f;
        [SerializeField, Min(0f)] private float detectionRadius = 15f;
        [SerializeField, Min(0f)] private float depthFromCamera = 10f;

        [Header("Swirl")]
        [SerializeField, Range(0f, 2f)] private float orbitStrength = 0.8f;
        [SerializeField, Range(0f, 5f)] private float noiseStrength = 1.5f;

        private ParticleSystem _particleSystem;
        private ParticleSystem.Particle[] _particles;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || Mouse.current == null)
            {
                return;
            }

            Vector3 screenPosition = Mouse.current.position.ReadValue();
            screenPosition.z = depthFromCamera;
            Vector3 targetPosition = targetCamera.ScreenToWorldPoint(screenPosition);

            int aliveCount = _particleSystem.GetParticles(_particles);
            for (int i = 0; i < aliveCount; i++)
            {
                Vector3 toTarget = targetPosition - _particles[i].position;
                float distance = toTarget.magnitude;

                if (distance >= detectionRadius || distance <= Mathf.Epsilon)
                {
                    continue;
                }

                Vector3 direction = toTarget / distance;
                Vector3 orbitDirection = Vector3.Cross(direction, Vector3.forward).normalized;
                float seed = i * 0.1f;
                Vector3 noise = new Vector3(
                    Mathf.PerlinNoise(seed, Time.time) - 0.5f,
                    Mathf.PerlinNoise(seed + 1f, Time.time) - 0.5f,
                    0f);

                Vector3 desiredVelocity =
                    (direction + orbitDirection * orbitStrength + noise * noiseStrength) * magnetStrength;

                _particles[i].velocity = Vector3.Lerp(
                    _particles[i].velocity,
                    desiredVelocity,
                    1f - Mathf.Exp(-4f * Time.deltaTime));
            }

            _particleSystem.SetParticles(_particles, aliveCount);
        }
    }
}
