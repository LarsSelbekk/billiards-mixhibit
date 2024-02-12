#nullable enable

namespace MRIoT
{
    public enum PocketEnum
    {
        SouthWest,
        West,
        NorthWest,
        NorthEast,
        East,
        SouthEast,
    }
    
    public struct PocketDefinition
    {
        // Numbered and named clock-wise from the pocket closest to the left of the breaker
        public static readonly PocketDefinition[] PocketDefinitions =
        {
            new((int)PocketEnum.SouthWest, "South-West"),
            new((int)PocketEnum.West, "West"),
            new((int)PocketEnum.NorthWest, "North-West"),
            new((int)PocketEnum.NorthEast, "North-East"),
            new((int)PocketEnum.East, "East"),
            new((int)PocketEnum.SouthEast, "South-East"),
        };

        public int Index { get; private set; }
        public string Name { get; private set; }

        private PocketDefinition(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }

}
