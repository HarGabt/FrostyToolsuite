using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BlueprintEditorPlugin.Editors.BlueprintEditor;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.PropertyGrid.Items;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Converters;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;

namespace BlueprintEditorPlugin.Editors.PropertyGrid
{ 
    public class BlueprintPropertyGridItem : FrostyPropertyGridItem
    {
        private static readonly IReadOnlyDictionary<string, Type> StaticEditors = new Dictionary<string, Type>
        {
            //{ "String", typeof(FrostyStringEditor) },
            { "CString", typeof(FrostyCStringEditor) },
            { "String", typeof(FrostyStringEditor) },
            { "Boolean", typeof(FrostyBooleanEditor) },
            { "List`1", typeof(BlueprintArrayEditor) },
            { "Byte", typeof(BlueprintNumberEditor) },
            { "SByte", typeof(BlueprintNumberEditor) },
            { "Int16", typeof(BlueprintNumberEditor) },
            { "UInt16", typeof(BlueprintNumberEditor) },
            { "Int32", typeof(BlueprintNumberEditor) },
            { "UInt32", typeof(BlueprintNumberEditor) },
            { "Int64", typeof(BlueprintNumberEditor) },
            { "UInt64", typeof(BlueprintNumberEditor) },
            { "Single", typeof(BlueprintNumberEditor) },
            { "Double", typeof(BlueprintNumberEditor) },
            { "PointerRef", typeof(BlueprintPointerRefEditor) },
            { "ResourceRef", typeof(BlueprintResourceRefEditor) },
            { "Guid", typeof(BlueprintGuidEditor) }
        };

        public override void OnApplyTemplate()
        {
            FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;

            Button btn = GetTemplateChild("PART_ArrayRemoveButton") as Button;
            btn.Click += ArrayRemoveButton_Click;

            if (!item.IsCategory)
            {
                ContentControl value = GetTemplateChild("PART_Value") as ContentControl;
                UIElement elem = null;

                if (item.Value != null)
                {
                    string valueTypeName = item.Value.GetType().Name;
                    Type editorType = null;

                    if (item.TypeEditor != null)
                        editorType = item.TypeEditor;

                    if (editorType == null)
                    {
                        if (item.Name.Equals("__InstanceGuid"))
                        {
                            editorType = typeof(FrostyStructEditor);
                            item.IsReadOnly = true;
                        }
                        else if (StaticEditors.ContainsKey(valueTypeName))
                        {
                            editorType = StaticEditors[valueTypeName];
                        }
                        else if (item.Value is Enum)
                        {
                            editorType = typeof(FrostyEnumEditor);
                        }
                        else if (item.Value is IList)
                        {
                            editorType = typeof(FrostyArrayEditor);
                        }
                        else
                        {
                            editorType = App.PluginManager.GetTypeEditor(valueTypeName);
                            if (editorType == null)
                            {
                                editorType = typeof(FrostyStructEditor);
                            }
                        }
                    }

                    object editor = Activator.CreateInstance(editorType);
                    if (editorType.BaseType.Name == typeof(BlueprintTypeEditor<>).Name)
                    {
                        elem = (UIElement)editorType.GetMethod("CreateEditor").Invoke(editor, new object[] { item, GetPropertyGrid().NodeWrangler });
                    }
                    else
                    {
                        elem = (UIElement)editorType.GetMethod("CreateEditor").Invoke(editor, new object[] { item });
                    }
                }

                value.Content = elem;
            }

            ContextMenu cm = new ContextMenu();
            MenuItem mi = new MenuItem
            {
                Header = "Copy",
                Icon = new Image
                {
                    Source = StringToBitmapSourceConverter.CopySource,
                    Opacity = 0.5
                }
            };
            mi.Click += CopyMenuItem_Click;
            cm.Items.Add(mi);

            mi = new MenuItem
            {
                Header = "Paste",
                Icon = new Image
                {
                    Source = StringToBitmapSourceConverter.PasteSource,
                    Opacity = 0.5
                }
            };
            mi.Click += PasteMenuItem_Click;
            BindingOperations.SetBinding(mi, IsEnabledProperty, new Binding("HasData") { Source = FrostyClipboard.Current });
            cm.Items.Add(mi);

            if (item.IsPointerRef)
            {
                cm.Items.Add(new Separator());

                mi = new MenuItem
                {
                    Header = "Copy Guid",
                    Icon = new Image
                    {
                        Source = StringToBitmapSourceConverter.CopySource,
                        Opacity = 0.5
                    }
                };
                mi.Click += CopyGuidMenuItem_Click;
                cm.Items.Add(mi);
            }

            if (item.IsArrayChild)
            {
                cm.Items.Add(new Separator());

                mi = new MenuItem {Header = "Insert Before"};
                mi.Click += ArrayInsertBeforeMenuItem_Click;
                cm.Items.Add(mi);

                mi = new MenuItem {Header = "Insert After"};
                mi.Click += ArrayInsertAfterMenuItem_Click;
                cm.Items.Add(mi);
            }

            ContextMenu = cm;
        }

