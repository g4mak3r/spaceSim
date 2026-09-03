using SpaceSim.Player;
using SpaceSim.UI;
using SpaceSim.Utils;
using UnityEngine;

namespace SpaceSim.Systems
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class StarSystem : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private Transform rootTransform;

        public string SystemName { get; private set; }
        public Transform SunTransform { get; private set; }

        public void Initialize(
            float scaleMultiplier,
            int minPlanets,
            int maxPlanets,
            GameObject sunPrefab,
            GameObject planetPrefab)
        {
            SystemName = StarNameGenerator.Generate();
            gameObject.name = $"System_{SystemName}";

            if (rootTransform == null)
            {
                rootTransform = transform;
            }

            GenerateContent(scaleMultiplier, minPlanets, maxPlanets, sunPrefab, planetPrefab);
            ConfigureTrigger(scaleMultiplier, maxPlanets);
        }

        private void GenerateContent(
            float scale,
            int minPlanets,
            int maxPlanets,
            GameObject sunPrefab,
            GameObject planetPrefab)
        {
            GameObject sun = Instantiate(sunPrefab, rootTransform);
            sun.name = "Sun";
            SunTransform = sun.transform;

            float sunScale = Random.Range(10f, 20f) * scale;
            sun.transform.localScale = Vector3.one * sunScale;

            int planetCount = Random.Range(minPlanets, maxPlanets + 1);
            float currentOrbit = 100f * scale + sunScale * 0.5f;

            for (int i = 0; i < planetCount; i++)
            {
                currentOrbit += 80f * scale;
                CreatePlanet(planetPrefab, i, currentOrbit, scale);
            }
        }

        private void CreatePlanet(GameObject prefab, int index, float orbitRadius, float scale)
        {
            float angle = Random.Range(0f, 360f);
            Vector3 localPosition = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * orbitRadius;

            GameObject planet = Instantiate(prefab, rootTransform);
            planet.name = $"Planet_{index + 1}";
            planet.transform.localPosition = localPosition;
            planet.transform.localScale = Vector3.one * Random.Range(1f, 4f) * scale;
        }

        private void ConfigureTrigger(float scale, int maxPlanets)
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.zero;
            trigger.radius = maxPlanets * 150f * scale;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.TryGetComponent(out ShipController _))
            {
                return;
            }

            ShipHUD.Instance?.UpdateLocationName(SystemName);
        }
    }
}
