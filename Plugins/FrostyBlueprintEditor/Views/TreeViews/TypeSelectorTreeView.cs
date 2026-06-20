using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Frosty.Core.Controls;
using Frosty.Core.Converters;
using FrostySdk.Attributes;

namespace BlueprintEditorPlugin.Views.TreeViews
{
    public class TypeSelectorItem : BaseTreeExplorerItem
    {
        public override string Name => Type.Name;
        public Type Type { get; set; }

        public override ImageSource Icon 
        {
            get 
            {
                StringToBitmapSourceConverter converter = new StringToBitmapSourceConverter();
                DisplayNameAttribute attr = Type.GetCustomAttribute<DisplayNameAttribute>();
                string name = (attr != null) ? attr.Name : Type.Name;
                return converter.Convert(name, Type, null, CultureInfo.CurrentCulture) as ImageSource;
            }
        }
    }
    
    public class TypeSelectorTreeView : BaseTreeViewSelector
    {
        public static readonly DependencyProperty TypesProperty = DependencyProperty.Register("Types", typeof(IList<Type>), typeof(BaseTreeViewSelector), new UIPropertyMetadata(null));
        public IList<Type> Types
        {
            get => (IList<Type>)GetValue(TypesProperty);
            set => SetValue(TypesProperty, value);
        }
        
        public Type SelectedType
        {
            get
            {
                if (_treeExplorer.SelectedItem is TypeSelectorItem item)
                    return item.Type;
                return null;
            }
        }

        private List<TreeExplorerFolderItem> _modules = new();

        protected override void UpdateTreeView()
        {
            _modules.Clear();

            foreach (Type type in Types)
            {
                if (type.GetCustomAttribute<IsAbstractAttribute>() != null)
                    continue;

                EbxClassMetaAttribute attr = type.GetCustomAttribute<EbxClassMetaAttribute>();
                string moduleName = (attr != null) ? attr.Namespace : "Reflection";

                int index = _modules.FindIndex(item => { return item.Name.Equals(moduleName); });
                if (index == -1)
                {
                    index = _modules.Count;
                    _modules.Add(new TreeExplorerFolderItem() { Name = moduleName });
                }

                TreeExplorerFolderItem module = _modules[index];
                module.SubItems.Add(new TypeSelectorItem() { Type = type });
            }

            _treeExplorer.ItemsSource = _modules;
            _treeExplorer.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
        }
    }
}