        /// <summary>
        /// Copies the PointerRef's guid to the clipboard
        /// </summary>
        private void CopyGuidMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;

            string guidToCopy = "";

            PointerRef pointerRef = (PointerRef)item.Value;
            if (pointerRef.Type == PointerRefType.Null)
                guidToCopy = "";
            else if (pointerRef.Type == PointerRefType.External)
                guidToCopy = pointerRef.External.ClassGuid.ToString();
            else
            {
                dynamic obj = pointerRef.Internal;
                guidToCopy = obj.GetInstanceGuid().ToString();
            }

            Clipboard.SetText(guidToCopy);
        }

        /// <summary>
        /// Copies the items data to the clipboard
        /// </summary>
        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;
            FrostyClipboard.Current.SetData(item.Value);
        }

        /// <summary>
        /// Tries to paste the currently copied clipboard data
        /// </summary>
        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FrostyClipboard.Current.HasData)
            {
                FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;
                if (item.IsEnabled && !item.IsReadOnly)
                {
                    if (FrostyClipboard.Current.IsType(item.Value.GetType()))
                    {
                        BlueprintPropertyGrid pg = GetPropertyGrid();

                        if (pg.Asset != null)
                        {
                            // property grid is displaying an asset
                            item.Value = FrostyClipboard.Current.GetData(pg.Asset, App.AssetManager.GetEbxEntry(pg.Asset.FileGuid));
                        }
                        else
                        {
                            // property grid is displaying a custom EBX class
                            item.Value = FrostyClipboard.Current.GetData();
                        }
                    }
                }
            }
        }

        private BlueprintPropertyGrid GetPropertyGrid()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (!(parent.GetType().IsSubclassOf(typeof(BlueprintPropertyGrid)) || parent is BlueprintPropertyGrid))
                parent = VisualTreeHelper.GetParent(parent);
            return (parent as BlueprintPropertyGrid);
        }

        /// <summary>
        /// Inserts a new array element before the selected item
        /// </summary>
        private void ArrayInsertBeforeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;

            IList list = item.Parent.Value as IList;
            Type listType = list.GetType().GetGenericArguments()[0];

            item.Parent.InsertChild(Activator.CreateInstance(listType), Activator.CreateInstance(listType), item, -1);
        }

        /// <summary>
        /// Inserts a new array element after the selected item
        /// </summary>
        private void ArrayInsertAfterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;

            IList list = item.Parent.Value as IList;
            Type listType = list.GetType().GetGenericArguments()[0];

            item.Parent.InsertChild(Activator.CreateInstance(listType), Activator.CreateInstance(listType), item, 1);
        }

        /// <summary>
        /// Removes the selected array element
        /// </summary>
        private void ArrayRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            FrostyPropertyGridItemData item = (FrostyPropertyGridItemData)DataContext;

            item.Parent.RemoveChild(item);
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new BlueprintPropertyGridItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is BlueprintPropertyGridItem;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Subtract || e.Key == Key.Add || e.Key == Key.Multiply)
            {
                // to stop +/-/* from expanding/collapsing
                e.Handled = true;

                if (e.OriginalSource is TextBox tb)
                {
                    // to ensure that textboxes still respond to +/-/*
                    int lastLocation = tb.SelectionStart;
                    tb.Text = tb.Text.Insert(lastLocation, 
                        (e.Key == Key.Subtract) ? "-" :
                        (e.Key == Key.Add) ? "+" :
                        (e.Key == Key.Multiply) ? "*" : "");
                    tb.SelectionStart = lastLocation + 1;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            // to make sure right click selects item
            base.OnPreviewMouseRightButtonDown(e);
            IsSelected = true;
        }
    }

    
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(BlueprintPropertyGridItem))]
    public class BlueprintPropertyGridTreeView : TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new BlueprintPropertyGridItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is BlueprintPropertyGridItem;
        }
    }
    
    public class BlueprintPropertyGrid : Control
    {
        private const string PART_ClassesComboBox = "PART_ClassesComboBox";
        private const string PART_ClassesRow = "PART_ClassesRow";
        private const string PART_FilterTextBox = "PART_FilterTextBox";
        private const string PART_FilterInProgresBorder = "PART_FilterInProgresBorder";
        private const string PART_FilterProgressBar = "PART_FilterProgressBar";

        #region -- Properties --
        
        public INodeWrangler NodeWrangler { get; set; }

        #region -- Object --
        public static readonly DependencyProperty ObjectProperty = DependencyProperty.Register("Object", typeof(object), typeof(BlueprintPropertyGrid), new FrameworkPropertyMetadata(null, OnObjectChanged));
        public object Object
        {
            get => GetValue(ObjectProperty);
            set => SetValue(ObjectProperty, value);
        }
        private static void OnObjectChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            BlueprintPropertyGrid pg = sender as BlueprintPropertyGrid;
            if (args.NewValue != null)
            {
                pg.SetItems(pg.ProcessClassWithCategories(args.NewValue));
            }
        }
        #endregion

        #region -- Asset --
        public static readonly DependencyProperty AssetProperty = DependencyProperty.Register("Asset", typeof(EbxAsset), typeof(BlueprintPropertyGrid), new FrameworkPropertyMetadata(null, OnAssetChanged));
        public EbxAsset Asset
        {
            get => (EbxAsset)GetValue(AssetProperty);
            set => SetValue(AssetProperty, value);
        }
        private static void OnAssetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            BlueprintPropertyGrid pg = sender as BlueprintPropertyGrid;
            EbxAsset asset = args.NewValue as EbxAsset;
            pg.asset = asset;
        }
        #endregion

        #region -- Classes --
        public static readonly DependencyProperty ClassesProperty = DependencyProperty.Register("Classes", typeof(IEnumerable), typeof(BlueprintPropertyGrid), new FrameworkPropertyMetadata(null, OnClassesChanged));
        public IEnumerable Classes
        {
            get => (IEnumerable)GetValue(ClassesProperty);
            set => SetValue(ClassesProperty, value);
        }
        private static void OnClassesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            BlueprintPropertyGrid pg = sender as BlueprintPropertyGrid;
            IEnumerable classes = args.NewValue as IEnumerable;
            pg.classes = classes;
            pg.SetClass(classes.Cast<object>().First());
        }
        #endregion

        #region -- HeaderVisible --
        public static readonly DependencyProperty HeaderVisibleProperty = DependencyProperty.Register("HeaderVisible", typeof(bool), typeof(BlueprintPropertyGrid), new FrameworkPropertyMetadata(false));
        public bool HeaderVisible
        {
            get => (bool)GetValue(HeaderVisibleProperty);
            set => SetValue(HeaderVisibleProperty, value);
        }
        #endregion

        #region -- ClassViewVisible --
        public static readonly DependencyProperty ClassViewVisibleProperty = DependencyProperty.Register("ClassViewVisible", typeof(bool), typeof(BlueprintPropertyGrid), new UIPropertyMetadata(true));
        public bool ClassViewVisible
        {
            get => (bool)GetValue(ClassViewVisibleProperty);
            set => SetValue(ClassViewVisibleProperty, value);
        }
        #endregion

        #region -- Modified --
        public static readonly DependencyProperty ModifiedProperty = DependencyProperty.Register("Modified", typeof(bool), typeof(BlueprintPropertyGrid), new UIPropertyMetadata(false));
        public bool Modified
        {
            get => (bool)GetValue(ModifiedProperty);
            set => SetValue(ModifiedProperty, value);
        }
        #endregion

        #region -- InitialWidth --
        public static readonly DependencyProperty InitialWidthProperty = DependencyProperty.Register("InitialWidth", typeof(GridLength), typeof(BlueprintPropertyGrid), new UIPropertyMetadata(new GridLength(1.0, GridUnitType.Star)));
        public GridLength InitialWidth
        {
            get => (GridLength)GetValue(InitialWidthProperty);
            set => SetValue(InitialWidthProperty, value);
        }
        #endregion

        #region -- FilterText --
        public static readonly DependencyProperty FilterTextProperty = DependencyProperty.Register("FilterText", typeof(string), typeof(BlueprintPropertyGrid), new UIPropertyMetadata(""));
        public string FilterText
        {
            get => (string)GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }
        #endregion

        #endregion

        public object SelectedClass => Object;

        public event EventHandler<ItemModifiedEventArgs> OnModified;
        public event EventHandler<ItemPreModifiedEventArgs> OnPreModified;

        protected BaseTypeOverride additionalData;

        protected TreeView tv;
        private FrostyWatermarkTextBox filterBox;
        private Border filterInProgressBorder;
        private ProgressBar filterProgressBar;
        protected ObservableCollection<FrostyPropertyGridItemData> items;
        protected FrostyPropertyGridItemData rootChild;
        protected EbxAsset asset;
        protected IEnumerable classes;

        public static readonly DependencyProperty OnPreModifiedCommandProperty = DependencyProperty.Register("OnPreModifiedCommand", typeof(ICommand), typeof(BlueprintPropertyGrid), new UIPropertyMetadata(null));
        public ICommand OnPreModifiedCommand
        {
            get
            {
                return (ICommand)GetValue(OnPreModifiedCommandProperty);
            }
            set
            {
                SetValue(OnPreModifiedCommandProperty, value);
            }
        }

        public static readonly DependencyProperty OnModifiedCommandProperty = DependencyProperty.Register("OnModifiedCommand", typeof(ICommand), typeof(BlueprintPropertyGrid), new UIPropertyMetadata(null));
        public ICommand OnModifiedCommand
        {
            get
            {
                return (ICommand)GetValue(OnModifiedCommandProperty);
            }
            set
            {
                SetValue(OnModifiedCommandProperty, value);
            }
        }

        static BlueprintPropertyGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BlueprintPropertyGrid), new FrameworkPropertyMetadata(typeof(BlueprintPropertyGrid)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            tv = GetTemplateChild("tv") as TreeView;
            filterBox = GetTemplateChild(PART_FilterTextBox) as FrostyWatermarkTextBox;
            filterInProgressBorder = GetTemplateChild(PART_FilterInProgresBorder) as Border;
            filterProgressBar = GetTemplateChild(PART_FilterProgressBar) as ProgressBar;

            tv.ItemsSource = items;
            filterBox.KeyUp += FilterBox_KeyUp;
            filterBox.LostFocus += FilterBox_LostFocus;
            //filterBox.TextChanged += FilterBox_TextChanged;

            if (asset != null)
                SetClass(asset.RootObject);
            else if (classes != null)
                SetClass(classes.Cast<object>().First());
        }

        private async void FilterBox_LostFocus(object sender, RoutedEventArgs e)
        {            
            string filterText = filterBox.Text;
            if (filterText == FilterText)
                return;
            
            if (Object == null)
                return;

            filterBox.IsEnabled = false;
            tv.IsEnabled = false;
            filterProgressBar.Visibility = Visibility.Visible;
            filterInProgressBorder.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                List<object> refObjects = new List<object>();

                if (filterText.StartsWith("guid:"))
                {
                    string[] arr = filterText.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    string guidValue = (arr.Length > 1) ? arr[1] : "0";

                    foreach (var item in items)
                    {
                        if (item.FilterGuid(guidValue.ToLower(), refObjects))
                            item.IsHidden = true;
                    }
                }
                else
                {
                    foreach (var item in items)
                    {
                        if (item.FilterPropertyName(filterText, refObjects))
                            item.IsHidden = true;
                    }
                }
            });

            FilterText = filterText;
            filterBox.IsEnabled = true;
            tv.IsEnabled = true;
            filterProgressBar.Visibility = Visibility.Collapsed;
            filterInProgressBorder.Visibility = Visibility.Collapsed;

            GC.Collect();
        }

        private void FilterBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                filterBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        public void SetClass(object obj)
        {
            Object = obj;
        }

        protected void SetItems(FrostyPropertyGridItemData[] inItems)
        {
            if (items == null)
                items = new ObservableCollection<FrostyPropertyGridItemData>();
            items.Clear();
            foreach (FrostyPropertyGridItemData ti in inItems)
                items.Add(ti);

            if (tv != null)
                tv.ItemsSource = items;
        }

        protected virtual FrostyPropertyGridItemData[] ProcessClassWithCategories(object value)
        {
            if (value == null)
                return FrostyPropertyGridItemData.EmptyList;

            object defValue = Activator.CreateInstance(value.GetType());
            SortedDictionary<string, FrostyPropertyGridItemData> categories = new SortedDictionary<string, FrostyPropertyGridItemData>();

            rootChild = new FrostyPropertyGridItemData("");
            rootChild.Modified += SubItem_Modified;
            rootChild.PreModified += SubItem_PreModified;

            PropertyInfo[] pis = value.GetType().GetProperties();

            Type overrideType = App.PluginManager.GetTypeOverride(value.GetType().Name);
            object typeOverrideDefaultValue = null;

            if (overrideType != null)
            {
                additionalData = (BaseTypeOverride)Activator.CreateInstance(overrideType);
                additionalData.Original = value;
                additionalData.Load();

                typeOverrideDefaultValue = Activator.CreateInstance(overrideType);
                PropertyInfo[] newProperties = overrideType.GetProperties();

                pis = pis.Union(from p in newProperties
                                where pis.FirstOrDefault((PropertyInfo op) => op.Name == p.Name) == null
                                select p).ToArray<PropertyInfo>();
            }
            else
            {
                additionalData = null;
            }

            Array.Sort(pis, new PropertyComparer());

            foreach (PropertyInfo pi in pis)
            {
                AttributeList attributes = new AttributeList();
                attributes.AddRange(pi.GetCustomAttributes());

                if (overrideType != null)
                {
                    var overridePi = overrideType.GetProperty(pi.Name);
                    if (overridePi != null)
                    {
                        attributes.AddRangeAndReplace(overridePi.GetCustomAttributes());
                    }
                }

                if (attributes.GetCustomAttribute<IsHiddenAttribute>() != null)
                    continue;

                string category = "Misc";
                if (attributes.GetCustomAttribute<FrostySdk.Attributes.CategoryAttribute>() != null)
                    category = attributes.GetCustomAttribute<FrostySdk.Attributes.CategoryAttribute>().Name;

                if (!categories.ContainsKey(category))
                    categories.Add(category, new FrostyPropertyGridItemData(category));

                string name = pi.Name;
                if (attributes.GetCustomAttribute<FrostySdk.Attributes.DisplayNameAttribute>() != null)
                    name = attributes.GetCustomAttribute<FrostySdk.Attributes.DisplayNameAttribute>().Name;

                FrostyPropertyGridItemFlags flags = FrostyPropertyGridItemFlags.None;
                if (attributes.GetCustomAttribute<IsReferenceAttribute>() != null)
                    flags |= FrostyPropertyGridItemFlags.IsReference;

                object actualObject = value;
                object actualDefaultValue = defValue;
                if (overrideType != null && overrideType.GetProperties().Contains(pi))
                {
                    actualObject = additionalData;
                    actualDefaultValue = typeOverrideDefaultValue;
                }

                FrostyPropertyGridItemData subItem = new FrostyPropertyGridItemData(name, pi.Name, pi.GetValue(actualObject), pi.GetValue(actualDefaultValue), rootChild, flags) {Binding = new PropertyValueBinding(pi, actualObject)};

                if (attributes.GetCustomAttribute<FrostySdk.Attributes.IsReadOnlyAttribute>() != null)
                    subItem.IsReadOnly = true;
                if (attributes.GetCustomAttribute<FrostySdk.Attributes.DescriptionAttribute>() != null)
                    subItem.Description = attributes.GetCustomAttribute<FrostySdk.Attributes.DescriptionAttribute>().Description;
                if (attributes.GetCustomAttribute<DependsOnAttribute>() != null)
                    subItem.DependsOn = attributes.GetCustomAttribute<DependsOnAttribute>().Name;
                if (attributes.GetCustomAttribute<FrostySdk.Attributes.EditorAttribute>() != null)
                    subItem.TypeEditor = attributes.GetCustomAttribute<FrostySdk.Attributes.EditorAttribute>().EditorType;
                if (attributes.GetCustomAttribute<IsExpandedByDefaultAttribute>() != null)
                    subItem.IsExpanded = true;
                if (attributes.GetCustomAttribute<FixedSizeArrayAttribute>() != null)
                    subItem.IsEnabled = false;

                subItem.MetaData.AddRange(attributes.GetCustomAttributes<EditorMetaDataAttribute>());
                subItem.Attributes.AddRange(attributes.GetCustomAttributes<Attribute>());

                categories[category].Children.Add(subItem);
                rootChild.Children.Add(subItem);
            }

            return categories.Values.ToArray();
        }

        protected void SubItem_PreModified(object sender, ItemPreModifiedEventArgs e)
        {
            OnPreModifiedCommand?.Execute(e);
            OnPreModified?.Invoke(sender, e);
        }

        protected virtual void SubItem_Modified(object sender, ItemModifiedEventArgs e)
        {
            if (additionalData != null)
            {
                additionalData.Save(e);
            }
            else
            {
                PropertyValueBinding binding = e.Item.Binding as PropertyValueBinding;

                var parent = e.Item;
                while (binding == null)
                {
                    binding = parent.Binding as PropertyValueBinding;
                    parent = parent.Parent;
                }
            }

            if (NodeWrangler is IEbxNodeWrangler ebxNodeWrangler)
            {
                // @hack: To ensure that changes to only the transient id field are not exported to mods, but
                //        are saved to projects. This allows users to modify ids to their hearts contents without
                //        bloating their mods with unintentional edits.

                ebxNodeWrangler.Asset.TransientEdit = e.Item.GetCustomAttribute<IsTransientAttribute>() != null && e.Item.Name.Equals("__Id");
            }

            Modified = true;

            OnModifiedCommand?.Execute(e);
            OnModified?.Invoke(sender, e);
        }
    }

}