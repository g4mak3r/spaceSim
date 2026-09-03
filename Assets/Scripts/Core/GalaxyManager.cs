using System.Collections.Generic;
using SpaceSim.Systems;
using UnityEngine;

namespace SpaceSim.Core
{
    public sealed class GalaxyManager : MonoBehaviour
    {
        public static GalaxyManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private StarSystem systemPrefab;
        [SerializeField] private GameObject sunPrefab;
        [SerializeField] private GameObject planetPrefab;

        [Header("Generation")]
        [SerializeField, Min(0)] private int systemCount = 50;
        [SerializeField, Min(1000f)] private float galaxyRadius = 20000f;
        [SerializeField, Min(0.01f)] private float systemScale = 50f;

        private readonly List<StarSystem> _generatedSystems = new List<StarSystem>();

        public IReadOnlyList<StarSystem> GeneratedSystems => _generatedSystems;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            GenerateGalaxy();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void GenerateGalaxy()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            _generatedSystems.Clear();

            for (int i = 0; i < systemCount; i++)
            {
                Vector3 position = Random.onUnitSphere * Random.Range(1000f, galaxyRadius);
                StarSystem system = Instantiate(systemPrefab, position, Quaternion.identity, transform);
                system.Initialize(systemScale, 1, 6, sunPrefab, planetPrefab);
                _generatedSystems.Add(system);
            }
        }

        public StarSystem GetNearestSystem(Vector3 position)
        {
            StarSystem nearest = null;
            float nearestDistanceSquared = float.MaxValue;

            foreach (StarSystem system in _generatedSystems)
            {
                if (system == null)
                {
                    continue;
                }

                float distanceSquared = (system.transform.position - position).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearest = system;
            }

            return nearest;
        }

        private bool ValidateConfiguration()
        {
            if (systemPrefab != null && sunPrefab != null && planetPrefab != null)
            {
                return true;
            }

            Debug.LogError("GalaxyManager is missing one or more generation prefabs.", this);
            return false;
        }
    }
}
