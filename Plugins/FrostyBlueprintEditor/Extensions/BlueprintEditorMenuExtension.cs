using System.Windows.Media;
using BlueprintEditorPlugin.Editors.GraphEditor;
using BlueprintEditorPlugin.Options;
using BlueprintEditorPlugin.Windows;
using Frosty.Core;
using Frosty.Core.Windows;

namespace BlueprintEditorPlugin.Extensions
{
    public class ViewBlueprintContextMenuItem : DataExplorerContextMenuExtension
    {
        public static readonly ImageSource IconImageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/BlueprintEdit.png") as ImageSource;
        
        public override string ContextItemName => "Open as Graph...";
        public override ImageSource Icon => IconImageSource;

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            if (App.SelectedAsset == null) return;
            
            IEbxGraphEditor graphEditor = ExtensionsManager.GetValidGraphEditor(App.SelectedAsset);
            if (graphEditor == null)
            {
                App.Logger.LogError("No valid graph editor exists for this file");
                return;
            }
                
            BlueprintEditor editor;
            if (EditorOptions.LoadBeforeOpen)
            {
                editor = new BlueprintEditor();
                editor.LoadBlueprint(App.SelectedAsset, graphEditor);
            }
            else
            {
                editor = new BlueprintEditor(App.SelectedAsset, graphEditor);
            }
            
            App.EditorWindow.OpenEditor($"{App.SelectedAsset.Filename} (Ebx Graph)", editor);
        });
    }

    public class ViewHashingUtils : MenuExtension
    {
        public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/BlueprintEditorPlugin;component/Images/HashingUtilsIcon.png") as ImageSource;

        public override string TopLevelMenuName => "View";
        public override string MenuItemName => "Hashing Utilities";

        public override RelayCommand MenuItemClicked => new RelayCommand(o =>
        {
            HashingUtilsWindow window = new HashingUtilsWindow();
            window.Show();
        });
    }
}