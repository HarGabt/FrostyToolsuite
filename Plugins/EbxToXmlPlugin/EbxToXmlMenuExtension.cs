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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            ExportMode exportMode = win.SelectedExportMode;
            HashSet<string> selectedFolders = win.GetSelectedFolders();
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

                        // Check if this entry should be exported based on folder selection
                        string[] pathParts = entry.Path.Split('/');
                        string topLevelFolder = pathParts.Length > 0 ? pathParts[0].ToLower() : "";

                        bool shouldExport = false;
                        switch (exportMode)
                        {
                            case ExportMode.All:
                                shouldExport = true;
                                break;
                            case ExportMode.SelectedOnly:
                                shouldExport = selectedFolders.Contains(topLevelFolder);
                                break;
                            case ExportMode.ExcludeSelected:
                                shouldExport = !selectedFolders.Contains(topLevelFolder);
                                break;
                        }

                        if (!shouldExport)
                            continue;

                        string fullPath = outDir + "/" + entry.Path + "/";
                        string filename = entry.Filename + ".xml";
                        filename = string.Concat(filename.Split(Path.GetInvalidFileNameChars()));

						if (File.Exists(fullPath + filename))
                            continue;

                        try
                        {
                            // Create a cancellation token with 500ms timeout
                            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)))
                            {
                                FileStream fileStream = null;
                                var processTask = Task.Run(() =>
                                {
                                    try
                                    {
                                        DirectoryInfo di = new DirectoryInfo(fullPath);
                                        if (!di.Exists)
                                            Directory.CreateDirectory(di.FullName);

                                        EbxAsset asset = App.AssetManager.GetEbx(entry);
                                        fileStream = new FileStream(fullPath + filename, FileMode.Create);

                                        if (exportAsYaml)
                                        {
                                            using (EbxYamlWriter writer = new EbxYamlWriter(asset, fileStream, App.AssetManager, tabSize, false))
                                                writer.WriteObjects();
                                        }
                                        else
                                        {
                                            using (EbxXmlWriter writer = new EbxXmlWriter(asset, fileStream, App.AssetManager, tabSize, false))
                                                writer.WriteObjects();
                                        }
                                    }
                                    finally
                                    {
                                        fileStream?.Close();
                                        fileStream?.Dispose();
                                    }
                                }, cts.Token);

                                try
                                {
                                    processTask.Wait(cts.Token);
                                }
                                catch (OperationCanceledException)
                                {
                                    // Flush and close the file stream to preserve partial data
                                    try
                                    {
                                        fileStream?.Flush();
                                        fileStream?.Close();
                                        fileStream?.Dispose();

                                        // Add a note to the partial file indicating it was incomplete
                                        if (File.Exists(fullPath + filename))
                                        {
                                            string partialContent = File.ReadAllText(fullPath + filename);
                                            if (exportAsYaml)
                                            {
                                                File.WriteAllText(fullPath + filename, partialContent + "\n# File incomplete - processing timed out");
                                            }
                                            else
                                            {
                                                File.WriteAllText(fullPath + filename, partialContent + "\n<!-- File incomplete - processing timed out -->");
                                            }
                                        }
                                    }
                                    catch { /* Ignore errors during cleanup */ }

                                    App.Logger.Log("Partial export saved for {0} - processing timeout (>500ms)", entry.Filename);
                                    continue; // Skip to next file
                                }
                                finally
                                {
                                    // Clean up file stream immediately
                                    fileStream?.Close();
                                    fileStream?.Dispose();
                                    fileStream = null;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            App.Logger.Log("Failed to export {0}", entry.Filename);
                        }
                        finally
                        {
                            // Force cleanup every 50 files
                            if (idx % 50 == 0)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                            }
                        }
                    }
                });

                FrostyMessageBox.Show("Successfully exported EBX to " + outDir, "Frosty Editor");
            }
        });
    }
}
