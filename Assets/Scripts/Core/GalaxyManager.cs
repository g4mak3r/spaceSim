using System.Collections.Generic;
using SpaceSim.Systems;
using UnityEngine;

namespace SpaceSim.Core
{
    public sealed class GalaxyManager : MonoBehaviour
    {
        private readonly struct SystemCandidate
        {
            public readonly Vector3Int Cell;
            public readonly Vector3 Position;
            public readonly int Seed;
            public readonly float DistanceSquared;

            public SystemCandidate(Vector3Int cell, Vector3 position, int seed, float distanceSquared)
            {
                Cell = cell;
                Position = position;
                Seed = seed;
                DistanceSquared = distanceSquared;
            }
        }

        public static GalaxyManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform player;

        [Header("Prefabs")]
        [SerializeField] private StarSystem systemPrefab;
        [SerializeField] private GameObject sunPrefab;
        [SerializeField] private GameObject planetPrefab;

        [Header("Streaming")]
        [Tooltip("Maximum number of fully instantiated systems around the player.")]
        [SerializeField, Range(1, 12)] private int systemCount = 5;
        [SerializeField] private int galaxySeed = 1701;
        [SerializeField, Min(500f)] private float cellSize = 5200f;
        [SerializeField, Range(0.05f, 1f)] private float systemChance = 0.42f;
        [SerializeField, Min(500f)] private float loadRadius = 8500f;
        [SerializeField, Min(500f)] private float unloadRadius = 11000f;
        [SerializeField, Min(0.1f)] private float refreshInterval = 0.6f;

        [Header("System Generation")]
        [SerializeField, Min(0.01f)] private float systemScale = 8f;
        [SerializeField, Range(1, 8)] private int minPlanets = 1;
        [SerializeField, Range(1, 8)] private int maxPlanets = 4;


        private readonly Dictionary<Vector3Int, StarSystem> _loadedSystems = new Dictionary<Vector3Int, StarSystem>();
        private readonly List<StarSystem> _generatedSystems = new List<StarSystem>();
        private readonly List<SystemCandidate> _candidates = new List<SystemCandidate>();
        private readonly List<Vector3Int> _cellsToUnload = new List<Vector3Int>();
        private float _nextRefreshTime;
        private Vector3Int _lastPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

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
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            if (player == null)
            {
                Debug.LogError("GalaxyManager could not find a Player transform.", this);
                enabled = false;
                return;
            }

            RefreshStreaming(true);
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            Vector3Int currentCell = WorldToCell(player.position);
            bool changedCell = currentCell != _lastPlayerCell;

            if (changedCell || Time.unscaledTime >= _nextRefreshTime)
            {
                RefreshStreaming(changedCell);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public StarSystem GetNearestSystem(Vector3 position)
        {
            StarSystem nearest = null;
            float nearestDistanceSquared = float.MaxValue;

            for (int i = 0; i < _generatedSystems.Count; i++)
            {
                StarSystem system = _generatedSystems[i];
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

        private void RefreshStreaming(bool forceCandidateRefresh)
        {
            Vector3Int currentCell = WorldToCell(player.position);
            _lastPlayerCell = currentCell;
            _nextRefreshTime = Time.unscaledTime + refreshInterval;

            UnloadDistantSystems();

            if (forceCandidateRefresh || _loadedSystems.Count < systemCount)
            {
                BuildCandidates(currentCell);
                SpawnNearestCandidates();
            }

            RebuildGeneratedList();
        }

        private void BuildCandidates(Vector3Int centerCell)
        {
            _candidates.Clear();
            int cellRadius = Mathf.Max(1, Mathf.CeilToInt(loadRadius / cellSize));
            float loadRadiusSquared = loadRadius * loadRadius;

            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        Vector3Int cell = centerCell + new Vector3Int(x, y, z);
                        if (_loadedSystems.ContainsKey(cell))
                        {
                            continue;
                        }

                        int seed = HashCell(cell);
                        var random = new System.Random(seed);
                        if (random.NextDouble() > systemChance)
                        {
                            continue;
                        }

                        Vector3 position = CellToSystemPosition(cell, random);
                        float distanceSquared = (position - player.position).sqrMagnitude;
                        if (distanceSquared > loadRadiusSquared)
                        {
                            continue;
                        }

                        _candidates.Add(new SystemCandidate(cell, position, seed, distanceSquared));
                    }
                }
            }

            _candidates.Sort((a, b) => a.DistanceSquared.CompareTo(b.DistanceSquared));
        }

        private void SpawnNearestCandidates()
        {
            int freeSlots = systemCount - _loadedSystems.Count;

            for (int i = 0; i < _candidates.Count && freeSlots > 0; i++)
            {
                SystemCandidate candidate = _candidates[i];
                StarSystem system = Instantiate(systemPrefab, candidate.Position, Quaternion.identity, transform);
                system.Initialize(
                    candidate.Seed,
                    systemScale,
                    Mathf.Min(minPlanets, maxPlanets),
                    Mathf.Max(minPlanets, maxPlanets),
                    sunPrefab,
                    planetPrefab);

                _loadedSystems.Add(candidate.Cell, system);
                freeSlots--;
            }
        }

        private void UnloadDistantSystems()
        {
            float unloadRadiusSquared = Mathf.Max(unloadRadius, loadRadius + cellSize * 0.25f);
            unloadRadiusSquared *= unloadRadiusSquared;
            _cellsToUnload.Clear();

            foreach (KeyValuePair<Vector3Int, StarSystem> pair in _loadedSystems)
            {
                StarSystem system = pair.Value;
                if (system == null || (system.transform.position - player.position).sqrMagnitude > unloadRadiusSquared)
                {
                    _cellsToUnload.Add(pair.Key);
                }
            }

            for (int i = 0; i < _cellsToUnload.Count; i++)
            {
                Vector3Int cell = _cellsToUnload[i];
                if (_loadedSystems.TryGetValue(cell, out StarSystem system) && system != null)
                {
                    Destroy(system.gameObject);
                }

                _loadedSystems.Remove(cell);
            }
        }

        private void RebuildGeneratedList()
        {
            _generatedSystems.Clear();

            foreach (StarSystem system in _loadedSystems.Values)
            {
                if (system != null)
                {
                    _generatedSystems.Add(system);
                }
            }
        }

        private Vector3Int WorldToCell(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize),
                Mathf.FloorToInt(position.z / cellSize));
        }

        private Vector3 CellToSystemPosition(Vector3Int cell, System.Random random)
        {
            float margin = cellSize * 0.18f;
            float usableSize = cellSize - margin * 2f;

            return new Vector3(
                cell.x * cellSize + margin + (float)random.NextDouble() * usableSize,
                cell.y * cellSize + margin + (float)random.NextDouble() * usableSize,
                cell.z * cellSize + margin + (float)random.NextDouble() * usableSize);
        }

        private int HashCell(Vector3Int cell)
        {
            unchecked
            {
                int hash = galaxySeed;
                hash = hash * 397 ^ cell.x;
                hash = hash * 397 ^ cell.y;
                hash = hash * 397 ^ cell.z;
                return hash;
            }
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
