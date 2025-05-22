using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Ebx;
using FrostySdk.IO;
using GuidPlugin.Windows;
using System;
using System.Windows;
using System.Windows.Media;

namespace GuidPlugin
{
    public class CopyGuidContextMenuItem : DataExplorerContextMenuExtension
    {

        public override string ContextItemName => "Copy GUID";

        public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;component/Images/Copy.png") as ImageSource;

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            EbxAsset asset = App.AssetManager.GetEbx(App.SelectedAsset);
            Clipboard.SetText(asset.RootInstanceGuid.ToString());
        });
    }

    public class LogGuidContextMenuItem : DataExplorerContextMenuExtension
    {

        public override string ContextItemName => "Log GUIDs";

        public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Properties.png") as ImageSource;

        public override RelayCommand ContextItemClicked => new RelayCommand((o) =>
        {
            EbxAsset asset = App.AssetManager.GetEbx(App.SelectedAsset);

            App.Logger.Log($"File GUID: {asset.FileGuid}");
            App.Logger.Log($"Root GUID: {asset.RootInstanceGuid}");
        });
    }

    public class NewPointerRefMenuExtension : MenuExtension
    {
        public override string TopLevelMenuName => "Tools";

        public override string MenuItemName => "New PointerRef";

        public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Add.png") as ImageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) => 
        {
            NewPointerRefWindow win = new NewPointerRefWindow();
            if (win.ShowDialog() == true)
            {
                EbxImportReference newRef = new EbxImportReference {
                    FileGuid = win.SelectedFileGUID,
                    ClassGuid = win.SelectedRootGUID
                };
                FrostyClipboard.Current.SetData(new PointerRef(newRef));
                App.Logger.Log("Copied Reference to Clipboard");
            }
        });
    }

}
