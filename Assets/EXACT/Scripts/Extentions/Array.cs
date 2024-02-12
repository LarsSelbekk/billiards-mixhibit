using System;

public static class ArrayExtension
{
    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        if (length <= 0) { return new T[0]; }
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}
