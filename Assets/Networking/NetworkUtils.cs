namespace Networking
{
    public static class NetworkUtils
    {
        public static bool SequenceGreaterThan(ushort s1, ushort s2)
        {
            return (s1 > s2 && s1 - s2 <= 32768) ||
                   (s1 < s2 && s2 - s1 > 32768);
        }

        public static bool SequenceLessThan(ushort s1, ushort s2)
        {
            return SequenceGreaterThan(s2, s1);
        }
    }
}