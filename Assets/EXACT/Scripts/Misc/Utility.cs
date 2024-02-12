using System;

namespace Exact
{
    public static class Utility
    {
        /// <summary>
        /// Converts a string of hex values to a byte array
        /// https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
        /// </summary>
        /// <param name="hex">A string representing hex values</param>
        /// <returns></returns>
        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length / 2; ++i)
            {
                arr[i] = (byte)((GetHexValue(hex[i / 2]) << 4) + (GetHexValue(hex[(i / 2) + 1])));
            }

            return arr;
        }

        /// <summary>
        /// Converts a string of hex values to a byte array
        /// </summary>
        /// <param name="hex">A string representing hex values</param>
        /// <param name="separator">The character separating each byte</param>
        /// <returns></returns>
        public static byte[] StringToByteArray(string hex, char separator)
        {
            int length = hex.Length;
            for (int i = 0; i < hex.Length / 2; ++i)
            {
                if (hex[i] == separator) { length--; }
            }

            if (length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                if (hex[i] == separator)
                {
                    i--;
                }
                else
                {
                    arr[i] = (byte)((GetHexValue(hex[i]) << 4) + (GetHexValue(hex[i + 1])));
                }
            }

            return arr;
        }

        /// <summary>
        /// Converts a character to the hex value it reperesents
        /// </summary>
        /// <param name="hex">The character to convert</param>
        /// <returns>The hex value of the character</returns>
        public static int GetHexValue(char hex)
        {
            int val = hex;
            // For uppercase A-F letters:
            // return val - (val < 58 ? 48 : 55);
            // For lowercase a-f letters:
            // return val - (val < 58 ? 48 : 87);
            // Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
