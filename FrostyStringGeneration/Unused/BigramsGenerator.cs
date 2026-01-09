/*
 * This class, although it works, is unused atm. The goal was to do a statistical test on a string to see if it is English like, in order to eliminate Junk strings from strings coming from strings.exe from sysinternals
 * As I wrote my own string reader, this is no longer needed, but i'm leaving this here just for reference
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FrostyStringGeneration
{
    internal static class BigramsGenerator
    {
        public static Dictionary<string, double> BigramProbabilities => _bigramProbabilities;

        private static readonly Dictionary<string, long> _bigramCountsMap = new Dictionary<string, long>();
        private static long _totalBigrams = 0;
        private static Dictionary<string, double> _bigramProbabilities = new Dictionary<string, double>();

        public static void Initialize(bool generateBigrams = false)
        {
            string corpusFolderPath = @"G:\OANC-GrAF";
            string probabilitiesMapPath = null;

            if (generateBigrams)
            {
                GenerateFromCorpus(corpusFolderPath);
                //WriteProbabilitiesToFile("outputPath");
            }
            else
            {
                // if null we read from embedded resource
                ReadProbabilities(probabilitiesMapPath);
            }
        }

        public static void GenerateFromCorpus(string corpusFolderPath)
        {
            if (!Directory.Exists(corpusFolderPath))
            {
                return;
            }

            // Fetches all .txt files recursively
            string[] textFiles = Directory.GetFiles(corpusFolderPath, "*.txt", SearchOption.AllDirectories);
            foreach (string filePath in textFiles)
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    // Clean the line to remove unwanted characters
                    string[] words = CleanText(line);

                    // Extract bigrams
                    foreach (var word in words)
                    {
                        for (int i = 0; i < word.Length - 1; i++)
                        {
                            // Get the pair of adjacent characters
                            string bigram = word.Substring(i, 2);

                            // Filter out bigrams with non-letter chars
                            if (!IsTwoNonDigitBigram(bigram)) 
                                continue;

                            // Update dictionary counts
                            if (!_bigramCountsMap.ContainsKey(bigram))
                            {
                                _bigramCountsMap[bigram] = 0;
                            }
                            _bigramCountsMap[bigram]++;
                            _totalBigrams++;
                        }
                    }
                }
            }

            _bigramProbabilities = GetProbabilities();
        }

        // Convert the raw counts into probabilities:
        // Probability(bigram) = count(bigram) / total_bigrams
        public static Dictionary<string, double> GetProbabilities()
        {
            var probabilities = new Dictionary<string, double>();

            // Avoid dividing by zero in case no bigrams are processed
            if (_totalBigrams == 0)
                return probabilities;

            foreach (var kvp in _bigramCountsMap)
            {
                string bigram = kvp.Key;
                long count = kvp.Value;

                double probability = (double)count / _totalBigrams;
                probabilities[bigram] = probability;
            }

            return probabilities;
        }

        private static bool IsTwoNonDigitBigram(string bigram)
        {
            // bigram should be exactly 2 characters, so we can just check each
            return (bigram.Length == 2 &&
                    char.IsLetter(bigram[0]) &&
                    char.IsLetter(bigram[1]));
        }

        // Cleans a line of text by removing unwanted characters such as numbers,
        // annotations (e.g., content in brackets), and non-alphabetic characters.
        private static string[] CleanText(string text)
        {
            // Remove annotations enclosed in brackets (e.g., [text] or {text})
            string noAnnotations = System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]|\{[^\}]*\}", "");

            // Remove numbers and special characters, except spaces
            string cleanedText = System.Text.RegularExpressions.Regex.Replace(noAnnotations, @"[^a-zA-Z\s]", "");

            // Convert to lowercase for consistency
            string normalizedText = cleanedText.ToLowerInvariant();

            // Split the text by spaces, commas, or other common delimiters
            string[] words = normalizedText.Split(new[] { ' ', ',', ';', '.', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            return words;
        }

        public static void WriteProbabilitiesToFile(string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                foreach (var kvp in _bigramProbabilities)
                {
                    writer.WriteLine($"{kvp.Key},{kvp.Value}");
                }
            }
        }

        public static void ReadProbabilities(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                // Default to loading from embedded resource
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                                           .FirstOrDefault(name => name.EndsWith("Resources.BigramsProbabilities.csv", StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                {
                    Trace.WriteLine($"Embedded resource not found: {filePath}");
                    return;
                }

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        ProcessCsvLine(line);
                    }
                }
            }
            else
            {
                if (!File.Exists(filePath))
                {
                    Trace.WriteLine($"File not found: {filePath}");
                    return;
                }

                foreach (var line in File.ReadLines(filePath))
                {
                    ProcessCsvLine(line);
                }
            }
        }

        private static void ProcessCsvLine(string line)
        {
            var parts = line.Split(',');

            if (parts.Length == 2)
            {
                string bigram = parts[0].Trim();
                if (double.TryParse(parts[1].Trim(), out double probability))
                {
                    _bigramProbabilities[bigram] = probability;
                }
                else
                {
                    Trace.WriteLine($"Invalid probability value on line: {line}");
                }
            }
            else
            {
                Trace.WriteLine($"Invalid format on line: {line}");
            }
        }
    }
}
