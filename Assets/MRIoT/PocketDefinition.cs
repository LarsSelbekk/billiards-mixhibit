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
            new(PocketEnum.SouthWest, "South-West"),
            new(PocketEnum.West, "West"),
            new(PocketEnum.NorthWest, "North-West"),
            new(PocketEnum.NorthEast, "North-East"),
            new(PocketEnum.East, "East"),
            new(PocketEnum.SouthEast, "South-East"),
        };

        public PocketEnum Enum { get; private set; }
        public string Name { get; private set; }

        private PocketDefinition(PocketEnum @enum, string name)
        {
            Enum = @enum;
            Name = name;
        }
    }
}
