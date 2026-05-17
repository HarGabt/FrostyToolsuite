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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace AssetBankPlugin
{
    public class AntStateAssetDefinition : AssetDefinition
    {
        // Tracker to prevent loading the same level context twice.
        private static HashSet<string> _loadedContexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _loadLock = new object();

        // This loads all AssetBank bundles that share the same folder prefix as the current asset.
        // It ensures that cross‑referenced AnimationAssets (e.g. boss <-> level) are in AntRefTable.
        public static void LoadContextualBanks(EbxAssetEntry entry)
        {
            if (entry == null || entry.Bundles.Count == 0) return;

            string bundleName = App.AssetManager.GetBundleEntry(entry.Bundles[0]).Name;
            string prefix = bundleName.Contains("/")
                ? bundleName.Substring(0, bundleName.LastIndexOf('/') + 1)
                : bundleName;

            lock (_loadLock)
            {
                if (_loadedContexts.Contains(prefix)) return;
                var matchingBundles = App.AssetManager.EnumerateBundles()
                    .Where(b => b.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                foreach (var bundle in matchingBundles)
                {
                    LoadAntStateFromBundle(bundle);
                }
                _loadedContexts.Add(prefix);
            }
        }

        public override void GetSupportedExportTypes(List<AssetExportType> exportTypes)
        {
            // Adding SEAnim at the top makes it the default selection in the save dialog.
            exportTypes.Add(new AssetExportType("seanim", "SEAnim Animation File"));
            exportTypes.Add(new AssetExportType("gltf", "GL Transfer format"));
            exportTypes.Add(new AssetExportType("xml", "XML Animation Keyframe Dump"));
            exportTypes.Add(new AssetExportType("smd", "Source StudioMdl Data"));
            base.GetSupportedExportTypes(exportTypes);
        }

        public override FrostyAssetEditor GetEditor(ILogger logger) => new AntStateAssetEditor(logger);

        public override bool Export(EbxAssetEntry entry, string path, string filterType)
        {
            var opt = new AnimationOptions();
            opt.Load();

            if (string.IsNullOrEmpty(opt.ExportSkeletonAsset))
            {
                FrostyMessageBox.Show("Please set an Export Skeleton in Options first.", "Missing Skeleton");
                return false;
            }

            string exportDirectory = Path.GetDirectoryName(path);
            int exportedCount = 0;
            int totalAnimations = 0;

            // Pre‑load the bank and count animations
            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic antStateAsset = asset.RootObject;
            Stream bankStream;
            int bundleId = 0;

            // Dead Space (2023) AntStateAsset uses AssetBankResource (a res entry ulong),
            // not StreamingGuid. Use the same pattern as AntAnimationSetAssetDefinition.
            if (antStateAsset.AssetBankResource != (ulong)0)
            {
                ResAssetEntry res = App.AssetManager.GetResEntry(antStateAsset.AssetBankResource);
                if (res == null)
                {
                    App.Logger.LogError("[Export] Could not find ResAssetEntry for AssetBankResource.");
                    return false;
                }
                bundleId = res.Bundles[0];
                bankStream = App.AssetManager.GetRes(res);
            }
            else
            {
                App.Logger.LogError("[Export] AntStateAsset has no AssetBankResource.");
                return false;
            }

            Bank bank;
            using (var reader = new NativeReader(bankStream))
            {
                bank = new Bank(reader, bundleId);
            }
            totalAnimations = bank.DataNames.Count;

            FrostyTaskWindow.Show("Exporting Animations", "", (task) =>
            {
                try
                {
                    InternalSkeleton skeleton = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadContextualBanks(entry);
                        var skelEntry = App.AssetManager.GetEbxEntry(opt.ExportSkeletonAsset);
                        skeleton = SkeletonAssetExport.ConvertToInternal(App.AssetManager.GetEbx(skelEntry).RootObject);
                    });

                    int count = 0;
                    foreach (var dataName in bank.DataNames)
                    {
                        double progress = ((double)count / totalAnimations) * 100.0;
                        task.Update(dataName.Key, progress);

                        Dictionary<string, BoneChannelType> channels = null;
                        AnimationAsset anim = null;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var dat = AntRefTable.Get(dataName.Value);
                            if (dat is AnimationAsset foundAnim)
                            {
                                anim = foundAnim;
                                anim.Name = dataName.Key;
                                channels = anim.GetChannels(anim.ChannelToDofAsset);
                            }
                        });

                        /// Skip assets with broken references (caused by missing context)
                        if (anim != null && channels != null && channels.Count > 0)
                        {
                            anim.Channels = channels;
                            var intern = anim.ConvertToInternal();

                            if (intern != null)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    new AnimationExporterSEANIM().Export(intern, skeleton, exportDirectory);
                                });
                                exportedCount++;
                            }
                        }
                        count++;
                    }

                    App.Logger.Log($"[Export] Successfully exported {exportedCount} animations from {entry.Name}");
                }
                catch (Exception ex)
                {
                    App.Logger.LogError($"[Export] Failed: {ex.Message}");
                }
            });

            // finish message
            string finishMessage = $"Successfully exported {exportedCount} animations\nfrom '{entry.Filename}'\n\nDestination:\n{exportDirectory}";
            FrostyMessageBox.Show(finishMessage, "Export Complete");

            return true;
        }

        public static void LoadAntStateFromBundle(BundleEntry bundle)
        {
            /// AssetBank resource types (may vary slightly between games)
            var resources = App.AssetManager.EnumerateRes(bundle)
                .Where(r => r.ResType == 0x51A3C853 || r.ResType == 0xEC1B7BF4);

            foreach (var res in resources)
            {
                using (var antBank = App.AssetManager.GetRes(res))
                using (var antReader = new NativeReader(antBank))
                {
                    _ = new Bank(antReader, App.AssetManager.GetBundleId(bundle));
                }
            }
        }

        // Format: pack://application:,,,/AssemblyName;component/Folder/File.png
        protected static ImageSource antIcon = new ImageSourceConverter().ConvertFromString("pack://application:,,,/AssetBankPlugin;component/Images/AntStateAssetFileType.png") as ImageSource;

        // This tells Frosty which icon to show in the Data Explorer
        public override ImageSource GetIcon()
        {
            return antIcon;
        }
    }
}
