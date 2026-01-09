using FrostyStringGeneration;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FrostyStringGeneration
{
    public static class ExeStringPipeline
    {
        public static StringPipeline GetExeStringPipeline()
        {
            return new StringPipeline()
                .AddStep(new FilteringStep(new IFilteringRule[] {
                    new JunkFilteringRule() // Universal known junk filter
                }))
                // Universal filters
                .AddStep(new FilteringStep(new IFilteringRule[] {
                    new UniversalCriteriaFilteringRule(),
                    new DrivePathFilteringRule(),
                    new NoAlphaCharactersFilteringRule(),
                    new ThreeOrMoreRepeatedCharactersFilteringRule(),
                    new FiveOrMoreConsecutiveDigitsFilteringRule()
                }))
                .AddStep(new GenerativeStep(new IGenerativeRule[] {
                    new LowercaseMGenerativeRule()
                }));
        }

        #region Filtering Rules
        public class JunkFilteringRule : IFilteringRule
        {
            private static readonly HashSet<string> _junk = LoadJunk();

            public bool ShouldKeep(string input)
            {
                return !_junk.Contains(input);
            }

            private static HashSet<string> LoadJunk()
            {
                var junk = new HashSet<string>();
                Assembly assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();

                foreach (var resourceName in resourceNames.Where(name => name.Contains(".Junk.")))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) continue;

                        using (StreamReader reader = new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                if (!string.IsNullOrEmpty(line))
                                {
                                    junk.Add(line);
                                }
                            }
                        }
                    }
                }

                return junk;
            }
        }

        public class UniversalCriteriaFilteringRule : IFilteringRule
        {
            // These were quickly done lists to try to match junk strings (useless stuff like directx, internal apis such as  Origin, Blaze,  etc) and in no way comprehensive
            // The goal is to reduce the amount of junk as much as possible

            // Checks if a string starts with: - < > = ' \ _ % / & [ { ! . , ; : and space
            private static readonly Regex _startsWithForbiddenCharacterRegex = new Regex(@"^[-<>=\'\\_%/&[{\!.,;: ]", RegexOptions.Compiled);
            private static readonly List<string> _startsWithCaseSensitive = new List<string>()
            {
                "WeakPtr",
                "NativePtr",
                "c_",
                "r_",
                "rw",
                "Ecs",
                "FFX_",
                "hcl",
                "LCU",
                "hr_",
                "ff_",
                "df_",
                "vg_",
                "ld_",
                "spline_",
                "Tt",
                "TLS",
                "SSL",
                "GAME",
                "CLIENT",
                "SDK",
                "CLUB",
                "USER",
                "STAT",
                "AUTH",
                "ERR",
                //"Shader", // some of these keywords might remove too much
                //"Vertex",
                //"Dolby",
                //"Sprite",
                //"Enlighten",
                //"PBR",
            };

            private static readonly List<string> _startsWithCaseInsensitive = new List<string>()
            {
                "hk", // havok
                "x-",
                "xx-",
                "turbo.",
            };

            private static readonly List<string> _containsCaseSensitive = new List<string>()
            {
                "\"",
                "://", // protocols such as file://
                "->", // cpp pointer calls such as: m_device->CreateFence()
                "%",
                "CERTIFICATE",
                "PRIVATE",
                "EcsVisual",
                "std:",
                "STL",
                "Blaze:",
                "EA:",
                "EAWebKit",
                "EA.Physics.",
                "EACloth",
                "BWSavegame:",
                "IGO ",
                "LSX ",
                "Origin:",
                "Origin ",
                "ORIGIN",
                "Microsoft",
                "SimpleCloth:",
                "Serac:",
                "Snd:",
                "FBDestructionManager:",
                "rtFramework:",
                "RTAO",
                "HBAO",
                "albedo",
                "Surfel"
            };

            private static readonly List<string> _containsCaseInsensitive = new List<string>()
            {
                "http",
                ".cpp",
                ".dll",
                ".com",
                "d3d10",
                "d3d11",
                "dxgi",
                "directx",
                "direct3d",
                "fsr2",
                "openssl",
                "protobuf",
                "hkp", // havok
                "hknp", // havok
                "hknd", // havok
                "vp6",
                "vp8",
                "www"
            };

            private static readonly List<string> _endsWith = new List<string>()
            {
                "-Array",
                "()",
                "]",
                "}",
            };

            public bool ShouldKeep(string input)
            {
                if (_startsWithForbiddenCharacterRegex.IsMatch(input))
                {
                    //Trace.WriteLine(input);
                    return false;
                }
                foreach (var prefix in _startsWithCaseSensitive)
                {
                    if (input.StartsWith(prefix))
                    {
                        //Trace.WriteLine(input);
                        return false;
                    }
                }
                foreach (var prefix in _startsWithCaseInsensitive)
                {
                    if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        //Trace.WriteLine(input);
                        return false;
                    }
                }
                foreach (var word in _containsCaseSensitive)
                {
                    if (input.Contains(word))
                    {
                        //Trace.WriteLine(input);
                        return false;
                    }
                }
                foreach (var word in _containsCaseInsensitive)
                {
                    if (input.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        //Trace.WriteLine(input);
                        return false;
                    }
                }
                foreach (var suffix in _endsWith)
                {
                    if (input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        //Trace.WriteLine(input);
                        return false;
                    }
                }
                return true;
            }
        }

        public class DrivePathFilteringRule : IFilteringRule
        {
            private static readonly Regex DrivePathRegex = new Regex(@"^[A-Za-z]:[\\/]", RegexOptions.Compiled);

            public bool ShouldKeep(string input)
            {
                bool isMatch = DrivePathRegex.IsMatch(input);
                if (isMatch)
                {
                    //Trace.WriteLine(input);
                    return false;
                }
                return true;
            }
        }

        public class NoAlphaCharactersFilteringRule : IFilteringRule
        {
            // If a string doesn't have at least one alpha character (a-z or A-Z) then reject it
            private static readonly Regex NoAlphaCharactersRegex = new Regex(@"^[^A-Za-z]*$", RegexOptions.Compiled);

            public bool ShouldKeep(string input)
            {
                bool isMatch = NoAlphaCharactersRegex.IsMatch(input);
                if (isMatch)
                {
                    //Trace.WriteLine(input);
                    return false;
                }
                return true;
            }
        }

        public class ThreeOrMoreRepeatedCharactersFilteringRule : IFilteringRule
        {
            private static readonly Regex ThreeOrMoreRepeatedCharactersRegex = new Regex(@"(.)\1{2,}", RegexOptions.Compiled);

            public bool ShouldKeep(string input)
            {
                bool isMatch = ThreeOrMoreRepeatedCharactersRegex.IsMatch(input);
                if (isMatch)
                {
                    //Trace.WriteLine(input);
                    return false;
                }
                return true;
            }
        }

        public class FiveOrMoreConsecutiveDigitsFilteringRule : IFilteringRule
        {
            private static readonly Regex FiveOrMoreConsecutiveDigitsRegex = new Regex(@"\d{5,}", RegexOptions.Compiled);

            public bool ShouldKeep(string input)
            {
                bool isMatch = FiveOrMoreConsecutiveDigitsRegex.IsMatch(input);
                if (isMatch)
                {
                    //Trace.WriteLine(input);
                    return false;
                }
                return true;
            }
        }
        #endregion Filtering Rules

        #region Generative Rules

        public class LowercaseMGenerativeRule : IGenerativeRule
        {
            private static readonly Regex StartsWithLowercaseMRegex = new Regex(@"^m[A-Z]", RegexOptions.Compiled);

            public IEnumerable<string> Generate(string input)
            {
                bool isMatch = StartsWithLowercaseMRegex.IsMatch(input);
                if (isMatch)
                {
                    yield return input.Substring(1);
                }
                else if (input.StartsWith("m_"))
                {
                    var remainder = input.Substring(2);
                    if (!string.IsNullOrEmpty(remainder))
                    {
                        yield return char.ToUpper(remainder[0]) + remainder.Substring(1);
                    }
                }
                else
                {
                    yield break;
                }
            }
        }

        #endregion Generative Rules
    }
}
