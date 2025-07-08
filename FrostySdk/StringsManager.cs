using Frosty.Hash;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace FrostySdk
{
    public static class StringsManager
    {
        private static Dictionary<int, string> strings = new Dictionary<int, string>();

        public static string GetString(int hash)
        {
            if (!strings.TryGetValue(hash, out var value))
            {
                return "0x" + hash.ToString("x8");
            }
            return value;
        }

        /// <summary>
        /// Loads all resolved hashes
        /// </summary>
        /// <param name="path">The file to be read from for hashes.</param>
        public static void LoadStringList(string path = "strings.txt", ILogger logger = null)
        {
            if (!File.Exists(path)) return;

            strings.Clear();

            try
            {
                var allLines = File.ReadAllLines(path);
                int totalLines = allLines.Length;
                // ProcessCount - 1 in order to leave one core readily available for the UI thread
                int concurrencyLevel = Math.Max(Environment.ProcessorCount - 1, 1);
                var partitioner = Partitioner.Create(0, totalLines, Math.Max(totalLines / concurrencyLevel, 1));
                var stringPartitionResults = new ConcurrentBag<StringPartitionResult>();

                int processedCount = 0;
                int logIntervals = 20;
                int logInterval = Math.Max(totalLines / logIntervals, 1);

                Parallel.ForEach(partitioner, range =>
                {
                    var localDict = new Dictionary<int, string>(range.Item2 - range.Item1);

                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var currentString = allLines[i];
                        int hash = Fnv1.HashString(currentString);
                        localDict[hash] = currentString;

                        if (logger != null)
                        {
                            int countSoFar = Interlocked.Increment(ref processedCount);
                            if (countSoFar % logInterval == 0 || countSoFar == totalLines)
                            {
                                double progress = (double)countSoFar / totalLines * 100.0;
                                logger.Log($"progress: {progress}");
                            }
                        }
                    }

                    stringPartitionResults.Add(new StringPartitionResult(range.Item1, range.Item2, localDict));
                });

                // Sort partitions by start index so we overwrite from earliest to latest in file order
                var sortedPartitionResults = stringPartitionResults
                    .OrderBy(pr => pr.StartIndex)
                    .ToList();

                var finalDictionary = new Dictionary<int, string>(totalLines);

                // Merging from lowest StartIndex to highest ensures that collisions
                // are ultimately overwritten by lines that appear later in the file.
                foreach (var pr in sortedPartitionResults)
                {
                    foreach (var kvp in pr.LocalDictionary)
                    {
                        finalDictionary[kvp.Key] = kvp.Value;
                    }
                }

                strings = finalDictionary;
            }
            catch (Exception ex)
            {
                logger?.Log($"Error loading strings: {ex.Message}");
            }
        }

        private sealed class StringPartitionResult
        {
            public int StartIndex { get; }
            public int EndIndex { get; }
            public Dictionary<int, string> LocalDictionary { get; }

            public StringPartitionResult(int startIndex, int endIndex, Dictionary<int, string> localDictionary)
            {
                StartIndex = startIndex;
                EndIndex = endIndex;
                LocalDictionary = localDictionary;
            }
        }
    }
}
