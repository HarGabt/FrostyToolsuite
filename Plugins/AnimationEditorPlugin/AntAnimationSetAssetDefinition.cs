using AssetBankPlugin.Ant;
using AssetBankPlugin.Export;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetBankPlugin.GenericData;

namespace AssetBankPlugin
{
    public class AntAnimationSetAssetDefinition : AssetDefinition
    {
        public override void GetSupportedExportTypes(List<AssetExportType> exportTypes)
        {
            exportTypes.Add(new AssetExportType("gltf", "GL Transfer format"));
            exportTypes.Add(new AssetExportType("xml", "XML Animation Keyframe Dump"));
            exportTypes.Add(new AssetExportType("smd", "Source StudioMdl Data"));

            base.GetSupportedExportTypes(exportTypes);
        }

        public override bool Export(EbxAssetEntry entry, string path, string filterType)
        {
            var opt = new AnimationOptions();
                opt.Load();
            // Get the Ebx.
            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic antSetAsset = (dynamic)asset.RootObject;
            // Get the Chunk.
            Stream s;
            int bundleId = 0;
            if (antSetAsset.AssetBankResource != (ulong)0)
            {
                ResAssetEntry res = App.AssetManager.GetResEntry(antSetAsset.AssetBankResource);
                s = App.AssetManager.GetRes(res);
            }
            else
            {
                App.Logger.Log("Asset does not reference a bank");
                return false;
            }
            IEnumerable<BundleEntry> bundles;
            Directory.CreateDirectory("Caches");
            string cachePath = $"Caches/{ProfilesLibrary.ProfileName}_antstate.cache";
            string internalCachePath = $"Caches/{ProfilesLibrary.ProfileName}_antref.cache";

            // If we're not using the cache or it doesn't exist yet, then get every bundle.
            if (opt.UseCache)
            {
                if (File.Exists(cachePath) && File.Exists(internalCachePath))
                {
                    Cache.ReadState(cachePath);
                    Cache.ReadMap(internalCachePath);
                }
                else
                {
                    // First time setup, read all bundles and store them in the antstatecache.
                    bundles = App.AssetManager.EnumerateBundles();
                    foreach (var bundle in bundles)
                    {
                        LoadAntStateFromBundle(bundle);
                    }
                    Cache.WriteState(cachePath);
                    Cache.WriteMap(internalCachePath);
                }
            }
            else
            {
                bundles = App.AssetManager.EnumerateBundles();
                foreach (var bundle in bundles)
                {
                    LoadAntStateFromBundle(bundle);
                }
            }


            using (var r = new NativeReader(s))
            {
                var bank = new Bank(r, 0);

                

                EbxAssetEntry skelEntry = App.AssetManager.GetEbxEntry(antSetAsset.SkeletonAsset.External.FileGuid);
                var skelEbx = App.AssetManager.GetEbx(skelEntry);
                dynamic skel = (dynamic)skelEbx.RootObject;

                var skeleton = SkeletonAssetExport.ConvertToInternal(skel);
                foreach (var dataName in bank.DataNames)
                {
                    var dat = AntRefTable.Get(dataName.Value);
                    if (dat is AnimationAsset anim)
                    {
                        anim.Name = dataName.Key;
                        anim.Channels = anim.GetChannels(anim.ChannelToDofAsset);
                        var intern = anim.ConvertToInternal();
                        new AnimationExporterSEANIM().Export(intern, skeleton, Path.GetDirectoryName(path));
                    }
                }
            }

            FrostyMessageBox.Show($"Exported {entry.Name} for {ProfilesLibrary.ProfileName}", "Export Complete");

            return true;
        }
        public static void LoadAntStateFromBundle(BundleEntry bundle)
        {
            // Filter by known AssetBank resource type IDs (0x51A3C853 = AssetBank, 0xEC1B7BF4 = AnimationSet variant)
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
    }
}
