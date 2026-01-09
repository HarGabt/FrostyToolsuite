using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FrostyStringGeneration
{
    internal class Helpers
    {
        public static void DumpHashSetToDisk(HashSet<string> hashSet, string filePath)
        {
            if (hashSet == null)
                throw new ArgumentNullException(nameof(hashSet));

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var str in hashSet)
                {
                    writer.WriteLine(str);
                }
            }
        }

        public static HashSet<string> LoadHashSetFromDisk(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");

            var hashSet = new HashSet<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    hashSet.Add(line);
                }
            }
            return hashSet;
        }

        public static HashSet<string> GetAnthemSDKStrings()
        {
            string resourceName = "FrostyStringGeneration.Resources.SDK.AnthemSDK.txt";
            HashSet<string> lines = ReadEmbeddedResourceAsHashSet(resourceName);
            return lines;
        }

        public static HashSet<string> ReadEmbeddedResourceAsHashSet(string resourceName)
        {
            var hashSet = new HashSet<string>();
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new FileNotFoundException($"Resource '{resourceName}' not found.");

                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        hashSet.Add(line);
                    }
                }
            }

            return hashSet;
        }
    }
}
