using System;
using System.IO;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System.Reflection;
using Frosty.Core;
using System.Threading.Tasks;

/*
 * This project is used to generate strings that could match the unknown hashed values commonly found in the EBX property grid, such as property/event/link connections.
 */

namespace FrostyStringGeneration
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string text, params object[] vars) => Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]: " + text, vars);

        public void LogError(string text, params object[] vars) => throw new NotImplementedException();

        public void LogWarning(string text, params object[] vars) => throw new NotImplementedException();
    }

    public class Program
    {
        public static readonly ConsoleLogger logger = new ConsoleLogger();

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // Set the profile name to choose the game you want to generate strings for
            ProfileVersion profile = ProfileVersion.NeedForSpeedUnbound;
            // Set the directory where the Strings.txt will be outputted to, including Conflicts.txt
            string outputDirectoryPath = @"G:\";

            try
            {
                Initialize(profile).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            if (ProfilesLibrary.EbxVersion != 6)
            {
                throw new Exception("string generation not implemented");
            }

            StringGenerator stringGenerator = new StringGenerator();
            stringGenerator.SetLogger(logger);
            stringGenerator.Generate(outputDirectoryPath);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task<int> Initialize(ProfileVersion profile)
        {
            await Task.Run(() =>
            {
                Config.Load();
                ProfilesLibrary.Initialize();
                ProfilesLibrary.SelectProfile(profile);
                string basePath = Config.Get<string>("GamePath", "", ConfigScope.Game, ProfilesLibrary.ProfileName);
                if (ProfilesLibrary.RequiresKey)
                {
                    byte[] keyData = null;
                    if (!File.Exists(ProfilesLibrary.CacheName + ".key"))
                    {
                        throw new Exception("missing .key");
                    }
                    else
                    {
                        // otherwise just read the key from file
                        keyData = NativeReader.ReadInStream(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Open, FileAccess.Read));
                    }

                    // add primary encryption key
                    byte[] key = new byte[0x10];
                    Array.Copy(keyData, key, 0x10);
                    KeyManager.Instance.AddKey("Key1", key);

                    if (keyData.Length > 0x10)
                    {
                        // add additional encryption keys
                        key = new byte[0x10];
                        Array.Copy(keyData, 0x10, key, 0, 0x10);
                        KeyManager.Instance.AddKey("Key2", key);

                        key = new byte[0x4000];
                        Array.Copy(keyData, 0x20, key, 0, 0x4000);
                        KeyManager.Instance.AddKey("Key3", key);
                    }
                }

                App.FileSystemManager = new FileSystemManager(basePath);
                foreach (FileSystemSource source in ProfilesLibrary.Sources)
                {
                    App.FileSystemManager.AddSource(source.Path, source.SubDirs);
                }
                App.FileSystemManager.Initialize(KeyManager.Instance.GetKey("Key1"));

                App.ResourceManager = new ResourceManager(App.FileSystemManager);
                App.ResourceManager.SetLogger(logger);
                App.ResourceManager.Initialize();

                App.AssetManager = new AssetManager(App.FileSystemManager, App.ResourceManager);

                TypeLibrary.Initialize();

                // initialize plugin extensions
                //App.PluginManager.Initialize();

                //// load legacy asset manager if profile uses legacy system
                //if (ProfilesLibrary.IsLoaded(ProfileVersion.Fifa17,
                //        ProfileVersion.Fifa18,
                //        ProfileVersion.Madden19,
                //        ProfileVersion.Fifa19,
                //        ProfileVersion.Madden20,
                //        ProfileVersion.Fifa20,
                //        ProfileVersion.PlantsVsZombiesBattleforNeighborville))
                //{
                //    App.AssetManager.RegisterCustomAssetManager("legacy", typeof(LegacyFileManager));
                //}
                //else if (ProfilesLibrary.IsLoaded(ProfileVersion.Fifa21, ProfileVersion.Madden22, ProfileVersion.Fifa22,
                //    ProfileVersion.Madden23, ProfileVersion.Fifa23))
                //{
                //    App.AssetManager.RegisterCustomAssetManager("legacy", typeof(LegacyFileManagerV2));
                //}

                //// ensure mods folder is created
                //DirectoryInfo di = new DirectoryInfo("Mods/" + ProfilesLibrary.ProfileName);
                //if (!di.Exists)
                //{
                //    Directory.CreateDirectory(di.FullName);
                //}

                // newer ebx formats need the SDK for the types, so update the SDK before generating the cache
                if (ProfilesLibrary.EbxVersion > 4)
                {
                    if (TypeLibrary.GetSdkVersion() != App.FileSystemManager.Head)
                    {
                        throw new Exception("sdk update needed");
                    }
                }

                App.AssetManager.SetLogger(logger);
                App.AssetManager.Initialize(App.IsEditor);
            });

            return 0;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllname = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name;
            if (dllname.Equals("EbxClasses"))
            {
                FileInfo fi = new FileInfo(Assembly.GetExecutingAssembly().FullName);
                return Assembly.LoadFile(fi.DirectoryName + "/Profiles/" + ProfilesLibrary.SDKFilename + ".dll");
            }
            return null;
        }
    }
}
