using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Legacy;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Converters;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace FrostyEditor.Windows
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private class SplashWindowLogger : ILogger, INotifyPropertyChanged
        {
            private SplashWindow parent;

            private double progress;
            private string status;

            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// The progress of the current task.
            /// </summary>
            public double Progress {
                get {
                    return progress;
                }
                set {
                    if (value != progress)
                    {
                        progress = value;
                        NotifyPropertyChanged();
                    }
                }
            }

            /// <summary>
            /// The splash window's status.
            /// </summary>
            public string Status {
                get {
                    return status;
                }
                set {
                    if (value != status)
                    {
                        status = value;
                        NotifyPropertyChanged();
                    }
                }
            }

            public SplashWindowLogger(SplashWindow inParent)
            {
                parent = inParent;

                // Utilize DataBindings to eliminate need for Dispatcher
                BindingOperations.SetBinding(parent.logTextBox, TextBlock.TextProperty, new Binding("Status")
                {
                    Source = this
                });
                BindingOperations.SetBinding(parent.progressBar, ProgressBar.ValueProperty, new Binding("Progress")
                {
                    Source = this
                });

                parent.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

                BindingOperations.SetBinding(parent.TaskbarItemInfo, TaskbarItemInfo.ProgressValueProperty, new Binding("Progress")
                {
                    Converter = new DelegateBasedValueConverter(),
                    ConverterParameter = new Func<object, object>(delegate (object value) {
                        return (double)value / 100.0;
                    }),
                    Source = this,
                });
            }

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public void Log(string text, params object[] vars)
            {
                string fullText = string.Format(text, vars);

                if (fullText.StartsWith("progress:"))
                {
                    fullText = fullText.Replace("progress:", "");
                    Progress = double.Parse(fullText);
                }
                else
                {
                    Status = fullText;
                }
            }

            public void LogError(string text, params object[] vars)
            {
            }

            public void LogWarning(string text, params object[] vars)
            {
            }
        }

        public SplashWindow()
        {
            InitializeComponent();

            versionTextBlock.Text = Frosty.Core.App.Version;
            TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // load encryption key for profiles that require it
            if (ProfilesLibrary.RequiresKey)
            {
                byte[] keyData = null;
                if (!File.Exists(ProfilesLibrary.CacheName + ".key"))
                {
                    // prompt for encryption key
                    KeyPromptWindow keyPromptWin = new KeyPromptWindow();
                    if (keyPromptWin.ShowDialog() == false)
                    {
                        FrostyMessageBox.Show("Encryption key not entered. Unable to load profile.", "Frosty Editor");
                        Close();
                        return;
                    }

                    keyData = keyPromptWin.EncryptionKey;
                    using (NativeWriter writer = new NativeWriter(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Create)))
                        writer.Write(keyData);
                }
                else
                {
                    // otherwise just read the key from file
                    keyData = NativeReader.ReadInStream(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Open, FileAccess.Read));
                }

                // add primary encryption key
                byte[] key = new byte[0x10];

                try
                {
                    Array.Copy(keyData, key, 0x10);
                    KeyManager.Instance.AddKey("Key1", key);
                }
                catch
                {
                    File.Delete(ProfilesLibrary.CacheName + ".key");
                    FrostyMessageBox.Show("Encryption key is invalid. Unable to load profile.", "Frosty Editor");
                    Close();
                    return;
                }

                if (keyData.Length > 0x10)
                {
                    try
                    {
                        // add additional encryption keys
                        key = new byte[0x10];
                        Array.Copy(keyData, 0x10, key, 0, 0x10);
                        KeyManager.Instance.AddKey("Key2", key);

                        key = new byte[0x4000];
                        Array.Copy(keyData, 0x20, key, 0, 0x4000);
                        KeyManager.Instance.AddKey("Key3", key);
                    }
                    catch
                    {
                        File.Delete(ProfilesLibrary.CacheName + ".key");
                        FrostyMessageBox.Show("Encryption key is invalid. Unable to load profile.", "Frosty Editor");
                        Close();
                        return;
                    }
                }
            }

            Config.Save();

            App.Logger.Log("Loading profile for " + ProfilesLibrary.DisplayName);

            profileTextBlock.Text = ProfilesLibrary.DisplayName;
            bannerImage.Source = LoadBanner(ProfilesLibrary.Banner);

            DirectoryInfo di = new DirectoryInfo("Caches");
            if (!Directory.Exists(di.FullName))
            {
                Directory.CreateDirectory(di.FullName);
            }
            // move any existing cache/sbdata.cas file over to the new caches directory
            foreach (var cacheName in Directory.EnumerateFiles(new FileInfo(Assembly.GetEntryAssembly().FullName).DirectoryName, "*.cache"))
            {
                FileInfo fi = new FileInfo(cacheName);
                File.Move(fi.FullName, ".\\Caches\\" + fi.Name);

                string sbDataName = fi.FullName.Replace(".cache", ".sbdata");
                if (File.Exists(sbDataName))
                    File.Move(sbDataName, ".\\Caches\\" + fi.Name.Replace(".cache", ".sbdata"));
            }

            ILogger logger = new SplashWindowLogger(this);
            AssetManagerImportResult result = new AssetManagerImportResult();

            // load data from game or cache
            await LoadData(logger, KeyManager.Instance.GetKey("Key1"), result);

            // check to make sure SDK is up to date
            if (TypeLibrary.GetSdkVersion() != App.FileSystem.Head)
            {
                var skipSdkUpdate = new List<ProfileVersion>
                {
                    ProfileVersion.Anthem
                };

                // requires updating
                if (!skipSdkUpdate.Contains((ProfileVersion)ProfilesLibrary.DataVersion) && UpdateSdk())
                {
                    Close();
                }
                if (ProfilesLibrary.EbxVersion > 4)
                {
                    // initialze assetmanager anyways
                    await FinishLoadingData(logger, result);
                }
            }

            // load strings
            await LoadLocalizedStringResourceTables(logger);
            await LoadStringList(logger);

            foreach (var startupAction in App.PluginManager.StartupActions)
            {
                await Task.Run(() =>
                {
                    startupAction.Action(logger);
                });
            }

            // show the main editor window
            MainWindow win = new MainWindow();
            App.Current.MainWindow = win;
            win.Show();

            App.Logger.Log("Initialization complete");
            App.NotificationManager.Show("Initialization complete");

            // cleanup any outstanding editor mods (in case of crash)
            FileInfo exeInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            foreach (string file in Directory.EnumerateFiles(exeInfo.DirectoryName, $"Mods/{ProfilesLibrary.ProfileName}/EditorMod*"))
            {
                File.Delete(file);
            }

            Close();

            if (result.InvalidatedDueToPatch)
            {
                // show the results of the most recent patch
                PatchSummaryWindow summaryWin = new PatchSummaryWindow(result);
                summaryWin.ShowDialog();
            }
        }

        private BitmapImage LoadBanner(byte[] banner)
        {
            if (banner == null || banner.Length == 0)
                return null;
            BitmapImage bmp = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(banner))
            {
                bmp.BeginInit();
                bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = null;
                bmp.StreamSource = ms;
                bmp.EndInit();
            }
            bmp.Freeze();
            return bmp;
        }

        private async Task<int> LoadData(ILogger logger, byte[] key, AssetManagerImportResult result)
        {
            await Task.Run(() =>
            {
                string basePath = Config.Get<string>("GamePath", null, ConfigScope.Game);

                App.FileSystem = new FileSystemManager(basePath);
                foreach (FileSystemSource source in ProfilesLibrary.Sources)
                {
                    App.FileSystem.AddSource(source.Path, source.SubDirs);
                }
                App.FileSystem.Initialize(key);

                App.ResourceManager = new ResourceManager(App.FileSystem);
                App.ResourceManager.SetLogger(logger);
                App.ResourceManager.Initialize();

                App.AssetManager = new AssetManager(App.FileSystem, App.ResourceManager);

                // Initialize plugin extensions
                TypeLibrary.Initialize();
                App.PluginManager.Initialize();

            // load legacy asset manager if profile uses legacy system

            if (ProfilesLibrary.IsLoaded(ProfileVersion.Fifa17,
                    ProfileVersion.Fifa18,
                    ProfileVersion.Madden19,
                    ProfileVersion.Fifa19,
                    ProfileVersion.Madden20,
                    ProfileVersion.Fifa20,
                    ProfileVersion.PlantsVsZombiesBattleforNeighborville))
            {
                App.AssetManager.RegisterCustomAssetManager("legacy", typeof(LegacyFileManager));
                }
                else if (ProfilesLibrary.IsLoaded(ProfileVersion.Fifa21, ProfileVersion.Madden22, ProfileVersion.Fifa22,
                    ProfileVersion.Madden23, ProfileVersion.Fifa23))
                {
                    App.AssetManager.RegisterCustomAssetManager("legacy", typeof(LegacyFileManagerV2));
                }

                // ensure mods folder is created
                DirectoryInfo di = new DirectoryInfo("Mods/" + ProfilesLibrary.ProfileName);
                if (!di.Exists)
                {
                    Directory.CreateDirectory(di.FullName);
                }

                // newer ebx formats need the SDK for the types, so update the SDK before generating the cache
                if (ProfilesLibrary.EbxVersion > 4)
                {
                    if (TypeLibrary.GetSdkVersion() != App.FileSystem.Head)
                    {
                        return;
                    }
                }

                App.AssetManager.SetLogger(logger);
                App.AssetManager.Initialize(true, result);
            });

            return 0;
        }

        private async Task<int> LoadLocalizedStringResourceTables(ILogger logger)
        {
            logger.Log("Loading localized strings");
            await Task.Run(() =>
            {
                var localizedStringDb = App.PluginManager.GetLocalizedStringDatabase();
                localizedStringDb.Initialize();
            });
            return 0;
        }

        private async Task<int> LoadStringList(ILogger logger)
        {
            logger.Log("Loading custom strings");
            await Task.Run(() => StringsManager.GetString(0));
            return 0;
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private bool UpdateSdk()
        {
            SdkUpdateWindow sdkWin = new SdkUpdateWindow(this);
            if (sdkWin.ShowDialog() == true)
            {
                return true;
            }
            else if (TypeLibrary.GetSdkVersion() == 0)
            {
                MessageBoxResult result = FrostyMessageBox.Show("Missing SDK.\nPlease generate a SDK for this game.", "Frosty", MessageBoxButton.OK);
                if (result == MessageBoxResult.OK)
                {
                    return UpdateSdk();
                }
                return true;
            }
            return false;
        }

        private async Task<int> FinishLoadingData(ILogger logger, AssetManagerImportResult result)
        {
            await Task.Run(() =>
            {
                App.AssetManager.SetLogger(logger);
                App.AssetManager.Initialize(true, result);
            });

            return 0;
        }
    }
}
