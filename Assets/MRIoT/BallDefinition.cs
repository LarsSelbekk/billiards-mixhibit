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
            new((int)BallEnum.White, "White", Color.white, false),
            new((int)BallEnum.SolidYellow, "SolidYellow", Color.yellow, false),
            new((int)BallEnum.SolidBlue, "SolidBlue", Color.blue, false),
            new((int)BallEnum.SolidRed, "SolidRed", Color.red, false),
            new((int)BallEnum.SolidPurple, "SolidPurple", new Color(0.5f, 0f, 0.5f), false),
            new((int)BallEnum.SolidOrange, "SolidOrange", new Color(1f, 0.5f, 0f), false),
            new((int)BallEnum.SolidGreen, "SolidGreen", Color.green, false),
            new((int)BallEnum.SolidMaroon, "SolidMaroon", new Color(0.5f, 0f, 0f), false),
            new((int)BallEnum.Black, "Black", Color.black, false),
            new((int)BallEnum.StripedYellow, "StripedYellow", Color.yellow, true),
            new((int)BallEnum.StripedBlue, "StripedBlue", Color.blue, true),
            new((int)BallEnum.StripedRed, "StripedRed", Color.red, true),
            new((int)BallEnum.StripedPurple, "StripedPurple", new Color(0.5f, 0f, 0.5f), true),
            new((int)BallEnum.StripedOrange, "StripedOrange", new Color(1f, 0.5f, 0f), true),
            new((int)BallEnum.StripedGreen, "StripedGreen", Color.green, true),
            new((int)BallEnum.StripedMaroon, "StripedMaroon", new Color(0.5f, 0f, 0f), true),
        };

        public int Index { get; private set; }
        public Color Color { get; private set; }
        public string Name { get; private set; }
        public bool IsStriped { get; private set; }

        private BallDefinition(int index, string name, Color color, bool isStriped)
        {
            Index = index;
            Name = name;
            Color = color;
            IsStriped = isStriped;
        }
    }
}
