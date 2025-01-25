using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using FrostySdk.Managers.Entries;
using EbxToXmlPlugin.Windows;

namespace EbxToXmlPlugin
{
    public class EbxToXmlMenuExtension : MenuExtension
    {
        internal static ImageSource imageSource = new ImageSourceConverter().ConvertFromString("pack://application:,,,/EbxToXmlPlugin;component/Images/EbxToXml.png") as ImageSource;

        public override string TopLevelMenuName => "Tools";
        public override string SubLevelMenuName => null;

        public override string MenuItemName => "Export EBX to XML";
        public override ImageSource Icon => imageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
        {
            EbxToXmlWindow win = new EbxToXmlWindow();
            if (win.ShowDialog() == false)
                return;
            bool exportAsYaml = win.exportAsYamlCheckBox.IsChecked ?? false;
            bool skipConversationsFolder = win.skipConversationsCheckBox.IsChecked ?? false;
            bool skipVirtualFolder = win.skipVirtualCheckBox.IsChecked ?? false;
            int tabSize = Config.Get<int>("ExportTabSize", 2);

			FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string outDir = fbd.SelectedPath;
                FrostyTaskWindow.Show("Exporting EBX", "", (task) =>
                {
                    uint totalCount = App.AssetManager.GetEbxCount();
                    uint idx = 0;

                    foreach (EbxAssetEntry entry in App.AssetManager.EnumerateEbx())
                    {
                        task.Update(entry.Name, (idx++ / (double)totalCount) * 100.0d);

                        string fullPath = outDir + "/" + entry.Path + "/";

                        string filename = entry.Filename + ".xml";
                        filename = string.Concat(filename.Split(Path.GetInvalidFileNameChars()));

                        if ((skipConversationsFolder && entry.Path.ToLower().StartsWith("conversations"))
                        || (skipVirtualFolder && entry.Path.StartsWith("virtual")))
                            continue;

						if (File.Exists(fullPath + filename))
                            continue;

                        try
                        {
                            DirectoryInfo di = new DirectoryInfo(fullPath);
                            if (!di.Exists)
                                Directory.CreateDirectory(di.FullName);

                            EbxAsset asset = App.AssetManager.GetEbx(entry);
                            if (exportAsYaml)
                            {
                                using (EbxYamlWriter writer = new EbxYamlWriter(asset, new FileStream(fullPath + filename, FileMode.Create), App.AssetManager, tabSize, false))
                                    writer.WriteObjects();
                            }
                            else
                            {
                                using (EbxXmlWriter writer = new EbxXmlWriter(asset, new FileStream(fullPath + filename, FileMode.Create), App.AssetManager, tabSize, false))
                                    writer.WriteObjects();
                            }
                        }
                        catch (Exception)
                        {
                            App.Logger.Log("Failed to export {0}", entry.Filename);
                        }
                    }
                });

                FrostyMessageBox.Show("Successfully exported EBX to " + outDir, "Frosty Editor");
            }
        });
    }
}
