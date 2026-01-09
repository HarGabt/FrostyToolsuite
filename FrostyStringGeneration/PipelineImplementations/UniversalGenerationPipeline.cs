using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FrostyStringGeneration
{
    public static class UniversalGenerationPipeline
    {
        public static HashSet<string> Run(HashSet<string> strings)
        {
            var generatedStrings = new HashSet<string>();

            /*
             * Multi level pipeline
             */
            // Filter unusable strings
            var pipelineRemoveUnusable = new StringPipeline().AddStep(new FilteringStep(new[] {
                new CandidateForGenerationFilteringRule()
            }));
            var (validStrings1, _) = pipelineRemoveUnusable.Run(strings);

            // Split by underscore
            var pipeline2 = new StringPipeline().AddStep(new GenerativeStep(new[] { 
                new ConditionalCharacterSplitGenerativeRule('_', true)
            }));
            var (_, tokensSplitByUnderscore) = pipeline2.Run(validStrings1);

            // Filter unusable strings
            var (validStrings2, _) = pipelineRemoveUnusable.Run(tokensSplitByUnderscore);

            // Split by period
            var pipeline3 = new StringPipeline().AddStep(new GenerativeStep(new[] {
                new ConditionalCharacterSplitGenerativeRule('.', true)
            }));
            var (_, tokensSplitByPeriod) = pipeline3.Run(validStrings2);

            // Filter unusable strings
            var (validStrings3, _) = pipelineRemoveUnusable.Run(tokensSplitByPeriod);

            // Split by keyword (such as "On")
            var pipeline5 = new StringPipeline().AddStep(new GenerativeStep(new[] {
                new KeywordSplitGenerativeRule()
            }));
            var (_, tokensSplitByKeyword) = pipeline5.Run(validStrings3);
            validStrings3.UnionWith(tokensSplitByKeyword);

            // Feed all the splits, then split by camelCase-like algorithm
            var pipeline6 = new StringPipeline().AddStep(new GenerativeStep(new[] {
                new CamelCaseSplitGenerativeRule()
            }));
            var (_, camelCaseTokens) = pipeline6.Run(validStrings3);

            /*
             * Single level pipeline
             */
            // Checks if a string contains "In" or "Out" in camelCase style then appends numbers from 0 to 4
            var pipeline7 = new StringPipeline().AddStep(new GenerativeStep(new[] {
                new AppendNumbersForInOrOutGenerativeRule()
            }));
            var (_, numberAppendedStrings) = pipeline7.Run(strings);

            generatedStrings.UnionWith(validStrings3);
            generatedStrings.UnionWith(camelCaseTokens);
            generatedStrings.UnionWith(numberAppendedStrings);

            return generatedStrings;
        }
        public class CandidateForGenerationFilteringRule : IFilteringRule
        {
            // Simple checks to see if a string can be used as a generation source
            public bool ShouldKeep(string input)
            {
                // If it's a path, then no
                if (input.Contains("/"))
                    return false;

                if (HasWhitespace(input))
                    return false;

                // If digits only, then no
                if (IsDigitsOnly(input))
                    return false;

                // If it's a GUID, then no
                if (input.Length == 36 && Guid.TryParse(input, out _))
                    return false;

                return true;
            }
        }

        public class ContainsCharacterFilteringRule : IFilteringRule
        {
            private readonly char _character;

            public ContainsCharacterFilteringRule(char character)
            {
                _character = character;
            }

            public bool ShouldKeep(string input)
            {
                if (input.Contains(_character))
                {
                    //Trace.WriteLine(input);
                    return false;
                }

                return true;
            }
        }

        public class ConditionalCharacterSplitGenerativeRule : IGenerativeRule
        {
            private readonly char _separator;
            private readonly bool _yieldOriginalIfNoSeparator;

            public ConditionalCharacterSplitGenerativeRule(char separator, bool yieldOriginalIfNoSeparator = false)
            {
                _separator = separator;
                _yieldOriginalIfNoSeparator = yieldOriginalIfNoSeparator;
            }

            public IEnumerable<string> Generate(string input)
            {
                if (string.IsNullOrEmpty(input))
                    yield break;

                if (!input.Contains(_separator))
                {
                    if (_yieldOriginalIfNoSeparator)
                        yield return input;

                    yield break;
                }

                string[] parts = input.Split(_separator);
                foreach (var part in parts)
                {
                    yield return part;
                }
            }
        }

        public class KeywordSplitGenerativeRule : IGenerativeRule
        {
            // If a string has a specific keyword that is found in camelCase, split it from that keyword
            // e.g "OnlyOnBegin" has the "On" keyword, therefore we should generate "OnBegin"
            // e.g "CharacterIsLocalPlayerEntityData" has the "Is" keyword, therefore we should generate "IsLocalPlayerEntityData"
            private static readonly Regex KeywordSplitRegex = new Regex(
                @"(On)(?=$|[A-Z])",
                RegexOptions.Compiled
            );

            public IEnumerable<string> Generate(string input)
            {
                if (string.IsNullOrEmpty(input))
                    yield break;

                var matches = KeywordSplitRegex.Matches(input);
                foreach (Match match in matches)
                {
                    // Substring from the match index to the end.
                    // e.g. "OnlyOnBegin".Substring(4) => "OnBegin"
                    string substring = input.Substring(match.Index);
                    yield return substring;
                }
            }
        }

        public class CamelCaseSplitGenerativeRule : IGenerativeRule
        {
            // Regex to handle runs of uppercase, uppercase+lower/digit, or lower/digit.
            private static readonly Regex camelCaseLikePattern = new Regex(@"([A-Z]+(?=[A-Z]|$)|[A-Z][a-z0-9]+|[a-z0-9]+)", RegexOptions.Compiled);

            public IEnumerable<string> Generate(string input)
            {
                if (string.IsNullOrEmpty(input))
                    yield break;

                // Split the string by our custom "camelCase-like" logic
                var tokens = SplitCamelCaseLike(input);

                // Optionally discard tokens that are all uppercase
                //    (comment this in if you want to drop all-uppercase tokens)
                //tokens = tokens
                //    .Where(t => !(IsAllUpper(t) && t.Length > 0))
                //    .ToList();

                // Yield each token as-is
                foreach (var token in tokens)
                {
                    yield return token;
                }

                // Generate concatenated tokens: up to 4 words from the start
                //  e.g. if tokens = [ "Set", "Player", "Locomotion", "Spline" ]
                //  then yield "SetPlayer" (2-word) and "SetPlayerLocomotion" (3-word) and "SetPlayerLocomotionSpline" (4-word)..
                if (tokens.Count >= 2)
                {
                    // First 2
                    yield return tokens[0] + tokens[1];
                }
                if (tokens.Count >= 3)
                {
                    // First 3
                    yield return tokens[0] + tokens[1] + tokens[2];
                }
                if (tokens.Count >= 4)
                {
                    // First 4
                    yield return tokens[0] + tokens[1] + tokens[2] + tokens[3];
                }
            }

            /// <summary>
            /// Splits a string by a specialized "camelCase-like" algorithm with digits. Consecutive uppercases are grouped as one token.
            /// 
            /// Examples:
            ///   "BWCSMAction"          -> ["BWCSM", "Action"]
            ///   "Vector3Provider"      -> ["Vector3", "Provider"]
            ///   "infoUITexture"        -> ["info", "UI", "Texture"]  -- mind the lowercase on info
            ///   "DataContainer"        -> ["Data", "Container"]
            ///   "JSONFile"             -> ["JSON", "File"]
            /// </summary>
            private static List<string> SplitCamelCaseLike(string input)
            {
                var tokens = new List<string>();
                if (string.IsNullOrEmpty(input))
                    return tokens;

                var matches = camelCaseLikePattern.Matches(input);
                foreach (Match m in matches)
                {
                    tokens.Add(m.Value);
                }
                return tokens;
            }
        }

        public class AppendNumbersForInOrOutGenerativeRule : IGenerativeRule
        {
            // This regex checks whether the string contains "In" or "Out" (case-sensitive),
            // followed by either the end of the string or any character that is A-Z (not a-z or 0-9 or any other character)
            // Examples that match:
            //   "SelectStringIn"        -> "In" is at the end => success
            //   "SelectStringInValue"   -> 'In' is followed by 'V' (uppercase) => success
            // Examples that do NOT match:
            //   "SelectStringIn0"       -> 'In' is followed by a numeric (0) => fails
            //   "SelectStringInvalue"   -> 'In' is followed by 'v' (lowercase) => fails
            //   "somethingOUT"          -> 'OUT' does not match 'Out' (case matters)
            private static readonly Regex InOrOutRegex = new Regex(
                @"(In|Out)(?=$|[A-Z])",
                RegexOptions.Compiled
            );

            public IEnumerable<string> Generate(string input)
            {
                if (string.IsNullOrEmpty(input))
                    yield break;

                // 1) Check if there's at least one match for "In" or "Out" not followed by a lowercase letter
                if (!InOrOutRegex.IsMatch(input))
                {
                    // If it does NOT match, we yield nothing
                    yield break;
                }

                // 2) If the match is found, generate new strings appending 0..4
                //    e.g. "SelectStringIn" -> "SelectStringIn0", "SelectStringIn1"...
                for (int i = 0; i < 5; i++)
                {
                    yield return input + i;
                }
            }
        }
        private static bool HasWhitespace(string s) => s.Length > 0 && s.Any(char.IsWhiteSpace);

        private static bool IsDigitsOnly(string s) => s.Length > 0 && s.All(char.IsDigit);

        private static bool IsAllUpper(string s) => s.Length > 0 && s.All(char.IsUpper);
    }
}
