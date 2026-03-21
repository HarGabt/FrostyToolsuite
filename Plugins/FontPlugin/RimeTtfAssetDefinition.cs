using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers.Entries;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;

namespace FontPlugin
{
    public class RimeTtfAssetDefition : AssetDefinition
    {

        protected static ImageSource iconSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyCore;Component/Images/Assets/BlankFileType.png") as ImageSource;

        public override FrostyAssetEditor GetEditor(ILogger logger)
        {
            return new RimeTtfAssetEditor(logger);
        }

        public override ImageSource GetIcon()
        {
            return iconSource;
        }
    }

    public class RimeTtfAssetEditor : FrostyAssetEditor
    {
        public RimeTtfAssetEditor(ILogger logger) : base(logger)
        {
        }

        public override List<ToolbarItem> RegisterToolbarItems() {
            return new List<ToolbarItem>
            {
                new ToolbarItem("Export", "Export Font", "Images/Export.png", new RelayCommand((object state) => { ExportButton_Click(this, new RoutedEventArgs()); })),
                new ToolbarItem("Import", "Import Font", "Images/Import.png", new RelayCommand((object state) => { ImportButton_Click(this, new RoutedEventArgs()); })),
            };
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic root = asset.RootObject;
            ResAssetEntry fontResEntry = App.AssetManager.GetResEntry(root.FontResource);

            FrostySaveFileDialog saveFileDialog = new FrostySaveFileDialog("Export Font Asset", "TrueType Font (*.ttf)|*.ttf", "Font", AssetEntry.Filename, false);
            bool result = false;
            while (true)
            {
                string initialDir = saveFileDialog.InitialDirectory;
                result = saveFileDialog.ShowDialog();

                if (result)
                {
                    FileInfo fileInfo = new FileInfo(saveFileDialog.FileName);
                    saveFileDialog.InitialDirectory = fileInfo.DirectoryName;

                    if (fileInfo.Exists)
                    {
                        if (FrostyMessageBox.Show(saveFileDialog.FileName + " already exists\r\nDo you want to replace it?", "Frosty Editor (Exporting Font Asset)", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (!result)
            {
                return;
            }

            FrostyTaskWindow.Show("Exporting font", "Exporting font...", (task) =>
            {
                Stream resStream = App.AssetManager.GetRes(fontResEntry);
                if (resStream != null)
                {
                    using (NativeWriter writer = new NativeWriter(new FileStream(saveFileDialog.FileName, FileMode.Create)))
                    {
                        using (NativeReader reader = new NativeReader(resStream))
                            writer.Write(reader.ReadToEnd());
                    }
                }
                else
                {
                    logger.LogError("Failed to export res ${fontResEntry.ResRid}. Maybe it doesn't exist?");
                }
            });
            logger.Log("Exported Font Asset to " + saveFileDialog.FileName);
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            dynamic root = asset.RootObject;
            ResAssetEntry fontResEntry = App.AssetManager.GetResEntry(root.FontResource);

            FrostyOpenFileDialog openFileDialog = new FrostyOpenFileDialog("Import Font Asset", "TrueType Font (*.ttf)|*.ttf", "Font");
            if (openFileDialog.ShowDialog())
            {
                FrostyTaskWindow.Show("Importing Font", "Importing...", (task) =>
                {
                    using (NativeReader reader = new NativeReader(new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read)))
                    {
                        byte[] buffer = reader.ReadToEnd();
                        App.AssetManager.ModifyRes(fontResEntry.ResRid, buffer);
                    }
                });

                AssetEntry.LinkAsset(fontResEntry);

                // Refresh the property grid UI
                FrostyPropertyGrid pg = (GetTemplateChild("PART_AssetPropertyGrid") as FrostyPropertyGrid);
                pg.Object = asset.RootObject;
                InvokeOnAssetModified();

                logger.Log($"Succesfully imported {AssetEntry.Filename}.");
            }
        }
    }
}
