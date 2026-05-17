using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using FrostySdk;
using Microsoft.Win32;
using Frosty.Controls;
using Frosty.Core;
using System.Windows.Threading;
using System.Linq;

namespace FrostyEditor.Windows
{
    /// <summary>
    /// Interaction logic for PrelaunchWindow2.xaml
    /// </summary>
    public partial class PrelaunchWindow2 : FrostyDockableWindow
    {
        private ObservableCollection<FrostyConfiguration> configurations = new ObservableCollection<FrostyConfiguration>();
        private FrostyConfiguration defaultConfiguration;

        public PrelaunchWindow2()
        {
            InitializeComponent();
        }

        private void LaunchConfiguration(string profile)
        {
            // load profiles
            if (!ProfilesLibrary.SelectProfile(profile))
            {
                FrostyMessageBox.Show(Application.Current.TryFindResource("fe_Msg_ProfileError") as string ?? "There was an error when trying to load game using specified profile.", "Frosty Editor");
                Close();
                return;
            }

            App.InitDiscordRpc();
            App.UpdateDiscordRpc("Initializing");

            // launch splash
            SplashWindow splash = new SplashWindow();
            App.Current.MainWindow = splash;
            splash.Show();
            Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RemoveConfigurationButton.IsEnabled = false;
            LaunchConfigurationButton.IsEnabled = false;

            RefreshConfigurationList();

            if (ConfigurationListView.Items.Count == 0)
            {
                try
                {
                    await ScanGames();
                }
                catch
                {
                    // do nothing
                }
            }

            RefreshConfigurationList();

            string defaultConfigurationName = Config.Get<string>("DefaultProfile", null);

            if (!string.IsNullOrEmpty(defaultConfigurationName))
            {
                defaultConfiguration = configurations.FirstOrDefault(x => x.ProfileName == defaultConfigurationName);
            }

            ConfigurationListView.SelectedItem = defaultConfiguration;
        }

        private void LaunchConfigurationButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectConfiguration();
        }

        private void ConfigurationListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveConfigurationButton.IsEnabled = true;
            LaunchConfigurationButton.IsEnabled = true;

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
                LaunchConfigurationButton.IsEnabled = false;
            }
        }

        private void ConfigurationListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectConfiguration();
        }

        private void AddConfigurationButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "*.exe (Game Executable)|*.exe",
                Title = Application.Current.TryFindResource("fe_DlgTitle_ChooseExe") as string ?? "Choose Game Executable"
            };

            if (ofd.ShowDialog() == false)
            {
                FrostyMessageBox.Show(Application.Current.TryFindResource("fe_Msg_NoGameChosen") as string ?? "No game executable chosen.", "Frosty Editor");
                return;
            }
            FileInfo fi = new FileInfo(ofd.FileName);

            // try to load game profile
            if (!ProfilesLibrary.HasProfile(fi.Name.Remove(fi.Name.Length - 4)))
            {
                FrostyMessageBox.Show(Application.Current.TryFindResource("fe_Msg_ProfileError") as string ?? "There was an error when trying to load game using specified profile.", "Frosty Editor");
                return;
            }

            // make sure config doesnt already exist
            foreach (FrostyConfiguration configuration in configurations)
            {
                if (configuration.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
                {
                    FrostyMessageBox.Show(string.Format(Application.Current.TryFindResource("fe_Msg_ProfileExists") as string ?? "{0} already has a profile.", configuration.GameName), "Frosty Editor");
                    return;
                }
            }

            if (ProfilesLibrary.ContainsEAC)
                FrostyMessageBox.Show(Application.Current.TryFindResource("fe_Msg_EasyAntiCheat") as string ?? "This game contains EasyAntiCheat and cannot automatically generate an sdk. We will not support nor assist anyone who attempts to bypass it.", "Warning");

            // create
            Config.AddGame(fi.Name.Remove(fi.Name.Length - 4), fi.DirectoryName);
            configurations.Add(new FrostyConfiguration(fi.Name.Remove(fi.Name.Length - 4)));
            Config.Save();

            ConfigurationListView.Items.Refresh();
        }

        private void ScanButton_OnClick(object sender, RoutedEventArgs e)
        {
            ScanGames().ContinueWith(t =>
            {
                // Refresh the configuration list after scanning is done
                RefreshConfigurationList();
            });
        }

        private void IterateSubKeys(RegistryKey subKey, ref int totalCount)
        {
            foreach (string subKeyName in subKey.GetSubKeyNames())
            {
                try
                {
                    IterateSubKeys(subKey.OpenSubKey(subKeyName), ref totalCount);
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
                        FileInfo fi = new FileInfo(filename);
                        string nameWithoutExt = fi.Name.Replace(fi.Extension, "");

                        if (ProfilesLibrary.HasProfile(nameWithoutExt))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (FrostyConfiguration configuration in configurations)
                                {
                                    if (configuration.ProfileName == fi.Name.Remove(fi.Name.Length - 4))
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

        private void RemoveConfigurationButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (FrostyMessageBox.Show(Application.Current.TryFindResource("fe_Msg_ConfirmRemoveProfile") as string ?? "Are you sure you want to remove this profile?", "Frosty Editor", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FrostyConfiguration selectedItem = ConfigurationListView.SelectedItem as FrostyConfiguration;

                Config.RemoveGame(selectedItem.ProfileName);

                configurations.Remove(selectedItem);
                ConfigurationListView.Items.Refresh();

                ConfigurationListView.SelectedIndex = -1;
                Config.Save();
            }
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
                    catch (System.IO.FileNotFoundException)
                    {
                        Config.RemoveGame(profile); // couldn't find the exe, so remove it from the profile list
                        Config.Save();
                    }
                }

                ConfigurationListView.ItemsSource = configurations;
            });
        }

        private async void SelectConfiguration()
        {
            if (ConfigurationListView.SelectedIndex == -1)
                return;

            if (ConfigurationListView.SelectedItem is FrostyConfiguration configuration)
            {
                string version = Frosty.Core.App.Version;

                if (configuration.ProfileName == "Dragon Age The Veilguard")
                {

                    FrostyMessageBox.Show(configuration.GameName + " is not supported." + "\n\n" + "This release Does not support Dragon Age\u2122: The Veilguard. Use J-Lyt's version of Frosty 1.0.7", "Unsupported Profile");
                    return;
                }
                else
                {
                    LaunchConfiguration(configuration.ProfileName);
                    await Task.Delay(1);
                    Close();
                }
            }
            ConfigurationListView.SelectedIndex = -1;
        }

        private async Task ScanGames()
        {
            ScanButton.IsEnabled = false;

            await Task.Run((() =>
            {
                using (RegistryKey lmKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node"))
                {
                    int totalCount = 0;

                    IterateSubKeys(lmKey, ref totalCount);
                }
            }));

            ScanButton.IsEnabled = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}