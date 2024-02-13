#nullable enable

using UnityEngine;

namespace MRIoT
{
    public enum BallEnum
    {
        White,
        SolidYellow,
        SolidBlue,
        SolidRed,
        SolidPurple,
        SolidOrange,
        SolidGreen,
        SolidMaroon,
        Black,
        StripedYellow,
        StripedBlue,
        StripedRed,
        StripedPurple,
        StripedOrange,
        StripedGreen,
        StripedMaroon,
    }

    public struct BallDefinition
    {
        public static readonly BallDefinition[] BallDefinitions =
        {
            new(BallEnum.White, "White", Color.white, false),
            new(BallEnum.SolidYellow, "SolidYellow", Color.yellow, false),
            new(BallEnum.SolidBlue, "SolidBlue", Color.blue, false),
            new(BallEnum.SolidRed, "SolidRed", Color.red, false),
            new(BallEnum.SolidPurple, "SolidPurple", new Color(0.5f, 0f, 0.5f), false),
            new(BallEnum.SolidOrange, "SolidOrange", new Color(1f, 0.5f, 0f), false),
            new(BallEnum.SolidGreen, "SolidGreen", Color.green, false),
            // new(BallEnum.SolidMaroon, "SolidMaroon", new Color(0.5f, 0f, 0f), false),
            new(BallEnum.SolidMaroon, "SolidMaroon", new Color(1f, 0.45f, 0.55f), false),
            new(BallEnum.Black, "Black", Color.black, false),
            new(BallEnum.StripedYellow, "StripedYellow", Color.yellow, true),
            new(BallEnum.StripedBlue, "StripedBlue", Color.blue, true),
            new(BallEnum.StripedRed, "StripedRed", Color.red, true),
            new(BallEnum.StripedPurple, "StripedPurple", new Color(0.5f, 0f, 0.5f), true),
            new(BallEnum.StripedOrange, "StripedOrange", new Color(1f, 0.5f, 0f), true),
            new(BallEnum.StripedGreen, "StripedGreen", Color.green, true),
            // new(BallEnum.StripedMaroon, "StripedMaroon", new Color(0.5f, 0f, 0f), true),
            new(BallEnum.StripedMaroon, "StripedMaroon", new Color(1f, 0.45f, 0.55f), true),
        };

        public BallEnum Enum { get; private set; }
        public Color Color { get; private set; }
        public string Name { get; private set; }
        public bool IsStriped { get; private set; }

        private BallDefinition(BallEnum @enum, string name, Color color, bool isStriped)
        {
            Enum = @enum;
            Name = name;
            Color = color;
            IsStriped = isStriped;
        }
    }
}
