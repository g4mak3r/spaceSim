using System.Collections.Generic;
using UnityEngine;

namespace SpaceSim.Physics
{
    public sealed class GravityBody : MonoBehaviour
    {
        private const float GravitationalConstant = 0.1f;
        private const float MinDistanceSquared = 0.1f;
        private const float MaxDistanceSquared = 25_000_000f;

        private static readonly HashSet<GravityBody> Bodies = new HashSet<GravityBody>();

        [Header("Physics Settings")]
        [SerializeField, Min(0f)] private float mass = 10f;
        [SerializeField] private bool isStatic;

        public Vector3 Velocity { get; set; }
        public static IReadOnlyCollection<GravityBody> AllBodies => Bodies;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            Bodies.Clear();
        }

        private void OnEnable()
        {
            Bodies.Add(this);
        }

        private void OnDisable()
        {
            Bodies.Remove(this);
        }

        private void FixedUpdate()
        {
            if (isStatic)
            {
                return;
            }

            Vector3 acceleration = CalculateAcceleration();
            Velocity += acceleration * Time.fixedDeltaTime;
            transform.position += Velocity * Time.fixedDeltaTime;
        }

        private Vector3 CalculateAcceleration()
        {
            Vector3 acceleration = Vector3.zero;

            foreach (GravityBody other in Bodies)
            {
                if (other == null || other == this)
                {
                    continue;
                }

                Vector3 displacement = other.transform.position - transform.position;
                float distanceSquared = displacement.sqrMagnitude;

                if (distanceSquared < MinDistanceSquared || distanceSquared > MaxDistanceSquared)
                {
                    continue;
                }

                float inverseDistance = 1f / Mathf.Sqrt(distanceSquared);
                float accelerationMagnitude = GravitationalConstant * other.mass / distanceSquared;
                acceleration += displacement * inverseDistance * accelerationMagnitude;
            }

            return acceleration;
        }
    }
}
