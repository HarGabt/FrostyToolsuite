using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using FrostySdk;
using Microsoft.Win32;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.IO;
using FrostySdk.Managers;

namespace FrostyModManager.Windows
{
    /// <summary>
    /// Interaction logic for PrelaunchWindow2.xaml
    /// </summary>
    public partial class PrelaunchWindow2 : FrostyDockableWindow
    {
        private List<FrostyConfiguration> configs = new List<FrostyConfiguration>();
        private FrostyConfiguration defaultConfig = null;

        Config ini = new Config();

        public PrelaunchWindow2()
        {
            InitializeComponent();

            SymLinkHelper.Initialize(string.Empty);
        }

        private void TryShowFlatpakMessage()
        {
            if (Config.Get("FlatpakMessage", OperatingSystemHelper.IsWine()))
            {
                Config.Add("FlatpakMessage", false);

                var message = "If Frosty is run through Flatpak application (Bottles, Lutris, Heroic), then make sure to select 'All user files' in Flatseal for that application.";
                message += "\r\n\r\nOtherwise Frosty Mod Manager might crash.";

                FrostyMessageBox.Show(message, "Frosty Mod Manager");
            }
        }

        private void LaunchConfig(string profile)
        {
            // load profiles
            if (!ProfilesLibrary.SelectProfile(profile))
            {
                FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Mod Manager");
                Close();
                return;
            }

            // launch Mod Manager
            SplashWindow splashWin = new SplashWindow();
            App.Current.MainWindow = splashWin;
            splashWin.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshConfigurationList();

            RemoveConfigButton.IsEnabled = false;
            LaunchConfigButton.IsEnabled = false;

            string defaultConfigName = Config.Get<string>("DefaultProfile", null);

            foreach (FrostyConfiguration name in configs)
            {
                if (name.ProfileName == defaultConfigName)
                {
                    defaultConfig = name;
                }
            }

            ConfigList.SelectedItem = defaultConfig;
        }

        private void RefreshConfigurationList()
        {
            configs.Clear();

            foreach (string profile in Config.GameProfiles)
            {
                try
                {
                    configs.Add(new FrostyConfiguration(profile));
                }
                catch (System.IO.FileNotFoundException)
                {
                    Config.RemoveGame(profile); // couldn't find the exe, so remove it from the profile list
                    Config.Save();
                }
            }

            ConfigList.ItemsSource = configs;
        }

        private void ConfigList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveConfigButton.IsEnabled = true;
            LaunchConfigButton.IsEnabled = true;
        }

        private void NewConfigButton_Click(object sender, RoutedEventArgs e)
        {
            TryShowFlatpakMessage();

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.exe (Game Executable)|*.exe",
                Title = "Choose Game Executable"
            };

            if (ofd.ShowDialog() == false)
            {
                FrostyMessageBox.Show("No game executable chosen.", "Frosty Mod Manager");
                return;
            }

            if (OperatingSystemHelper.IsWine() && !DriveHelper.IsZDrive(ofd.FileName))
            {
                var sb = new StringBuilder();
                sb.Append("Game is not located on Wine Z: drive, which is not recommended.\r\n\r\n");
                sb.Append("This can cause broken sym-links and game not booting, especially when game is launched through sandboxed environment like flatpak.");
                sb.Append("\r\n\r\nAdd game via Z: drive for better stability.");

                FrostyMessageBox.Show(sb.ToString(), "Frosty Mod Manager");
            }

