using Frosty.Core;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers.Entries;
using FrostyStringGeneration.Ebx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FrostyStringGeneration
{
    internal class StringGenerator
    {
        // Final strings dictionary. It is a map of hash to ordered List of strings that match this hash, in order of likelihood
        Dictionary<int, List<string>> _finalStrings = new Dictionary<int, List<string>>();

        // Strings coming from executable, using the exe string scanner
        // Main game (selected profile)
        HashSet<string> _exe_strings_mainGame = new HashSet<string>();
        // Additional games, if applicable (same EA division, BioWare -> DA:I, ME:A, Anthem, etc.)
        HashSet<string> _exe_strings_additionalGames = new HashSet<string>();

        // Strings from SDK
        HashSet<string> _sdk_strings = new HashSet<string>();

        // Strings coming from EBX
        HashSet<string> _ebx_folderNames = new HashSet<string>();
        HashSet<string> _ebx_fullPaths = new HashSet<string>();
        HashSet<string> _ebx_cstrings = new HashSet<string>();

        // Logging
        private ILogger m_logger;
        public void SetLogger(ILogger inLogger) => m_logger = inLogger;
        private void WriteToLog(string text, params object[] vars) => m_logger?.Log(text, vars);

        public void Generate(string outputDirectoryPath)
        {
            // Collect strings from exe using custom low-junk string scanner.
            CollectStringsFomExecutable();

            // Collect strings from sdk (type names, field names, enums).
            CollectStringsFromSdk();

            // Collect strings from ebx (cstrings, folder names, asset names).
            CollectStringsFromEbxLazy();

            WriteToLog("Generating strings...");
            // Step 1: Hash the Sdk strings
            foreach (string str in _sdk_strings)
                AddToFinalStrings(str);

            // Step 2: Hash the ebx strings
            foreach (var str in _ebx_cstrings)
                AddToFinalStrings(str);

            foreach (var str in _ebx_folderNames)
                AddToFinalStrings(str);

            foreach (var str in _ebx_fullPaths)
                AddToFinalStrings(str);

            // Step 3: Do generative on sdk strings
            foreach (string str in UniversalGenerationPipeline.Run(_sdk_strings))
                AddToFinalStrings(str);

            // Step 4: Hash the exe strings
            foreach (var str in _exe_strings_mainGame)
                AddToFinalStrings(str);

            // Step 5: Do generative on exe strings
            foreach (string str in UniversalGenerationPipeline.Run(_exe_strings_mainGame))
                AddToFinalStrings(str);

            // Step 6: Do generative on ebx strings
            foreach (string str in UniversalGenerationPipeline.Run(_ebx_cstrings))
                AddToFinalStrings(str);

            foreach (string str in UniversalGenerationPipeline.Run(_ebx_folderNames))
                AddToFinalStrings(str);

            // Step 7: Hash the exe strings from other games (if applicable), single pass, no generation
            foreach (var str in _exe_strings_additionalGames)
                AddToFinalStrings(str);

            // Step 8: Apply strings override that are known to be perfectly valid
            ApplyOverrides();

            // End
            WriteToLog("Writing Strings.txt");
            WriteConflictsToFile(Path.Combine(outputDirectoryPath, "Conflicts.txt"));
            WriteFinalStringsToFile(Path.Combine(outputDirectoryPath, "Strings.txt"), true);

            WriteToLog("All done, press any key to continue");
            Console.ReadKey();
        }


        public void CollectStringsFromSdk()
        {
            // Technically the SDK strings will all be found inside the executable dumped strings as well.
            // However, we leverage the fact that there is absolutely zero junk in these to prioritize these strings when doing the generation.
            WriteToLog("Collecting strings from sdk...");

            Type[] types = TypeLibrary.GetConcreteTypes();

            foreach (var type in types)
            {
                _sdk_strings.Add(type.Name);
                if (type.IsEnum)
                {
                    var values = type.GetEnumValues();
                    // enum members
                    for (int i = 0; i < values.Length; i++)
                    {
                        var enumValue = values.GetValue(i).ToString();
                        _sdk_strings.Add(enumValue);
                    }
                }
                // class properties
                foreach (var pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (pi.Name.StartsWith("_"))
                        continue;

                    _sdk_strings.Add(pi.Name);
                }
            }
        }

        public void CollectStringsFromEbxLazy()
        {
            WriteToLog("Collecting strings from ebx...");

            uint totalCount = App.AssetManager.GetEbxCount();
            uint idx = 0;
            foreach (EbxAssetEntry entry in App.AssetManager.EnumerateEbx())
            {
                if (++idx % 20000 == 0)
                {
                    WriteToLog($"Collecting strings from ebx progress: {(idx++ / (double)totalCount) * 100.0d}%");
                }
                _ebx_fullPaths.Add(entry.Name);
                foreach (var part in entry.Path.Split('/'))
                {
                    _ebx_folderNames.Add(part);
                }

                Stream ebxStream = App.AssetManager.GetEbxStream(entry);
                if (ebxStream == null)
                {
                    continue;
                }
                try
                {
                    using (NativeReader reader = new NativeReader(ebxStream))
                    {
                        EbxRiffDumper ebxRiffDumper = new EbxRiffDumper(reader);
                        List<string> cstrings = ebxRiffDumper.DumpCStrings(reader);

                        // Skip the first string as its always the full asset name including path, which we already have
                        for (int i = 1; i < cstrings.Count; i++)
                        {
                            // Quick heuristic: if string length is greater than 255, reject it
                            if (cstrings[i].Length > 255)
                            {
                                continue;
                            }
                            _ebx_cstrings.Add(cstrings[i]);
                        }
                    }
                }
                catch
                {
                    WriteToLog("skipping " + entry.Name);
                }
            }
        }

        // TODO, not fully implemented atm, a better way to generate from ebx
        //public void GenerateFromEbx()
        //{
        // 
        //    foreach (EbxAssetEntry entry in App.AssetManager.EnumerateEbx())
        //    {
        //        _ebx_fullPaths.Add(entry.Name);
        //        foreach (var part in entry.Path.Split('/'))
        //        {
        //            _ebx_folderNames.Add(part);
        //        }

        //        Stream ebxStream = App.AssetManager.GetEbxStream(entry);
        //        if (ebxStream == null)
        //        {
        //            continue;
        //        }
        //        try
        //        {
        //            using (NativeReader reader = new NativeReader(ebxStream))
        //            {
        //                // use the EbxCStringReader on the Ebx object created from reflection
        //            }
        //        }
        //        catch
        //        {
        //            Console.WriteLine("skipping " + entry.Name);
        //        }
        //    }

        //    foreach (var cstring in _ebx_cstrings)
        //    {
        //        AddString(cstring);
        //    }
        //    foreach (var folderName in _ebx_folderNames)
        //    {
        //        AddString(folderName);
        //    }
        //    foreach (var assetName in _ebx_fullPaths)
        //    {
        //        AddString(assetName);
        //    }
        //}

        public void CollectStringsFomExecutable()
        {
            WriteToLog("Collecting strings from executable...");

            // no longer need to use Bigrams test as the string scanner is good enough to reduce junk to a minimum
            //BigramsGenerator.Initialize();
            //BigramsProcessor bigramsProcessor = new BigramsProcessor(BigramsGenerator.BigramProbabilities);
            _exe_strings_mainGame = ScanExecutableForProfile((ProfileVersion)ProfilesLibrary.DataVersion);
            _exe_strings_additionalGames = CollectStringsFromOtherExecutables((ProfileVersion)ProfilesLibrary.DataVersion);
        }

        // This method is completelhy optional
        // It will scan other games .exe and add all their strings into one single HashSet.
        // Since Veilguard is a Bioware game, we may want to also get the .exe strings from other Bioware games, like ME:A, DA:I, Anthem
        // DICE games like SWBF2 might want to get strings from SWBF1. Etc.
        public HashSet<string> CollectStringsFromOtherExecutables(ProfileVersion mainGameVersion)
        {
            HashSet<string> otherGamesStrings = new HashSet<string>();

            List<ProfileVersion> additionalGamesToScan = new List<ProfileVersion>();
            switch (mainGameVersion)
            {
                case ProfileVersion.NeedForSpeedUnbound:
                    additionalGamesToScan = new List<ProfileVersion>() {
                        ProfileVersion.DragonAgeInquisition,
                        ProfileVersion.MassEffectAndromeda,
                        ProfileVersion.DeadSpace,
                        ProfileVersion.Anthem
                    };
                    break;
                default:
                    break;
            }

            foreach (ProfileVersion otherGameVersion in additionalGamesToScan)
            {
                Profile? profileDefinition = ProfilesLibrary.GetProfileDefinition(otherGameVersion);
                WriteToLog($"Collecting strings from optional executable with profile '{profileDefinition.Value.Name}'...");
                otherGamesStrings.UnionWith(ScanExecutableForProfile(otherGameVersion));
                WriteToLog($"Collected {otherGamesStrings.Count} strings from optional executables.");
            }

            return otherGamesStrings;
        }

        private HashSet<string> ScanExecutableForProfile(ProfileVersion version)
        {
            HashSet<string> foundStrings = new HashSet<string>();
            Profile? profileDefinition = ProfilesLibrary.GetProfileDefinition(version);

            // Get the executable path from the Editor/Manager Config json
            string basePath = ""; 
            try
            {
                basePath = Config.Get<string>("GamePath", "", ConfigScope.Game, profileDefinition.Value.Name);
            }
            catch
            {
                throw new Exception("Profile doesn't exist in Config file");
            }

            string executableFilePath = Path.Combine(basePath, profileDefinition.Value.Name + ".exe");

            FrostbiteStringScannerParams scannerParams = new FrostbiteStringScannerParams();
            List<string> sectionsToScan = new List<string>();

            switch (version)
            {
                case ProfileVersion.Anthem:
                    sectionsToScan = new List<string> { ".arch" };
                    scannerParams.StartOnString = "Dylan";
                    // scannerParams.StopOnString = @"C:\DYL\TnT\Local\Bin\Dylan\Win64\retail\Dylan.Main_Win64_retail.pdb", // This includes slightly more junk
                    scannerParams.StopOnString = "$Element";
                    break;
                case ProfileVersion.NeedForSpeedUnbound:
                    sectionsToScan = new List<string> { ".00cfg" };
                    scannerParams.StartOnString = "Excalibur";
                    scannerParams.StopOnString = @"D:\dev\TnT\Local\Bin\Nfs22\Win64\retail\Nfs22.Main_Win64_retail.pdb";
                    break;
                case ProfileVersion.DragonAgeInquisition:
                    sectionsToScan = new List<string> { ".data1" };
                    scannerParams.StartOnString = "Dragon Age: Inquisition";
                    // scannerParams.StopOnString = @"C:\monkey\bwmonkey-da3\tnt\local_win64_retail\Bin\DA3.Main_Win64_retail.pdb" // This includes slightly more junk
                    scannerParams.StopOnString = "Stereo";
                    break;
                case ProfileVersion.MassEffectAndromeda:
                    sectionsToScan = new List<string> { ".rdata" };
                    scannerParams.StartOnString = "Future";
                    // scannerParams.StopOnString = @"C:\MEA\rc\TnT\Local\Bin\Contact\Win64\retail\Contact.Main_Win64_retail.pdb", // This includes slightly more junk
                    scannerParams.StopOnString = "Registry";
                    break;
                case ProfileVersion.StarWarsBattlefrontII:
                    sectionsToScan = new List<string> { ".idata" };
                    scannerParams.StartOnString = "Walrus";
                    scannerParams.StopOnString = @"D:\dev\TnT\Local\Bin\WS\Win64\retail\WS.Main_Win64_retail.pdb";
                    break;
                case ProfileVersion.Battlefield1:
                    sectionsToScan = new List<string> { ".data2" };
                    scannerParams.StartOnString = "Tunguska.Tunguska.{}";
                    scannerParams.StopOnString = @"E:\dev\tun\TnT\Local\Bin\Tunguska.Main_Win64_retail.pdb";
                    break;
                default:
                    break;
            }

            FrostbiteStringScanner scanner = new FrostbiteStringScanner(executableFilePath, scannerParams);

            // Scan sections
            WriteToLog($"Scanning strings from executable... ({profileDefinition.Value.Name + ".exe"})");
            HashSet<string> rawStrings = new HashSet<string>();
            foreach (string sectionName in sectionsToScan)
            {
                scanner.ScanSection(sectionName, rawStrings);
            }

            WriteToLog($"Found {rawStrings.Count} total unique strings across sections");
            StringPipeline pipeline = ExeStringPipeline.GetExeStringPipeline();
            var (filteredStrings, generatedStrings) = pipeline.Run(rawStrings);

            WriteToLog($"After applying generic filtering pipeline, {filteredStrings.Count} strings remain");
            // We do a minimal generation on the generic filtering pipeline
            filteredStrings.UnionWith(generatedStrings);
            foundStrings = filteredStrings;

            return foundStrings;
        }

        public void AddToFinalStrings(string s, bool prioritize = false)
        {
            int hash = Fnv1.HashString(s);

            if (!_finalStrings.ContainsKey(hash))
            {
                _finalStrings[hash] = new List<string>();
            }

            if (!_finalStrings[hash].Contains(s))
            {
                if (prioritize)
                {
                    _finalStrings[hash].Insert(0, s); // Add as the first item
                }
                else
                {
                    _finalStrings[hash].Add(s);
                }
            }
        }

        public void ApplyOverrides()
        {
            WriteToLog("Applying overrides...");

            var overrides = new HashSet<string>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames.Where(name => name.Contains(".Overrides.")))
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
                                overrides.Add(line);
                            }
                        }
                    }
                }
            }

            foreach (string s in overrides)
            {
                AddToFinalStrings(s, true);
            }
        }

        public void WriteFinalStringsToFile(string filePath, bool oneValuePerHash)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var kvp in _finalStrings)
                {
                    if (oneValuePerHash)
                    {
                        // Write only the first value if the list is not empty.
                        if (kvp.Value.Count > 0)
                        {
                            writer.WriteLine(kvp.Value[0]);
                        }
                    }
                    else
                    {
                        // Write all values in the list.
                        foreach (var value in kvp.Value)
                        {
                            writer.WriteLine(value);
                        }
                    }
                }
            }
        }

        public void WriteConflictsToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var kvp in _finalStrings)
                {
                    if (kvp.Value.Count > 1)
                    {
                        writer.WriteLine(kvp.Key);

                        foreach (string value in kvp.Value)
                        {
                            writer.WriteLine(value);
                        }

                        writer.WriteLine();
                    }
                }
            }
        }
    }
}
