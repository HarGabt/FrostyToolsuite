using System.Globalization;
using FrostySdk;

namespace BlueprintEditorPlugin.BlueprintUtils
{
    public static class HashingUtils
    {
        /// <summary>
        /// Hashes a string or converts it to a integer if its determined as hexadecimal
        /// </summary>
        /// <param name="str">The string to hash. If it starts with "0x" it will convert it straight to an int from hexadecimal</param>
        /// <returns></returns>
        public static int SmartHashString(string str)
        {
            if (str.StartsWith("0x"))
            {
                return int.Parse(str.Replace("0x", ""), NumberStyles.AllowHexSpecifier);
            }
            else
            {
                return Utils.HashString(str);
            }
        }
    }
}