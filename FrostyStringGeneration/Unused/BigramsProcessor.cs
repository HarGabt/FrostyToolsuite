/*
 * This class, although it works, is unused atm. The goal was to do a statistical test on a string to see if it is English like, in order to eliminate Junk strings from strings coming from strings.exe from sysinternals
 * As I wrote my own string reader, this is no longer needed, but i'm leaving this here just for reference
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FrostyStringGeneration
{
    internal class BigramsProcessor
    {
        private Dictionary<string, double> _bigramProbabilities;
        private const double c_threshold = -8.0;

        public BigramsProcessor(Dictionary<string, double> bigramProbabilities)
        {
            _bigramProbabilities = bigramProbabilities;
        }
        private bool IsEnglishLike(double score) => score >= c_threshold;

        public double GetBigramScore(List<string> words)
        {
            double normalizedLogLikelihood = 0.0;
            double logLikelihoodSum = 0.0;
            int bigramCount = 0;

            // Normalize the bigram score over the entire sentence
            foreach (string word in words)
            {
                string normalizedWord = word.ToLowerInvariant();
                for (int i = 0; i < normalizedWord.Length - 1; i++)
                {
                    string bigram = normalizedWord.Substring(i, 2);
                    if (_bigramProbabilities.TryGetValue(bigram, out double prob))
                    {
                        logLikelihoodSum += Math.Log(prob);
                    }
                    else
                    {
                        // If we don't have it in dictionary, it may be extremely rare/unlikely so penalize it heavily
                        logLikelihoodSum += Math.Log(1e-10);
                    }
                    bigramCount++;
                }
            }
            normalizedLogLikelihood = bigramCount > 0 ? logLikelihoodSum / bigramCount : double.NegativeInfinity;
            return normalizedLogLikelihood;
        }

        public string[] ProcessString(string input)
        {
            List<string> wordsInInput = new List<string>();

            // Split by whitespace or punctuation
            string[] stringSplitBySpaces = input.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var text in stringSplitBySpaces)
            {
                // Split camelCase words
                var camelCaseSplit = Regex.Replace(text, @"(?<!^)(?=[A-Z])", " ");
                string[] wordsSplitByCamelCase = camelCaseSplit.Split(' ');
                wordsInInput.AddRange(wordsSplitByCamelCase);
            }

            double cumulativeScore = GetBigramScore(wordsInInput);
            bool isEnglishLike = IsEnglishLike(cumulativeScore);
            if (!isEnglishLike)
            {
                //Trace.WriteLine(input + " : " + cumulativeScore);
                return null; // reject
            }
            else
            {
                // do something with the string
                return new string[0];
            }
        }
    }
}
