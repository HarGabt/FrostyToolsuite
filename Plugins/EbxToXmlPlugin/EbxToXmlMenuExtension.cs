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
using System.Diagnostics;

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
                            Thread workerThread = null;
                            Exception threadException = null;
                            bool completed = false;

                            FileStream globalFileStream = null;
                            EbxXmlWriter globalXmlWriter = null;
                            EbxYamlWriter globalYamlWriter = null;

                            workerThread = new Thread(() =>
                            {
                                try
                                {
                                    DirectoryInfo di = new DirectoryInfo(fullPath);
                                    if (!di.Exists)
                                        Directory.CreateDirectory(di.FullName);

                                    EbxAsset asset = App.AssetManager.GetEbx(entry);
                                    globalFileStream = new FileStream(fullPath + filename, FileMode.Create);

                                    if (exportAsYaml)
                                    {
                                        globalYamlWriter = new EbxYamlWriter(asset, globalFileStream, App.AssetManager, tabSize, false);
                                        globalYamlWriter.WriteObjects();
                                    }
                                    else
                                    {
                                        globalXmlWriter = new EbxXmlWriter(asset, globalFileStream, App.AssetManager, tabSize, false);
                                        globalXmlWriter.WriteObjects();
                                    }

                                    asset = null;
                                    completed = true;
                                }
                                catch (ThreadAbortException)
                                {
                                    // Save partial data before thread dies
                                    try
                                    {
                                        globalFileStream?.Flush();
                                        globalXmlWriter?.Dispose();
                                        globalYamlWriter?.Dispose();
                                        globalFileStream?.Dispose();
                                    }
                                    catch { }
                                    App.Logger.Log("Thread aborted for {0} - partial file saved", entry.Filename);
                                }
                                catch (Exception ex)
                                {
                                    threadException = ex;
                                }
                            }) { IsBackground = true };

                            workerThread.Start();

                            if (!workerThread.Join(500)) // 500ms timeout
                            {
                                // KILL THE FUCKING THREAD TO FREE MEMORY
                                App.Logger.Log("TIMEOUT: Aborting hung thread for {0}", entry.Filename);
                                workerThread.Abort();

                                // Wait longer for abort to complete and thread to fully clean up
                                if (!workerThread.Join(2000))
                                {
                                    App.Logger.Log("WARNING: Thread did not abort cleanly for {0}", entry.Filename);
                                }

                                // Clean up any remaining file handles
                                try
                                {
                                    globalXmlWriter?.Dispose();
                                    globalYamlWriter?.Dispose();
                                    globalFileStream?.Dispose();
                                }
                                catch { }

                                // More aggressive memory cleanup after thread abort
                                GC.Collect(2, GCCollectionMode.Forced, true);
                                GC.WaitForPendingFinalizers();
                                GC.Collect(2, GCCollectionMode.Forced, true);

                                continue;
                            }

                            if (threadException != null)
                            {
                                App.Logger.Log("Error processing {0}: {1}", entry.Filename, threadException.Message);
                            }
                        }
                        catch (Exception)
                        {
                            App.Logger.Log("Failed to export {0}", entry.Filename);
                        }
                        finally
                        {
                            // Minimal cleanup - let .NET handle GC naturally
                            if (idx % 100 == 0)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }
                });

                FrostyMessageBox.Show("Successfully exported EBX to " + outDir, "Frosty Editor");
            }
        });
    }
}
