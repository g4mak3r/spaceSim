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

        public static string Generate(int seed)
        {
            var random = new System.Random(seed);
            string prefix = Prefixes[random.Next(Prefixes.Length)];
            string root = Roots[random.Next(Roots.Length)];
            int number = random.Next(100, 9999);

            return $"{prefix}-{root}-{number}";
        }
    }
}
