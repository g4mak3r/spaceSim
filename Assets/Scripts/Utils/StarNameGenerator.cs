using UnityEngine;

namespace SpaceSim.Utils
{
    public static class StarNameGenerator
    {
        private static readonly string[] Prefixes =
        {
            "HD", "RX", "Kepler", "Zeta", "Nova", "Tau", "Sigma", "Epsilon", "Omicron"
        };

        private static readonly string[] Roots =
        {
            "Orion", "Draco", "Lyra", "Aquila", "Cygnus", "Vega", "Altair", "Centauri"
        };

        public static string Generate()
        {
            string prefix = Prefixes[Random.Range(0, Prefixes.Length)];
            string root = Roots[Random.Range(0, Roots.Length)];
            int number = Random.Range(100, 9999);

            return $"{prefix}-{root}-{number}";
        }
    }
}
