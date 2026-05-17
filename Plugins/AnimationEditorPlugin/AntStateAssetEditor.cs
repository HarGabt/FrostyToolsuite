using AssetBankPlugin.Ant;
using AssetBankPlugin.Export;
using AssetBankPlugin.GenericData;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AssetBankPlugin
{
    // --- VIEW MODELS ---

    public class AntTypeGroup
    {
        public string TypeName { get; set; }
        public int ItemCount => Assets.Count;
        public ObservableCollection<AntAssetViewModel> Assets { get; set; } = new ObservableCollection<AntAssetViewModel>();
    }

    public class AntAssetViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public object AssetInstance { get; set; }
        public AntStateAssetEditor ParentEditor { get; set; }

        private bool _isSelectedForExport;
        public bool IsSelectedForExport
        {
            get => _isSelectedForExport;
            set
            {
                _isSelectedForExport = value;
                OnPropertyChanged(nameof(IsSelectedForExport));

                // Direct call to parent editor
                ParentEditor?.NotifyExportStatus();
            }
        }

        private bool _showCheckbox;
        public Visibility CheckboxVisibility => (_showCheckbox && AssetInstance is AnimationAsset) ? Visibility.Visible : Visibility.Collapsed;

        public void SetBulkMode(bool show)
        {
            _showCheckbox = show;
            if (!show) IsSelectedForExport = false;
            OnPropertyChanged(nameof(CheckboxVisibility));
        }

        public ICommand CopyGuidCommand => new RelayCommand((o) => Clipboard.SetText(Id.ToString()));
        public Visibility ExportVisibility => (AssetInstance is AnimationAsset) ? Visibility.Visible : Visibility.Collapsed;
        public ICommand ExportCommand => new RelayCommand((o) => ExportAsset());

        private void ExportAsset()
        {
            if (!(AssetInstance is AnimationAsset anim)) return;

            var opt = new AnimationOptions();
            opt.Load();

            if (string.IsNullOrEmpty(opt.ExportSkeletonAsset))
            {
                FrostyMessageBox.Show("Please set an Export Skeleton in Options first.", "Missing Skeleton");
                return;
            }

            FrostySaveFileDialog sfd = new FrostySaveFileDialog("Save Animation", "*.seanim (SEAnim)|*.seanim", "SEAnim", Name);
            if (sfd.ShowDialog())
            {
                string exportDirectory = Path.GetDirectoryName(sfd.FileName);
                string assetName = Name;

                FrostyTaskWindow.Show("Exporting Animation", "", (task) =>
                {
                    // We run the EBX Loading and Exporting on the UI thread because 
                    // Frosty's EBX descriptors and Logger are NOT thread-safe.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            EbxAssetEntry skelEntry = App.AssetManager.GetEbxEntry(opt.ExportSkeletonAsset);
                            var skelEbx = App.AssetManager.GetEbx(skelEntry);
                            dynamic skel = skelEbx.RootObject;
                            var skeleton = SkeletonAssetExport.ConvertToInternal(skel);

                            anim.Name = assetName;
                            anim.Channels = anim.GetChannels(anim.ChannelToDofAsset);
                            var intern = anim.ConvertToInternal();

                            if (intern != null)
                            {
                                new AnimationExporterSEANIM().Export(intern, skeleton, exportDirectory);
                                App.Logger.Log($"[AntStateEditor] Successfully exported {assetName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Logger.LogError($"[AntStateEditor] Failed to export {assetName}: {ex.Message}");
                        }
                    });
                });
            }
        }

        private ObservableCollection<AntPropertyViewModel> _properties;
        public ObservableCollection<AntPropertyViewModel> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new ObservableCollection<AntPropertyViewModel>();
                    if (AssetInstance != null)
                    {
                        Type type = AssetInstance.GetType();

                        foreach (var p in ReflectionHelper.GetAllProperties(type))
                        {
                            if (p.Name == "Name" || p.Name == "ID" || p.Name == "Bank") continue;
                            try { _properties.Add(new AntPropertyViewModel(p.Name, p.GetValue(AssetInstance))); } catch { }
                        }

                        foreach (var f in ReflectionHelper.GetAllFields(type))
                        {
                            if (f.Name.Contains("<") || f.Name.Contains("k__BackingField")) continue;
                            try { _properties.Add(new AntPropertyViewModel(f.Name, f.GetValue(AssetInstance))); } catch { }
                        }
                    }
                }
                return _properties;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class AntPropertyViewModel
    {
        public string Name { get; set; }
        public string ValueStr { get; set; }
        public string TypeStr { get; set; }
        public object ValueObj { get; set; }

        // Command to copy the property's value to clipboard
        public ICommand CopyValueCommand => new RelayCommand((o) =>
        {
            if (!string.IsNullOrEmpty(ValueStr))
            {
                try { Clipboard.SetText(ValueStr); } catch { }
            }
        });

        public AntPropertyViewModel(string name, object val)
        {
            Name = name;
            ValueObj = val;
            TypeStr = val != null ? GetFriendlyTypeName(val.GetType()) : "Null";
            ValueStr = GetString(val);
        }

        private string GetFriendlyTypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                return $"List of {t.GetGenericArguments()[0].Name}";
            return t.Name;
        }

        private string GetString(object obj)
        {
            if (obj == null) return "null";
            Type t = obj.GetType();

            if (t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t.IsEnum)
                return obj.ToString();

            if (obj is IEnumerable enumerable)
            {
                int count = 0;
                var enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext()) count++;
                return $"[{count} Items]";
            }

            return $"[{t.Name}]";
        }

        private ObservableCollection<AntPropertyViewModel> _children;
        public ObservableCollection<AntPropertyViewModel> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new ObservableCollection<AntPropertyViewModel>();
                    if (ValueObj != null)
                    {
                        Type t = ValueObj.GetType();
                        if (!(t.IsPrimitive || t == typeof(string) || t == typeof(Guid) || t.IsEnum))
                        {
                            if (ValueObj is IEnumerable enumerable)
                            {
                                int i = 0;
                                foreach (var item in enumerable)
                                {
                                    _children.Add(new AntPropertyViewModel($"[{i}]", item));
                                    i++;
                                }
                            }
                            else
                            {
                                foreach (var p in ReflectionHelper.GetAllProperties(t))
                                {
                                    try { _children.Add(new AntPropertyViewModel(p.Name, p.GetValue(ValueObj))); } catch { }
                                }
                                foreach (var f in ReflectionHelper.GetAllFields(t))
                                {
                                    if (f.Name.Contains("<") || f.Name.Contains("k__BackingField")) continue;
                                    try { _children.Add(new AntPropertyViewModel(f.Name, f.GetValue(ValueObj))); } catch { }
                                }
                            }
                        }
                    }
                }
                return _children;
            }
        }
    }

    public static class ReflectionHelper
    {
        public static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null) return Enumerable.Empty<FieldInfo>();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return t.GetFields(flags).Concat(GetAllFields(t.BaseType));
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(Type t)
        {
            if (t == null) return Enumerable.Empty<PropertyInfo>();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            return t.GetProperties(flags).Concat(GetAllProperties(t.BaseType));
        }
    }

    // --- EDITOR LOGIC ---

    [TemplatePart(Name = "PART_SearchBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_AssetTreeView", Type = typeof(TreeView))]
    [TemplatePart(Name = "PART_LoadingOverlay", Type = typeof(Border))]
    [TemplatePart(Name = "PART_LoadingText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_BulkToggle", Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
    [TemplatePart(Name = "PART_ExportBulkButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_AddRefButton", Type = typeof(Button))]
    public class AntStateAssetEditor : FrostyAssetEditor, INotifyPropertyChanged
    {
        private TextBox m_searchBox;
        private TreeView m_assetTreeView;
        private Border m_loadingOverlay;
        private TextBlock m_loadingText;
        private System.Windows.Controls.Primitives.ToggleButton m_bulkToggle;

        private List<AntTypeGroup> _masterGroups = new List<AntTypeGroup>();
        private ObservableCollection<AntTypeGroup> _filteredGroups = new ObservableCollection<AntTypeGroup>();

        private AntAssetViewModel _selectedAsset;
        private bool _isBulkMode = false;

        public bool IsExportEnabled
        {
            get
            {
                if (_isBulkMode)
                    return _masterGroups.SelectMany(g => g.Assets).Any(a => a.IsSelectedForExport);

                return _selectedAsset != null && _selectedAsset.AssetInstance is AnimationAsset;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyExportStatus() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExportEnabled)));

        static AntStateAssetEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AntStateAssetEditor), new FrameworkPropertyMetadata(typeof(AntStateAssetEditor)));
        }

        public AntStateAssetEditor(ILogger inLogger) : base(inLogger) { }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            m_searchBox = GetTemplateChild("PART_SearchBox") as TextBox;
            m_assetTreeView = GetTemplateChild("PART_AssetTreeView") as TreeView;
            m_loadingOverlay = GetTemplateChild("PART_LoadingOverlay") as Border;
            m_loadingText = GetTemplateChild("PART_LoadingText") as TextBlock;
            m_bulkToggle = GetTemplateChild("PART_BulkToggle") as System.Windows.Controls.Primitives.ToggleButton;
            var exportBulkBtn = GetTemplateChild("PART_ExportBulkButton") as Button;
            var addRefBtn = GetTemplateChild("PART_AddRefButton") as Button;

            if (m_bulkToggle != null)
            {
                m_bulkToggle.Checked += (s, e) => { _isBulkMode = true; ToggleBulkMode(true); NotifyExportStatus(); };
                m_bulkToggle.Unchecked += (s, e) => { _isBulkMode = false; ToggleBulkMode(false); NotifyExportStatus(); };
            }

            if (exportBulkBtn != null)
            {
                exportBulkBtn.Click += (s, e) => ExportSelected();
            }

            if (addRefBtn != null)
            {
                addRefBtn.Click += (s, e) => OpenReferenceSelector();
            }

            if (m_assetTreeView != null)
            {
                m_assetTreeView.ItemsSource = _filteredGroups;
                m_assetTreeView.SelectedItemChanged += (s, e) =>
                {
                    _selectedAsset = e.NewValue as AntAssetViewModel;
                    NotifyExportStatus();
                };
            }

            if (m_searchBox != null) m_searchBox.TextChanged += (s, e) => ApplySearchFilter(m_searchBox.Text);

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                SetLoadingState(true, "Locating data stream...");

                dynamic antStateAsset = RootObject;
                Stream s = null;
                int bundleId = 0;

                // Dead Space (2023) uses AssetBankResource (ulong res-hash), not StreamingGuid.
                if (ProfilesLibrary.IsLoaded(ProfileVersion.DeadSpace))
                {
                    try
                    {
                        if (antStateAsset.AssetBankResource != (ulong)0)
                        {
                            var res = App.AssetManager.GetResEntry(antStateAsset.AssetBankResource);
                            if (res != null) { bundleId = res.Bundles[0]; s = App.AssetManager.GetRes(res); }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"[AntStateEditor] Dead Space: Failed to load AssetBankResource: {ex.Message}");
                    }
                }
                else if (antStateAsset.StreamingGuid == Guid.Empty)
                {
                    var res = App.AssetManager.GetResEntry(AssetEntry.Name);
                    if (res != null) { bundleId = res.Bundles[0]; s = App.AssetManager.GetRes(res); }
                }
                else
                {
                    var chunk = App.AssetManager.GetChunkEntry(antStateAsset.StreamingGuid);
                    if (chunk != null) { bundleId = chunk.Bundles[0]; s = App.AssetManager.GetChunk(chunk); }
                }

                if (s == null)
                {
                    SetLoadingState(false, "");
                    logger.LogError("Failed to locate AntState stream.");
                    return;
                }

                SetLoadingState(true, $"Parsing {s.Length} bytes...");

                Bank bank = await Task.Run(() =>
                {
                    using (var reader = new NativeReader(s)) { return new Bank(reader, bundleId); }
                });

                SetLoadingState(true, "Building asset hierarchy...");

                _masterGroups = await Task.Run(() =>
                {
                    var tempGroups = new Dictionary<string, AntTypeGroup>();

                    foreach (var kvp in bank.DataNames)
                    {
                        var antAsset = AntRefTable.Get(kvp.Value);
                        string typeName = antAsset != null ? antAsset.GetType().Name : "Unknown";

                        if (!tempGroups.ContainsKey(typeName))
                            tempGroups[typeName] = new AntTypeGroup { TypeName = typeName };

                        /// Pass the editor reference to each ViewModel
                        tempGroups[typeName].Assets.Add(new AntAssetViewModel
                        {
                            ParentEditor = this,
                            Name = kvp.Key,
                            Id = kvp.Value,
                            AssetInstance = antAsset
                        });
                    }

                    var sortedGroups = tempGroups.Values.OrderBy(g => g.TypeName).ToList();
                    foreach (var group in sortedGroups)
                    {
                        group.Assets = new ObservableCollection<AntAssetViewModel>(group.Assets.OrderBy(a => a.Name));
                    }
                    return sortedGroups;
                });

                Dispatcher.Invoke(() => ApplySearchFilter(""));
                SetLoadingState(false, "");
            }
            catch (Exception ex)
            {
                SetLoadingState(false, "");
                logger.LogError($"[AntStateEditor] {ex.Message}");
            }
        }

        private void SetLoadingState(bool isVisible, string text)
        {
            Dispatcher.Invoke(() =>
            {
                if (m_loadingOverlay != null) m_loadingOverlay.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                if (m_loadingText != null) m_loadingText.Text = text;
            });
        }

        private void ApplySearchFilter(string query)
        {
            _filteredGroups.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                foreach (var group in _masterGroups) _filteredGroups.Add(group);
                return;
            }

            query = query.ToLower();

            foreach (var group in _masterGroups)
            {
                var matchingAssets = group.Assets.Where(a =>
                    a.Name.ToLower().Contains(query) ||
                    a.Id.ToString().ToLower().Contains(query)).ToList();

                if (matchingAssets.Count > 0)
                {
                    _filteredGroups.Add(new AntTypeGroup
                    {
                        TypeName = group.TypeName,
                        Assets = new ObservableCollection<AntAssetViewModel>(matchingAssets)
                    });
                }
            }
        }

        private void ToggleBulkMode(bool isBulk)
        {
            foreach (var group in _masterGroups)
            {
                foreach (var asset in group.Assets)
                {
                    asset.SetBulkMode(isBulk);
                }
            }
        }

        private void ExportSelected()
        {
            List<AntAssetViewModel> assetsToExport = new List<AntAssetViewModel>();
            if (_isBulkMode)
            {
                assetsToExport = _masterGroups.SelectMany(g => g.Assets)
                                                .Where(a => a.IsSelectedForExport && a.AssetInstance is AnimationAsset)
                                                .ToList();
            }
            else if (_selectedAsset != null && _selectedAsset.AssetInstance is AnimationAsset)
            {
                assetsToExport.Add(_selectedAsset);
            }

            if (assetsToExport.Count == 0) return;

            var opt = new AnimationOptions();
            opt.Load();
            if (string.IsNullOrEmpty(opt.ExportSkeletonAsset))
            {
                FrostyMessageBox.Show("Please set an Export Skeleton in Options first.", "Missing Skeleton");
                return;
            }

            FrostySaveFileDialog sfd = new FrostySaveFileDialog("Select Export Directory", "*.seanim (SEAnim)|*.seanim", "SEAnim", "FolderSelection");
            if (!sfd.ShowDialog()) return;
            string exportDirectory = Path.GetDirectoryName(sfd.FileName);

            FrostyTaskWindow.Show($"Exporting {assetsToExport.Count} Animations", "", (task) =>
            {
                // 1. Get Skeleton once. 
                // We must do this on UI thread because GetEbx can trigger WPF property changes.
                InternalSkeleton skeleton = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var skelEntry = App.AssetManager.GetEbxEntry(opt.ExportSkeletonAsset);
                    var skelEbx = App.AssetManager.GetEbx(skelEntry);
                    skeleton = SkeletonAssetExport.ConvertToInternal(skelEbx.RootObject);
                });

                int progress = 0;
                foreach (var asset in assetsToExport)
                {
                    string assetName = asset.Name;
                    var anim = asset.AssetInstance as AnimationAsset;

                    task.Update($"Exporting {assetName}...", ((double)progress / assetsToExport.Count) * 100.0);

                    // 2. Process and Export each animation on the UI thread.
                    // task.Update is thread-safe, but the rest of this isn't.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            anim.Name = assetName;
                            anim.Channels = anim.GetChannels(anim.ChannelToDofAsset);
                            var intern = anim.ConvertToInternal();

                            if (intern != null)
                            {
                                new AnimationExporterSEANIM().Export(intern, skeleton, exportDirectory);
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Logger.LogError($"[AntStateEditor] Failed {assetName}: {ex.Message}");
                        }
                    });

                    progress++;
                }
                App.Logger.Log($"[AntStateEditor] Exported {assetsToExport.Count} assets.");
            });
        }

        public override List<ToolbarItem> RegisterToolbarItems()
        {
            return new List<ToolbarItem>
            {
                new ToolbarItem("Refresh", "Reload data", null, new RelayCommand((state) => _ = LoadAsync()))
            };
        }

        private void OpenReferenceSelector()
        {
            var selectionWindow = new BankSelectionWindow
            {
                Owner = Window.GetWindow(this)
            };

            if (selectionWindow.ShowDialog() == true)
            {
                var selectedBanks = selectionWindow.SelectedEntries.ToList();
                if (selectedBanks.Count == 0) return;

                FrostyTaskWindow.Show("Loading Reference Banks", "", (task) =>
                {
                    int progress = 0;
                    foreach (var entry in selectedBanks)
                    {
                        task.Update($"Caching {entry.Name}...", ((double)progress / selectedBanks.Count) * 100.0);

                        try
                        {
                            EbxAsset asset = App.AssetManager.GetEbx(entry);
                            dynamic antStateAsset = asset.RootObject;
                            Stream s = null;
                            int bundleId = entry.Bundles.Count > 0 ? entry.Bundles[0] : 0;

                            // Dead Space (2023) uses AssetBankResource (ulong res-hash), not StreamingGuid.
                            if (ProfilesLibrary.IsLoaded(ProfileVersion.DeadSpace))
                            {
                                if (antStateAsset.AssetBankResource != (ulong)0)
                                {
                                    var res = App.AssetManager.GetResEntry(antStateAsset.AssetBankResource);
                                    if (res != null) { bundleId = res.Bundles[0]; s = App.AssetManager.GetRes(res); }
                                }
                            }
                            else if (antStateAsset.StreamingGuid == Guid.Empty)
                            {
                                var res = App.AssetManager.GetResEntry(entry.Name);
                                if (res != null) s = App.AssetManager.GetRes(res);
                            }
                            else
                            {
                                var chunk = App.AssetManager.GetChunkEntry(antStateAsset.StreamingGuid);
                                if (chunk != null) { bundleId = chunk.Bundles[0]; s = App.AssetManager.GetChunk(chunk); }
                            }

                            if (s != null)
                            {
                                using (var reader = new NativeReader(s))
                                {
                                    // Constructing the Bank triggers deserialization and populates AntRefTable cache
                                    _ = new Bank(reader, bundleId);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Logger.LogError($"[AntStateEditor] Failed to load reference bank {entry.Name}: {ex.Message}");
                        }
                        progress++;
                    }
                });

                App.Logger.Log($"[AntStateEditor] Successfully loaded {selectedBanks.Count} reference banks into cache.");
            }
        }
    }

    // REFERENCE BANK SELECTOR (Virtualization + Data Binding)

    public class BankNode : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public bool IsFolder { get; set; }
        public EbxAssetEntry Entry { get; set; }
        public ObservableCollection<BankNode> Children { get; set; } = new ObservableCollection<BankNode>();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                // Update the icon when expanded/collapsed
                if (IsFolder) OnPropertyChanged(nameof(Icon));
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                if (Entry != null)
                    BankSelectionWindow.UpdateSelection(Entry, value);
            }
        }

        public ImageSource Icon => IsFolder
            ? (IsExpanded ? BankSelectionWindow.OpenFolderIcon : BankSelectionWindow.FolderIcon)
            : null;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public class BankSelectionWindow : FrostyWindow
    {
        public static ImageSource FolderIcon = new BitmapImage(new Uri("pack://application:,,,/FrostyEditor;component/Images/CloseFolder.png"));
        public static ImageSource OpenFolderIcon = new BitmapImage(new Uri("pack://application:,,,/FrostyEditor;component/Images/OpenFolder.png"));

        private static HashSet<EbxAssetEntry> _selectedCache = new HashSet<EbxAssetEntry>();

        private TreeView _treeView;
        private FrostyWatermarkTextBox _searchBox;
        private List<EbxAssetEntry> _allEntries;
        private ObservableCollection<BankNode> _rootNodes = new ObservableCollection<BankNode>();

        public IEnumerable<EbxAssetEntry> SelectedEntries => _selectedCache;

        public static void UpdateSelection(EbxAssetEntry entry, bool isSelected)
        {
            if (isSelected) _selectedCache.Add(entry);
            else _selectedCache.Remove(entry);
        }

        public BankSelectionWindow()
        {
            Title = "  Select Reference Banks (AntStateAsset)";
            Width = 550;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Define converters locally in C# to prevent "Resource Not Found" errors
            var boolToVis = new BooleanToVisibilityConverter();
            var inverseBoolToVis = new InverseBoolToVisConverter();

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) });

            _searchBox = new FrostyWatermarkTextBox
            {
                Margin = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                WatermarkText = "search ant state assets by name"
            };
            _searchBox.TextChanged += (s, e) => BuildTree();
            Grid.SetRow(_searchBox, 0);
            grid.Children.Add(_searchBox);

            _treeView = new TreeView
            {
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderThickness = new Thickness(0),
                ItemsSource = _rootNodes
            };
            VirtualizingStackPanel.SetIsVirtualizing(_treeView, true);
            VirtualizingStackPanel.SetVirtualizationMode(_treeView, VirtualizationMode.Recycling);

            var template = new HierarchicalDataTemplate(typeof(BankNode)) { ItemsSource = new Binding("Children") };
            var factory = new FrameworkElementFactory(typeof(StackPanel));
            factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            factory.SetValue(StackPanel.MarginProperty, new Thickness(0, 2, 0, 2));

            // Folder icon
            var imgFactory = new FrameworkElementFactory(typeof(Image));
            imgFactory.SetBinding(Image.SourceProperty, new Binding("Icon"));
            imgFactory.SetValue(Image.WidthProperty, 16.0);
            imgFactory.SetValue(Image.HeightProperty, 16.0);
            imgFactory.SetBinding(Image.VisibilityProperty, new Binding("IsFolder") { Converter = boolToVis });

            // CheckBox for assets
            var cbFactory = new FrameworkElementFactory(typeof(CheckBox));
            cbFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
            cbFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            cbFactory.SetBinding(CheckBox.VisibilityProperty, new Binding("IsFolder") { Converter = inverseBoolToVis });

            // Text block
            var txtFactory = new FrameworkElementFactory(typeof(TextBlock));
            txtFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            txtFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
            txtFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            txtFactory.SetValue(TextBlock.MarginProperty, new Thickness(6, 0, 0, 0));
            txtFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            factory.AppendChild(imgFactory);
            factory.AppendChild(cbFactory);
            factory.AppendChild(txtFactory);
            template.VisualTree = factory;
            _treeView.ItemTemplate = template;

            var style = new Style(typeof(TreeViewItem), (Style)Application.Current.FindResource(typeof(TreeViewItem)));
            style.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty, new Binding("IsExpanded") { Mode = BindingMode.TwoWay }));
            _treeView.ItemContainerStyle = style;

            Grid.SetRow(_treeView, 1);
            grid.Children.Add(_treeView);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var loadBtn = new Button { Content = "Load Selected", Width = 120, Height = 26, Margin = new Thickness(0, 0, 15, 0) };
            loadBtn.Click += (s, e) => { DialogResult = true; Close(); };
            var cancelBtn = new Button { Content = "Cancel", Width = 100, Height = 26 };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };

            btnPanel.Children.Add(loadBtn);
            btnPanel.Children.Add(cancelBtn);
            Grid.SetRow(btnPanel, 2);
            grid.Children.Add(btnPanel);

            Content = grid;
            _allEntries = App.AssetManager.EnumerateEbx("AntStateAsset").OrderBy(x => x.Path).ThenBy(x => x.Filename).ToList();
            BuildTree();
        }

        private void BuildTree()
        {
            _rootNodes.Clear();
            string query = _searchBox.Text?.ToLower() ?? "";
            bool isSearching = !string.IsNullOrWhiteSpace(query);
            var folders = new Dictionary<string, BankNode>();

            foreach (var entry in _allEntries)
            {
                if (isSearching && !entry.Name.ToLower().Contains(query) && !entry.Path.ToLower().Contains(query))
                    continue;

                string[] parts = entry.Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                BankNode currentParent = null;
                string currentPath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    currentPath += parts[i] + "/";
                    if (!folders.TryGetValue(currentPath, out BankNode folderNode))
                    {
                        folderNode = new BankNode { Name = parts[i], IsFolder = true, IsExpanded = isSearching };
                        if (currentParent == null) _rootNodes.Add(folderNode);
                        else currentParent.Children.Add(folderNode);
                        folders[currentPath] = folderNode;
                    }
                    currentParent = folderNode;
                }

                var assetNode = new BankNode { Name = entry.Filename, Entry = entry, IsFolder = false, IsSelected = _selectedCache.Contains(entry) };
                if (currentParent == null) _rootNodes.Add(assetNode);
                else currentParent.Children.Add(assetNode);
            }
        }
    }

    public class InverseBoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
