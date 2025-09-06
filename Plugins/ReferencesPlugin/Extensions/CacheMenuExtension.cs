using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ReferencesPlugin.Extensions
{
    public class CreateCacheMenuExtension : MenuExtension
    {
        public override string TopLevelMenuName => "Tools";

        public override string SubLevelMenuName => "Generate Cache";

        public override string MenuItemName => "Reference Plugin Cache";

        public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Database.png") as ImageSource;

        public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
        {

            FrostyTaskWindow.Show("Creating Cache", "", (task) =>
            {
                CreateCache(task);
            });
        });
        public static string CacheDirectory = System.AppDomain.CurrentDomain.BaseDirectory + @"Plugins\Caches\";
        public static string CacheFileDir = CacheDirectory + Enum.GetName(typeof(ProfileVersion), ProfilesLibrary.DataVersion) + "_ReferencePlugin_Cache.cache";
        private void CreateCache(FrostyTaskWindow task)
        {
            Dictionary<Guid, Guid> TextureGuidsToLog = new Dictionary<Guid, Guid>();
            AssetManager AM = App.AssetManager;
            int textureassetcount = AM.EnumerateEbx(type: "TextureAsset").ToList().Count;
            int idx = 0;
            TextureGuidsToLog.Clear();
            Dictionary<UInt32, List<Tuple<string, int>>> metadataList = new Dictionary<UInt32, List<Tuple<string, int>>>();
            foreach (EbxAssetEntry refEntry in AM.EnumerateEbx(type: "UIMetaDataAsset"))
            {
                if (!refEntry.IsAdded)
                {
                    EbxAsset refAsset = AM.GetEbx(refEntry, true);
                    dynamic refRoot = refAsset.RootObject;
                    int idx2 = 0;
                    foreach (dynamic item in refRoot.Items)
                    {
                        UInt32 TypeFNV = FNVConvertMetadata(item.Internal.GetType().Name);
                        foreach (UInt32 Identifier in item.Internal.Identifiers)
                        {
                            UInt32 newIden = TypeFNV ^ Identifier;
                            if (metadataList.ContainsKey(newIden))
                                metadataList[newIden].Add(new Tuple<string, int>(refEntry.Name, idx2));
                            else
                                metadataList.Add(newIden, new List<Tuple<string, int>>() { new Tuple<string, int>(refEntry.Name, idx2) });
                        }
                        idx2++;
                    }
                }
            }
            foreach (EbxAssetEntry refEntry in AM.EnumerateEbx(type: "TextureAsset"))
            {
                task.Update("Logging Texture Assets", ((idx++ / (double)textureassetcount) * 100.0d));
                if (!refEntry.IsAdded)
                {
                    EbxAsset refAsset = AM.GetEbx(refEntry, true);
                    TextureGuidsToLog.Add(refAsset.FileGuid, refAsset.RootInstanceGuid);
                }
            }
            if (!Directory.Exists(CacheDirectory))
                Directory.CreateDirectory(CacheDirectory);
            using (NativeWriter writer = new NativeWriter(new FileStream(CacheFileDir, FileMode.Create)))
            {
                writer.Write(TextureGuidsToLog.Keys.Count);
                writer.Write(metadataList.Keys.Count);
                foreach (Guid TexFileGuid in TextureGuidsToLog.Keys)
                {
                    writer.Write(TexFileGuid);
                    writer.Write(TextureGuidsToLog[TexFileGuid]);
                }
                foreach (UInt32 key in metadataList.Keys)
                {
                    writer.Write(key);
                    writer.Write(metadataList[key].Count);
                    foreach (Tuple<string, int> tup in metadataList[key])
                    {
                        writer.WriteNullTerminatedString(tup.Item1);
                        writer.Write(tup.Item2);
                    }
                }
            }
        }
        public static UInt32 FNVConvertMetadata(string FNVInput)
        {
            int FNV_offset_basis = 5381;
            int FNV_prime = 33;
            for (int i = 0; i < FNVInput.Length; i++)
            {
                byte b = (byte)FNVInput[i];
                FNV_offset_basis = (FNV_offset_basis * FNV_prime) ^ b;
            }
            String Output = "";
            Int64 FNV_offset_basis_Temp = Convert.ToInt64(FNV_offset_basis);
            if (FNV_offset_basis < 0)
            {
                FNV_offset_basis = FNV_offset_basis * -1;
                string Original = Convert.ToString(FNV_offset_basis, 2).PadLeft(32, '0');
                string Zero = "0";
                for (int i = 0; i < Original.Length; i++)
                {
                    if (Original[i] == Zero[0])
                    {
                        Output = Output + "1";
                    }
                    else
                    {
                        Output = Output + "0";
                    }
                }
                FNV_offset_basis_Temp = Convert.ToInt64(Output, 2) + 2;
            }
            else
            {
                FNV_offset_basis_Temp = FNV_offset_basis_Temp + 1;
            }
            return Convert.ToUInt32(FNV_offset_basis_Temp);
        }
    }
}
