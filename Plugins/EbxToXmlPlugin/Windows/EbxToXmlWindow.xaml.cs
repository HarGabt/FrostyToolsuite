using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FrostySdk.Managers.Entries;

namespace EbxToXmlPlugin.Windows
{
    public class FolderItem : INotifyPropertyChanged
    {
        private string name;
        private bool isSelected;
        private bool isVisible = true;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool IsVisible
        {
            get => isVisible;
            set
            {
                isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum ExportMode
    {
        All,
        SelectedOnly,
        ExcludeSelected
    }

    /// <summary>
    /// Interaction logic for DuplicateAssetWindow.xaml
    /// </summary>
    public partial class EbxToXmlWindow : FrostyDockableWindow
    {
        public ObservableCollection<FolderItem> Folders { get; set; }
        public ExportMode SelectedExportMode { get; set; }
        private CollectionViewSource foldersView;

        public EbxToXmlWindow()
        {
            InitializeComponent();
            Folders = new ObservableCollection<FolderItem>();
            SelectedExportMode = ExportMode.All;
        }

        private void FrostyDockableWindow_FrostyLoaded(object sender, EventArgs e)
        {
            LoadFolders();
        }

        private void LoadFolders()
        {
            HashSet<string> uniqueFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (EbxAssetEntry entry in App.AssetManager.EnumerateEbx())
            {
                string[] pathParts = entry.Path.Split('/');
                if (pathParts.Length > 0 && !string.IsNullOrEmpty(pathParts[0]))
                {
                    uniqueFolders.Add(pathParts[0]);
                }
            }

            foreach (string folder in uniqueFolders.OrderBy(f => f))
            {
                Folders.Add(new FolderItem { Name = folder, IsSelected = false });
            }

            foldersView = new CollectionViewSource { Source = Folders };
            foldersView.Filter += FoldersView_Filter;
            folderList.ItemsSource = foldersView.View;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ExportMode_Changed(object sender, RoutedEventArgs e)
        {
            // Check if controls are initialized to prevent null reference during construction
            if (exportAllRadio == null || exportSelectedRadio == null || skipSelectedRadio == null ||
                folderLabel == null || folderBorder == null)
                return;

            if (exportAllRadio.IsChecked == true)
            {
                SelectedExportMode = ExportMode.All;
                folderLabel.Visibility = Visibility.Collapsed;
                folderBorder.Visibility = Visibility.Collapsed;
            }
            else if (exportSelectedRadio.IsChecked == true)
            {
                SelectedExportMode = ExportMode.SelectedOnly;
                folderLabel.Text = "Select folders to export:";
                folderLabel.Visibility = Visibility.Visible;
                folderBorder.Visibility = Visibility.Visible;
            }
            else if (skipSelectedRadio.IsChecked == true)
            {
                SelectedExportMode = ExportMode.ExcludeSelected;
                folderLabel.Text = "Select folders to skip:";
                folderLabel.Visibility = Visibility.Visible;
                folderBorder.Visibility = Visibility.Visible;
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchBox != null && searchBox.Text == "Search...")
            {
                searchBox.Text = "";
                searchBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (searchBox != null && string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.Text = "Search...";
                searchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (foldersView != null && searchBox != null && searchBox.Text != "Search...")
            {
                foldersView.View.Refresh();
            }
        }

        private void FoldersView_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is FolderItem folder)
            {
                if (searchBox.Text == "Search..." || string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = folder.Name.IndexOf(searchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
        }

        public HashSet<string> GetSelectedFolders()
        {
            return new HashSet<string>(Folders.Where(f => f.IsSelected).Select(f => f.Name.ToLower()));
        }
    }
}
