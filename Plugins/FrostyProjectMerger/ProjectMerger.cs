using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Resources;
using FrostySdk.Managers.Entries;
using Frosty.Hash;
using System.Reflection;
using FrostyEditor.Windows;

namespace ProjectMerger
{
    public class ProjectMergerToolbarExtension : ToolbarExtension
    {
        public override string Name => "Import Project";
        public override string Tooltip => "Import from Project";
        public override string Icon => "FrostyEditor;component/Images/Import.png";

        public override RelayCommand ToolbarItemClicked => new RelayCommand((o) =>
        {
            FrostyOpenFileDialog openFileDialog = new FrostyOpenFileDialog("Import Project", "*.fbproject (Frosty Project)|*.fbproject", "FrostyProject");
            if (!openFileDialog.ShowDialog()) return;

            FrostyTaskWindow.Show("Importing project...", "", task =>
            {
                using (NativeReader reader = new NativeReader(new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read)))
                {
                    ulong magic = reader.ReadULong();
                    if (magic != 0x00005954534F5246)
                    {
                        MessageBoxResult result = FrostyMessageBox.Show(
                            "This project file appears to be a legacy project, and importing it could corrupt the current project file. Are you sure you wish to continue?",
                            "Project Merger", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No) return;
                    }

#if !FROSTY_DEVELOPER
                    try
                    {
#endif
                        #region Header (skip)

                        uint version = reader.ReadUInt();
                        if (version < 9) return;

                        reader.ReadNullTerminatedString(); // profile name
                        reader.ReadLong();                 // creation date
                        reader.ReadLong();                 // modified date
                        reader.ReadUInt();                 // game version
                        reader.ReadNullTerminatedString(); // title
                        reader.ReadNullTerminatedString(); // author
                        reader.ReadNullTerminatedString(); // category
                        reader.ReadNullTerminatedString(); // version
                        reader.ReadNullTerminatedString(); // description

                        if (version >= 17)
                            reader.ReadNullTerminatedString();

                        if (version >= 18)
                            reader.ReadNullTerminatedString();

                        // icon
                        int size = reader.ReadInt();
                        if (size > 0) reader.ReadBytes(size);

                        // 4 screenshots
                        for (int i = 0; i < 4; i++)
                        {
                            size = reader.ReadInt();
                            if (size > 0) reader.ReadBytes(size);
                        }

                        // DEXResource (v18+)
                        if (version >= 18)
                        {
                            size = reader.ReadInt();
                            if (size > 0) reader.ReadBytes(size);
                        }

                        reader.ReadInt(); // superbundle count (added)

                        #endregion

                        #region Add dummies (bundles, EBX, RES, chunks)

                        // bundles
                        int numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            string name   = reader.ReadNullTerminatedString();
                            string sbName = reader.ReadNullTerminatedString();
                            BundleType type = (BundleType)reader.ReadInt();
                            App.AssetManager.AddBundle(name, type, App.AssetManager.GetSuperBundleId(sbName));
                        }

                        // EBX dummies
                        numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            EbxAssetEntry entry = new EbxAssetEntry
                            {
                                Name = reader.ReadNullTerminatedString(),
                                Guid = reader.ReadGuid()
                            };
                            App.AssetManager.AddEbx(entry);
                            entry.IsDirty = true;
                        }

                        // RES dummies
                        numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            ResAssetEntry entry = new ResAssetEntry
                            {
                                Name     = reader.ReadNullTerminatedString(),
                                ResRid   = reader.ReadULong(),
                                ResType  = reader.ReadUInt(),
                                ResMeta  = reader.ReadBytes(0x10)
                            };
                            App.AssetManager.AddRes(entry);
                        }

                        // chunk dummies
                        numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            ChunkAssetEntry newEntry = new ChunkAssetEntry
                            {
                                Id  = reader.ReadGuid(),
                                H32 = reader.ReadInt()
                            };
                            App.AssetManager.AddChunk(newEntry);
                        }

                        #endregion

                        #region Write modified data

                        Dictionary<int, AssetEntry> h32map = new Dictionary<int, AssetEntry>();
                        bool allowOverwrite = false;
                        bool userDecision   = false;

                        // --- EBX ---
                        numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            string name = reader.ReadNullTerminatedString();
                            List<AssetEntry> linkedEntries = FrostyProject.LoadLinkedAssets(reader);
                            List<int> bundles = new List<int>();

                            if (version >= 13)
                            {
                                int length = reader.ReadInt();
                                for (int j = 0; j < length; j++)
                                {
                                    string bundleName = reader.ReadNullTerminatedString();
                                    int bid = App.AssetManager.GetBundleId(bundleName);
                                    if (bid != -1) bundles.Add(bid);
                                }
                            }

                            bool isModified          = reader.ReadBoolean();
                            bool isTransientModified = false;
                            string userData          = "";
                            byte[] data              = null;
                            bool modifiedResource    = false;

                            if (isModified)
                            {
                                isTransientModified = reader.ReadBoolean();
                                if (version >= 12)
                                    userData = reader.ReadNullTerminatedString();

                                if (version < 13)
                                {
                                    int length = reader.ReadInt();
                                    for (int j = 0; j < length; j++)
                                    {
                                        string bundleName = reader.ReadNullTerminatedString();
                                        int bid = App.AssetManager.GetBundleId(bundleName);
                                        if (bid != -1) bundles.Add(bid);
                                    }
                                }

                                if (version >= 13)
                                    modifiedResource = reader.ReadBoolean();

                                data = reader.ReadBytes(reader.ReadInt());
                            }

                            EbxAssetEntry entry = App.AssetManager.GetEbxEntry(name);
                            if (entry == null) continue;

                            if (!userDecision && !entry.IsDirty && entry.IsModified)
                            {
                                MessageBoxResult result = FrostyMessageBox.Show(
                                    "Do you wish to overwrite modified files in this project with those from the imported one?",
                                    "Project Merger", MessageBoxButton.YesNo);
                                allowOverwrite = result == MessageBoxResult.Yes;
                                userDecision   = true;
                            }

                            if (allowOverwrite || entry.IsDirty || !entry.IsModified)
                            {
                                entry.LinkedAssets.AddRange(linkedEntries);
                                entry.AddedBundles.AddRange(bundles);

                                if (isModified)
                                {
                                    entry.ModifiedEntry = new ModifiedAssetEntry
                                    {
                                        IsTransientModified = isTransientModified,
                                        UserData            = userData
                                    };

                                    if (modifiedResource)
                                    {
                                        entry.ModifiedEntry.DataObject = ModifiedResource.Read(data);
                                    }
                                    else
                                    {
                                        if (!entry.IsAdded && App.PluginManager.GetCustomHandler(entry.Type) != null)
                                        {
                                            // custom-handler assets cannot be merged as raw EBX
                                        }

                                        using (EbxReader ebxReader = EbxReader.CreateProjectReader(new MemoryStream(data), App.FileSystemManager))
                                        {
                                            EbxAsset asset = ebxReader.ReadAsset<EbxAsset>();
                                            entry.ModifiedEntry.DataObject = asset;
                                            if (entry.IsAdded)
                                                entry.Type = asset.RootObject.GetType().Name;
                                            entry.ModifiedEntry.DependentAssets.AddRange(asset.Dependencies);
                                        }
                                    }

                                    entry.OnModified();
                                    entry.IsDirty = true;
                                }

                                int hash = Fnv1.HashString(entry.Name);
                                if (!h32map.ContainsKey(hash))
                                    h32map.Add(hash, entry);
                            }
                        }

