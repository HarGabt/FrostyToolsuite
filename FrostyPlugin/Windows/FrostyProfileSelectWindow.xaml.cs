using Frosty.Controls;
using Frosty.Core.Controls;
using FrostySdk;
using Microsoft.Win32;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            RefreshButton.IsEnabled = false;


            await Task.Run((() =>
            {
                using (RegistryKey lmKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node"))
                {
                    int totalCount = 0;

                    IterateSubKeys(lmKey, ref totalCount);
                }
            }));

            RefreshButton.IsEnabled = true;
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
                        string nameWithoutExt = fi.Name.Replace(fi.Extension, "");

                        if (ProfilesLibrary.HasProfile(nameWithoutExt))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (FrostyConfiguration config in configurations)
                                {
                                    if (config.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                                        return;
                                }

                                Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
                                configurations.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
                            });

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