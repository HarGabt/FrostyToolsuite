using Frosty.Controls;
using Frosty.Core.Controls;
using FrostyModManager;
using FrostySdk;
using Microsoft.Win32;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Frosty.Core.Windows
{
    public partial class FrostyProfileSelectWindow
    {
        private ObservableCollection<FrostyConfiguration> configurations = new ObservableCollection<FrostyConfiguration>();
        private string selectedProfileName;
        
        public FrostyProfileSelectWindow()
        {
            InitializeComponent();

            SymLinkHelper.Initialize(string.Empty);
        }

        private async void ProfileSelectWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RemoveConfigurationButton.IsEnabled = false;
            SelectConfigurationButton.IsEnabled = false;

            RefreshConfigurationList();

            if (ConfigurationListView.Items.Count == 0)
            {
                try
                {
                    await ScanGames();
                }
                catch
                {
                    FrostyHandledExceptionBox.Show("An error occurred while scanning for games.\n\nPlease manually set the game executable(s).");
                }
            }

            RefreshConfigurationList();
        }
        
        private void RefreshConfigurationList()
        {
            Dispatcher.Invoke(() =>
            {
                configurations.Clear();

                foreach (string profile in Config.GameProfiles)
                {
                    try
                    {
                        configurations.Add(new FrostyConfiguration(profile));
                    }
                    catch (FileNotFoundException)
                    {
                        Config.RemoveGame(profile); // couldn't find the exe, so remove it from the profile list
                        Config.Save();
                    }
                }

                ConfigurationListView.ItemsSource = configurations;
            });
        }

        private void SelectConfiguration()
        {
            if (ConfigurationListView.SelectedIndex == -1)
                return;

            if (ConfigurationListView.SelectedItem is FrostyConfiguration configuration)
            {
                string version = App.Version;

                if (configuration.ProfileName == "Dragon Age The Veilguard")
                {
                    FrostyMessageBox.Show(configuration.GameName + " is not supported." + "\n\n" + "This release is never meant to support Dragon Age\u2122: The Veilguard. Use J-Lyt's release for that game.", "Unsupported Profile");
                    return;
                }
                else
                {
                    selectedProfileName = configuration.ProfileName;
                    Close();
                }
            }
        }

        private void RemoveConfiguration()
        {
            if (FrostyMessageBox.Show("Are you sure you want to remove this profile?", "Remove Profile", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FrostyConfiguration selectedItem = ConfigurationListView.SelectedItem as FrostyConfiguration;

                Config.RemoveGame(selectedItem.ProfileName);

                configurations.Remove(selectedItem);
                ConfigurationListView.Items.Refresh();

                ConfigurationListView.SelectedIndex = -1;
                Config.Save();
            }
        }

        private async Task ScanGames()
        {
            FileLogger.Info($"Scan games started. IsWine={OperatingSystemHelper.IsWine()}.");

            RefreshButton.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
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
                                IterateSubKeys(lmKey, ref totalCount);
                                FileLogger.Info($"Registry scan found {totalCount} game candidate(s).");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Info($"Registry scan failed with exception:\n{ex}");
                    }

                    if (OperatingSystemHelper.IsWine())
                    {
                        try
                        {
                            var zGames = ScanZDirectory();

                            FileLogger.Info($"Z: drive scan found {zGames.Count} game candidate(s).");

                            foreach (var filename in zGames)
                            {
                                AddGameCandidate(filename);
                            }
                        }
                        catch (Exception ex)
                        {
                            FileLogger.Info($"Z: drive scan failed with exception:\n{ex}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.Info($"Scan games failed with an unhandled exception:\n{ex}");
            }

            RefreshButton.IsEnabled = true;

            FileLogger.Info("Scan games finished.");
        }

        private void AddGameCandidate(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            string nameWithoutExt = fi.Name.Remove(fi.Name.Length - fi.Extension.Length);

            if (!ProfilesLibrary.HasProfile(nameWithoutExt))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                bool exists = configurations.Any(config => config.ProfileName == nameWithoutExt);

                if (!exists)
                {
                    Config.AddGame(nameWithoutExt, fi.DirectoryName);
                    configurations.Add(new FrostyConfiguration(nameWithoutExt));
                }
            });
        }

        private class ScanPathItem
        {
            public string Path { get; set; }
            public int Depth { get; set; }
        }

        private List<string> ScanZDirectory()
        {
            var res = new List<string>();

            var rootPath = "Z:\\home\\";

            if (!Directory.Exists(rootPath))
            {
                FileLogger.Info($"Drive '{rootPath}' was not found during scanning.");
                return res;
            }

            var queue = new Queue<ScanPathItem>();

            queue.Enqueue(new ScanPathItem { Path = rootPath, Depth = 1 });

            var mountPath = "Z:\\run\\media";
            if (Directory.Exists(mountPath))
            {
                queue.Enqueue(new ScanPathItem { Path = mountPath, Depth = 10 });
            }

            int dirsVisited = 0;

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                dirsVisited++;

                if (!Directory.Exists(item.Path))
                {
                    continue;
                }

                string[] files;

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
                    if (ProfilesLibrary.HasProfile(Path.GetFileNameWithoutExtension(file)))
                    {
                        res.Add(file);
                    }
                }

                if (item.Depth >= 20)
                {
                    continue;
                }

                string[] dirs;

                try
                {
                    dirs = Directory.GetDirectories(item.Path).Where(IsDirectoryScanValid).ToArray();
                }
                catch (Exception ex)
                {
                    FileLogger.Info($"Could not list subdirectories under '{item.Path}': {ex.Message}");
                    continue;
                }

                foreach (var dir in dirs)
                {
                    queue.Enqueue(new ScanPathItem { Path = dir, Depth = item.Depth + 1 });
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

        private void IterateSubKeys(RegistryKey subKey, ref int totalCount)
        {
            foreach (string subKeyName in subKey.GetSubKeyNames())
            {
                try
                {
                    IterateSubKeys(subKey.OpenSubKey(subKeyName), ref totalCount);
                }
                catch (System.Security.SecurityException)
                {
                    // do nothing
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
                        FileInfo fi = new FileInfo(filename);

                        if (ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - fi.Extension.Length)))
                        {
                            AddGameCandidate(filename);
                            totalCount++;
                        }
                    }
                }
            }
        }

        public static string Show(bool hasLoadedProfile = false)
        {
            string profileName = "";

            FrostyProfileSelectWindow win = new FrostyProfileSelectWindow() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
            
            profileName = win.selectedProfileName;

            return profileName;
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            ScanGames().ContinueWith(t =>
            {
                // Refresh the configuration list after scanning is done
                RefreshConfigurationList();
            });
        }
        
        private void AddConfigurationButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.exe (Game Executable)|*.exe",
                Title = "Choose Game Executable"
            };

            if (ofd.ShowDialog() == false)
            {
                FrostyMessageBox.Show("No game executable chosen.", "Frosty Core");
                return;
            }

            FileInfo fi = new FileInfo(ofd.FileName);

            // try to load game profile 
            if (!ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4)))
            {
                FrostyMessageBox.Show("There was an error when trying to load game using specified profile.", "Frosty Core");
                return;
            }

            // make sure config doesnt already exist
            foreach (FrostyConfiguration configuration in configurations)
            {
                if (configuration.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                {
                    FrostyMessageBox.Show(configuration.GameName + " already has a profile.", "Frosty Core");
                    return;
                }
            }

            // create
            Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
            configurations.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
            Config.Save();

            ConfigurationListView.Items.Refresh();
        }

        private void SelectConfigurationButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectConfiguration();
        }

        private void RemoveConfigurationButton_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveConfiguration();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Owner.Close();
        }

        private void ConfigurationListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectConfiguration();
        }

        private void ConfigurationListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveConfigurationButton.IsEnabled = true;
            SelectConfigurationButton.IsEnabled = true;

            if (SelectGameTextBlock.IsVisible)
            {
                SelectGameTextBlock.Visibility = Visibility.Collapsed;
            }
            
            if (ConfigurationListView.SelectedItem is FrostyConfiguration configuration)
            {
                ProfileNameTextBlock.Text = configuration.GameName;
                ProfilePathTextBlock.Text = configuration.GamePath;
            }
            else
            {
                ProfileNameTextBlock.Text = "";
                ProfilePathTextBlock.Text = "";
                SelectGameTextBlock.Visibility = Visibility.Visible;

                RemoveConfigurationButton.IsEnabled = false;
                SelectConfigurationButton.IsEnabled = false;
            }
        }
    }
}