                        // --- RES ---
                        numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            string name = reader.ReadNullTerminatedString();
                            List<AssetEntry> linkedEntries = FrostyProject.LoadLinkedAssets(reader);
                            List<int> bundles = new List<int>();

                            if (version >= 13)
                            {
                                int length = reader.ReadInt();
                                for (int j = 0; j < length; j++)
                                {
                                    string bundleName = reader.ReadNullTerminatedString();
                                    int bid = App.AssetManager.GetBundleId(bundleName);
                                    if (bid != -1) bundles.Add(bid);
                                }
                            }

                            bool isModified    = reader.ReadBoolean();
                            Sha1 sha1          = Sha1.Zero;
                            long originalSize  = 0;
                            byte[] resMeta     = null;
                            byte[] data        = null;
                            string userData    = "";

                            if (isModified)
                            {
                                sha1         = reader.ReadSha1();
                                originalSize = reader.ReadLong();

                                int length = reader.ReadInt();
                                if (length > 0)
                                    resMeta = reader.ReadBytes(length);

                                if (version >= 12)
                                    userData = reader.ReadNullTerminatedString();

                                if (version < 13)
                                {
                                    length = reader.ReadInt();
                                    for (int j = 0; j < length; j++)
                                    {
                                        string bundleName = reader.ReadNullTerminatedString();
                                        int bid = App.AssetManager.GetBundleId(bundleName);
                                        if (bid != -1) bundles.Add(bid);
                                    }
                                }

                                data = reader.ReadBytes(reader.ReadInt());
                            }

                            ResAssetEntry entry = App.AssetManager.GetResEntry(name);
                            if (entry == null) continue;

                            if (allowOverwrite || entry.IsDirty || !entry.IsModified)
                            {
                                entry.LinkedAssets.AddRange(linkedEntries);
                                entry.AddedBundles.AddRange(bundles);

                                if (isModified)
                                {
                                    entry.ModifiedEntry = new ModifiedAssetEntry
                                    {
                                        Sha1         = sha1,
                                        OriginalSize = originalSize,
                                        ResMeta      = resMeta,
                                        UserData     = userData
                                    };

                                    if (sha1 == Sha1.Zero)
                                        entry.ModifiedEntry.DataObject = ModifiedResource.Read(data);
                                    else
                                        entry.ModifiedEntry.Data = data;

                                    entry.OnModified();
                                }

                                int hash = Fnv1.HashString(entry.Name);
                                if (!h32map.ContainsKey(hash))
                                    h32map.Add(hash, entry);
                            }
                        }

