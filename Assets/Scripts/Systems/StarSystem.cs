using System.Collections.Generic;
using SpaceSim.Physics;
using SpaceSim.Player;
using SpaceSim.UI;
using SpaceSim.Utils;
using UnityEngine;

namespace SpaceSim.Systems
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class StarSystem : MonoBehaviour
    {
        private sealed class OrbitingPlanet
        {
            public Transform Transform;
            public float Radius;
            public float Angle;
            public float AngularSpeed;
            public Quaternion PlaneRotation;
        }

        [Header("Config")]
        [SerializeField] private Transform rootTransform;

        private readonly List<OrbitingPlanet> _planets = new List<OrbitingPlanet>();

        public string SystemName { get; private set; }
        public Transform SunTransform { get; private set; }

        public void Initialize(
            int seed,
            float scaleMultiplier,
            int minPlanets,
            int maxPlanets,
            GameObject sunPrefab,
            GameObject planetPrefab)
        {
            var random = new System.Random(seed);

            SystemName = StarNameGenerator.Generate(seed);
            gameObject.name = $"System_{SystemName}";

            if (rootTransform == null)
            {
                rootTransform = transform;
            }

            _planets.Clear();
            float outerOrbit = GenerateContent(random, scaleMultiplier, minPlanets, maxPlanets, sunPrefab, planetPrefab);
            ConfigureTrigger(outerOrbit, scaleMultiplier);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < _planets.Count; i++)
            {
                OrbitingPlanet planet = _planets[i];
                planet.Angle = Mathf.Repeat(planet.Angle + planet.AngularSpeed * deltaTime, 360f);

                Vector3 orbitPosition = Quaternion.Euler(0f, planet.Angle, 0f)
                    * Vector3.forward
                    * planet.Radius;

                planet.Transform.localPosition = planet.PlaneRotation * orbitPosition;
            }
        }

        private float GenerateContent(
            System.Random random,
            float scale,
            int minPlanets,
            int maxPlanets,
            GameObject sunPrefab,
            GameObject planetPrefab)
        {
            GameObject sun = Instantiate(sunPrefab, rootTransform);
            sun.name = "Sun";
            SunTransform = sun.transform;
            SunTransform.localPosition = Vector3.zero;

            DisableGravitySimulation(sun);

            float sunScale = RandomRange(random, 7f, 13f) * scale;
            sun.transform.localScale = Vector3.one * sunScale;

            int planetCount = random.Next(minPlanets, maxPlanets + 1);
            float currentOrbit = 42f * scale + sunScale * 0.55f;

            for (int i = 0; i < planetCount; i++)
            {
                currentOrbit += RandomRange(random, 28f, 48f) * scale;
                CreatePlanet(random, planetPrefab, i, currentOrbit, scale);
            }

            return currentOrbit;
        }

        private void CreatePlanet(
            System.Random random,
            GameObject prefab,
            int index,
            float orbitRadius,
            float scale)
        {
            GameObject planetObject = Instantiate(prefab, rootTransform);
            planetObject.name = $"Planet_{index + 1}";
            planetObject.transform.localScale = Vector3.one * RandomRange(random, 0.8f, 2.8f) * scale;

            DisableGravitySimulation(planetObject);

            var orbit = new OrbitingPlanet
            {
                Transform = planetObject.transform,
                Radius = orbitRadius,
                Angle = RandomRange(random, 0f, 360f),
                AngularSpeed = RandomRange(random, 0.35f, 1.15f) / Mathf.Max(0.25f, scale * 0.15f),
                PlaneRotation = Quaternion.Euler(
                    RandomRange(random, -10f, 10f),
                    0f,
                    RandomRange(random, -10f, 10f))
            };

            Vector3 initialPosition = Quaternion.Euler(0f, orbit.Angle, 0f)
                * Vector3.forward
                * orbit.Radius;
            orbit.Transform.localPosition = orbit.PlaneRotation * initialPosition;

            _planets.Add(orbit);
        }

        private void ConfigureTrigger(float outerOrbit, float scale)
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.radius = outerOrbit + 35f * scale;
        }

        private static void DisableGravitySimulation(GameObject body)
        {
            if (body.TryGetComponent(out GravityBody gravityBody))
            {
                gravityBody.enabled = false;
            }
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.TryGetComponent(out ShipController _))
            {
                return;
            }

            ShipHUD.Instance?.UpdateLocationName(SystemName);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player") && !other.TryGetComponent(out ShipController _))
            {
                return;
            }

            ShipHUD.Instance?.UpdateLocationName("DEEP SPACE");
        }
    }
}