            AddGameProfile(ofd.FileName, out var errorMessage);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                FrostyMessageBox.Show(errorMessage, "Frosty Mod Manager");
            }

            ConfigList.Items.Refresh();
        }

        private static bool CheckGameProfile(string path)
        {
            FileInfo fi = new FileInfo(path);

            return ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4));
        }

        private void AddGameProfile(string path, out string errorMessage)
        {
            errorMessage = string.Empty;

            FileInfo fi = new FileInfo(path);

            // try to load game profile
            if (!ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4)))
            {
                errorMessage = "There was an error when trying to load game using specified profile.";
                return;
            }

            // make sure config doesnt already exist
            foreach (FrostyConfiguration config in configs)
            {
                if (config.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                {
                    errorMessage = "That game already has a configuration.";
                    return;
                }
            }

            // create
            Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
            configs.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
            Config.Save();
        }

        private async void ConfigList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConfigList.SelectedIndex == -1)
                return;

            if (ConfigList.SelectedItem is FrostyConfiguration config)
            {
                LaunchConfig(config.ProfileName);
                await Task.Delay(1);
                Close();
            }
            ConfigList.SelectedIndex = -1;
        }

        private void RemoveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrostyMessageBox.Show("Are you sure you want to delete this configuration?", "Frosty Mod Manager", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FrostyConfiguration selectedItem = ConfigList.SelectedItem as FrostyConfiguration;

                Config.RemoveGame(selectedItem.ProfileName);

                configs.Remove(selectedItem);
                ConfigList.Items.Refresh();

                ConfigList.SelectedIndex = 0;
                Config.Save();
            }
        }

        private async void LaunchConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigList.SelectedIndex == -1)
                return;

            if (ConfigList.SelectedItem is FrostyConfiguration config)
            {
                LaunchConfig(config.ProfileName);
                await Task.Delay(1);
                Close();
            }
            ConfigList.SelectedIndex = -1;
        }

        private void ScanForGamesButton_Click(object sender, RoutedEventArgs e)
        {
            FileLogger.Info($"Scan for games button clicked. IsWine={OperatingSystemHelper.IsWine()}.");

            try
            {
                ScanForGames();
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Scan for games failed with an unhandled exception:\n{ex}");
                FrostyMessageBox.Show($"Scanning for games failed with an error:\n\n{ex.Message}\n\nSee executor.log for details.", "Frosty Mod Manager");
            }
        }

        private void ScanForGames()
        {
            TryShowFlatpakMessage();

            var games = new List<string>();

            CancellationTokenSource cancelToken = new CancellationTokenSource();

            FrostyTaskWindow.Show("Scanning for games", "", (task) =>
            {
                task.TaskLogger.Log("Scanning registry...");

                try
                {
                    using (RegistryKey lmKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node"))
                    {
                        if (lmKey == null)
                        {
                            FileLogger.Info("Registry scan: 'SOFTWARE\\WOW6432Node' could not be opened, skipping registry scan.");
                        }
                        else
                        {
                            int totalCount = 0;

                            var regGames = IterateSubKeys(lmKey, ref totalCount);

                            FileLogger.Info($"Registry scan found {regGames.Count} game candidate(s).");

                            games.AddRange(regGames);
                        }
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Info($"Registry scan failed with exception:\n{ex}");
                }

                if (OperatingSystemHelper.IsWine())
                {
                    task.TaskLogger.Log("Scanning Z: drive...");

                    try
                    {
                        var zGames = ScanZDirectory(cancelToken);

                        FileLogger.Info($"Z: drive scan found {zGames.Count} game candidate(s).");

                        games.AddRange(zGames);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Info($"Z: drive scan failed with exception:\n{ex}");
                    }
                }
            }, showCancelButton: true, cancelCallback: (task) => cancelToken.Cancel());

            games = games.Select(x => x.Trim()).Distinct().ToList();

            games.Sort((x, y) => string.Compare(x, y, true) * -1);

            FileLogger.Info($"Scan finished with {games.Count} total candidate(s).");

            foreach (var game in games)
            {
                FileLogger.Info($"Scanning found game candidate: '{game}'.");

                AddGameProfile(game, out _);
            }

            ConfigList.Items.Refresh();
        }

        private class PathItem
        {
            public string Path { get; set; }
            public int Depth { get; set; }
        }

        private List<string> ScanZDirectory(CancellationTokenSource cancelToken)
        {
            var res = new List<string>();

            var rootPath = "Z:\\home\\";

            if (!Directory.Exists(rootPath))
            {
                FileLogger.Info($"Drive '{rootPath}' was not found during scanning.");
                return res;
            }

            var queue = new Queue<PathItem>();

            queue.Enqueue(new PathItem { Path = rootPath, Depth = 1 });

            var mountPath = "Z:\\run\\media";
            if (Directory.Exists(mountPath))
            {
                queue.Enqueue(new PathItem { Path = mountPath, Depth = 10 });
            }

            string[] files;
            string[] dirs;
            int dirsVisited = 0;

            while (queue.Count > 0)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    FileLogger.Info($"Z: drive scan cancelled after visiting {dirsVisited} directories.");
                    return res;
                }

                var item = queue.Dequeue();
                dirsVisited++;

                if (!Directory.Exists(item.Path))
                {
                    continue;
                }

                try
                {
                    files = Directory.GetFiles(item.Path, "*.exe");
                }
                catch (Exception ex)
                {
                    FileLogger.Info($"Could not list files under '{item.Path}': {ex.Message}");
                    continue;
                }

                foreach (var file in files)
                {
                    if (CheckGameProfile(file))
                    {
                        res.Add(file);
                    }
                }

                if (item.Depth >= 20)
                {
                    continue;
                }

                dirs = new string[0];

                try
                {
                    dirs = Directory.GetDirectories(item.Path).Where(d => IsDirectoryScanValid(d)).ToArray();
                }
                catch (Exception ex)
                {
                    FileLogger.Info($"Could not list subdirectories under '{item.Path}': {ex.Message}");
                    continue;
                }

                foreach (var dir in dirs)
                {
                    queue.Enqueue(new PathItem { Path = dir, Depth = item.Depth + 1 });
                }
            }

            FileLogger.Info($"Z: drive scan visited {dirsVisited} directories, found {res.Count} candidate(s).");

            return res;
        }

        private static bool IsDirectoryScanValid(string dir)
        {
            var dirTempName = Path.GetFileName(dir);

            if (string.IsNullOrWhiteSpace(dirTempName))
            {
                return false;
            }

            dirTempName = dirTempName.Trim().ToLower();

            if (dirTempName.StartsWith("$"))
            {
                return false;
            }

            if (dirTempName == "cache" || dirTempName == "config" || dirTempName == "tmp")
            {
                return false;
            }

            // .steam is where distro-packaged Steam (e.g. Debian/Ubuntu's steam package)
            // keeps its real steamapps directory, not just a symlink shim like upstream
            // Steam installs use, so it needs to stay scannable alongside .local/.var.
            if (dirTempName.StartsWith(".") && dirTempName != ".local" && dirTempName != ".var" && dirTempName != ".steam")
            {
                return false;
            }

            try
            {
                if (SymLinkHelper.IsSymbolicLink(dir))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Don't let a single symlink-check failure abort scanning the rest of
                // this directory's siblings - just log it and treat this one as valid.
                FileLogger.Info($"Symbolic link check failed for '{dir}', treating it as a regular directory. Details: {ex.Message}");
            }

            return true;
        }

        private List<string> IterateSubKeys(RegistryKey subKey, ref int totalCount)
        {
            var res = new List<string>();

            foreach (string subKeyName in subKey.GetSubKeyNames())
            {
                try
                {
                    res.AddRange(IterateSubKeys(subKey.OpenSubKey(subKeyName), ref totalCount));
                }
                catch (System.Exception)
                {
                    continue;
                }
            }

            foreach (string subKeyValue in subKey.GetValueNames())
            {
                if (subKeyValue.IndexOf("Install Dir", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    string installDir = subKey.GetValue("Install Dir") as string;
                    if (string.IsNullOrEmpty(installDir))
                        continue;
                    if (!Directory.Exists(installDir))
                        continue;

                    foreach (string filename in Directory.EnumerateFiles(installDir, "*.exe"))
                    {
                        if (CheckGameProfile(filename))
                        {
                            res.Add(filename);

                            totalCount++;
                        }
                    }
                }
            }

            return res;
        }
    }
}