                        // --- Chunks ---
                        numItems = reader.ReadInt();
                        for (int i = 0; i < numItems; i++)
                        {
                            Guid id = reader.ReadGuid();
                            List<int> bundles      = new List<int>();
                            List<int> superBundles = new List<int>();

                            if (version >= 13)
                            {
                                int length = reader.ReadInt();
                                for (int j = 0; j < length; j++)
                                {
                                    string bundleName = reader.ReadNullTerminatedString();
                                    int bid = App.AssetManager.GetBundleId(bundleName);
                                    if (bid != -1) bundles.Add(bid);
                                }
                            }

                            if (version > 13)
                            {
                                int length = reader.ReadInt();
                                for (int j = 0; j < length; j++)
                                {
                                    string sbName = reader.ReadNullTerminatedString();
                                    int sbid = App.AssetManager.GetSuperBundleId(sbName);
                                    if (sbid != -1) superBundles.Add(sbid);
                                }
                            }

                            Sha1   sha1          = Sha1.Zero;
                            uint   logicalOffset = 0;
                            uint   logicalSize   = 0;
                            uint   rangeStart    = 0;
                            uint   rangeEnd      = 0;
                            int    firstMip      = -1;
                            int    h32           = 0;
                            bool   addToChunkBundles = false;
                            string userData      = "";
                            byte[] data          = null;

                            if (version > 15)
                            {
                                firstMip = reader.ReadInt();
                                h32      = reader.ReadInt();
                            }

                            bool isModified = true;
                            if (version >= 13)
                                isModified = reader.ReadBoolean();

                            if (isModified)
                            {
                                sha1          = reader.ReadSha1();
                                logicalOffset = reader.ReadUInt();
                                logicalSize   = reader.ReadUInt();
                                rangeStart    = reader.ReadUInt();
                                rangeEnd      = reader.ReadUInt();

                                if (version < 16)
                                {
                                    firstMip = reader.ReadInt();
                                    h32      = reader.ReadInt();
                                }

                                addToChunkBundles = reader.ReadBoolean();
                                if (version >= 12)
                                    userData = reader.ReadNullTerminatedString();

                                if (version < 13)
                                {
                                    int length = reader.ReadInt();
                                    for (int j = 0; j < length; j++)
                                    {
                                        string bundleName = reader.ReadNullTerminatedString();
                                        int bid = App.AssetManager.GetBundleId(bundleName);
                                        if (bid != -1) bundles.Add(bid);
                                    }
                                }

                                data = reader.ReadBytes(reader.ReadInt());
                            }

                            ChunkAssetEntry entry = App.AssetManager.GetChunkEntry(id);

                            // If chunk doesn't exist yet, create it and link it via h32 to the owning asset's bundles
                            if (entry == null && isModified)
                            {
                                ChunkAssetEntry newEntry = new ChunkAssetEntry { Id = id, H32 = h32 };
                                App.AssetManager.AddChunk(newEntry);

                                if (h32map.ContainsKey(newEntry.H32))
                                {
                                    foreach (int bundleId in h32map[newEntry.H32].Bundles)
                                        newEntry.AddToBundle(bundleId);
                                }
                                entry = newEntry;
                            }

                            if (entry != null)
                            {
                                entry.AddedBundles.AddRange(bundles);
                                entry.AddedSuperBundles.AddRange(superBundles);

                                if (isModified)
                                {
                                    entry.ModifiedEntry = new ModifiedAssetEntry
                                    {
                                        Sha1             = sha1,
                                        LogicalOffset    = logicalOffset,
                                        LogicalSize      = logicalSize,
                                        RangeStart       = rangeStart,
                                        RangeEnd         = rangeEnd,
                                        FirstMip         = firstMip,
                                        H32              = h32,
                                        AddToChunkBundle = addToChunkBundles,
                                        UserData         = userData,
                                        Data             = data
                                    };
                                    entry.OnModified();
                                }
                                else
                                {
                                    entry.H32      = h32;
                                    entry.FirstMip = firstMip;
                                }
                            }
                        }

                        #endregion

                        App.Logger.Log(Path.GetFileName(openFileDialog.FileName) + " has been merged successfully.");

#if !FROSTY_DEVELOPER
                    }
                    catch (Exception ex)
                    {
                        App.Logger.LogError("Project merging failed: " + ex.Message);
                    }
#endif
                }
            });

            App.EditorWindow.DataExplorer.RefreshItems();
            typeof(MainWindow).InvokeMember("ResetItemsSources",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod,
                null, Application.Current.MainWindow, Array.Empty<object>());
        });
    }
}
