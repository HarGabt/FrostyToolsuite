using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Attributes;
using Frosty.Core.Misc;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FrostySdk.Managers.Entries;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using System.IO;
using System.Windows.Media;

namespace ReferencesPlugin
{
    [TemplatePart(Name = PART_RefExplorerToTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_RefExplorerFromTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_RefExplorerToListView, Type = typeof(FrostyAssetListView))]
    [TemplatePart(Name = PART_RefExplorerFromListView, Type = typeof(FrostyAssetListView))]
    public class ReferenceTabItem : FrostyTabItem
    {
        private const string PART_RefExplorerToTextBlock = "PART_RefExplorerToTextBlock";
        private const string PART_RefExplorerFromTextBlock = "PART_RefExplorerFromTextBlock";
        private const string PART_RefExplorerToListView = "PART_RefExplorerToListView";
        private const string PART_RefExplorerFromListView = "PART_RefExplorerFromListView";
        private const string PART_RefExplorerToOpenItem = "PART_RefExplorerToOpenItem";
        private const string PART_RefExplorerFromOpenItem = "PART_RefExplorerFromOpenItem";

        private const string PART_RefExplorerToFindItem = "PART_RefExplorerToFindItem";
        private const string PART_RefExplorerFromFindItem = "PART_RefExplorerFromFindItem";
		
        // AdamRaichu
        private const string PART_RefExplorerToOpenBlueprintEditor = "PART_RefExplorerToOpenBlueprintEditor";
        private const string PART_RefExplorerFromOpenBlueprintEditor = "PART_RefExplorerFromOpenBlueprintEditor";
        private const string PART_RefExplorerToCopyGUID = "PART_RefExplorerToCopyGUID";
        private const string PART_RefExplorerFromCopyGUID = "PART_RefExplorerFromCopyGUID";
        private const string PART_RefExplorerToCopyFilePath = "PART_RefExplorerToCopyFilePath";
        private const string PART_RefExplorerFromCopyFilePath = "PART_RefExplorerFromCopyFilePath";

        // Mophead
        private const string PART_RefExplorerToDisplayInfo = "PART_RefExplorerToDisplayInfo";
        private const string PART_RefExplorerFromDisplayInfo = "PART_RefExplorerFromDisplayInfo";
        private const string PART_RefExplorerToTextBlockInfo = "PART_RefExplorerToTextBlockInfo";
        private const string PART_RefExplorerFromTextBlockInfo = "PART_RefExplorerFromTextBlockInfo";

        private FrostyAssetListView refExplorerToList;
        private FrostyAssetListView refExplorerFromList;
        private TextBlock refExplorerToText;
        private TextBlock refExplorerFromText;
        private MenuItem refExplorerToOpenItem;
        private MenuItem refExplorerFromOpenItem;

        private MenuItem refExplorerToFindItem;
        private MenuItem refExplorerFromFindItem;

        // AdamRaichu
        private MenuItem refExplorerToOpenBlueprintEditor;
        private MenuItem refExplorerFromOpenBlueprintEditor;
        private MenuItem refExplorerToCopyGUID;
        private MenuItem refExplorerFromCopyGUID;
        private MenuItem refExplorerToCopyFilePath;
        private MenuItem refExplorerFromCopyFilePath;

        // Mophead
        private MenuItem refExplorerToDisplayInfo;
        private MenuItem refExplorerFromDisplayInfo;
        private TextBlock refExplorerToTextBlockInfo;
        private TextBlock refExplorerFromTextBlockInfo;

        private IBlueprintEditorHandler blueprintEditorHandler = App.PluginManager.BlueprintEditorOpenAction.Count() == 0 ? new DefaultBlueprintEditorHandler() : App.PluginManager.BlueprintEditorOpenAction.First();

        static ReferenceTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ReferenceTabItem), new FrameworkPropertyMetadata(typeof(ReferenceTabItem)));
        }

        public ReferenceTabItem()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Bind References Asset List View Elements
            refExplorerToList = GetTemplateChild(PART_RefExplorerToListView) as FrostyAssetListView;
            refExplorerFromList = GetTemplateChild(PART_RefExplorerFromListView) as FrostyAssetListView;

            // Bind References Textblock Elements
            refExplorerToText = GetTemplateChild(PART_RefExplorerToTextBlock) as TextBlock;
            refExplorerFromText = GetTemplateChild(PART_RefExplorerFromTextBlock) as TextBlock;

            // Bind References Textblock Info Elements
            refExplorerToTextBlockInfo = GetTemplateChild(PART_RefExplorerToTextBlockInfo) as TextBlock;
            refExplorerFromTextBlockInfo = GetTemplateChild(PART_RefExplorerFromTextBlockInfo) as TextBlock;

            // Bind Open Asset Elements
            refExplorerToOpenItem = GetTemplateChild(PART_RefExplorerToOpenItem) as MenuItem;
            refExplorerFromOpenItem = GetTemplateChild(PART_RefExplorerFromOpenItem) as MenuItem;

            // Bind Find Asset Elements
            refExplorerToFindItem = GetTemplateChild(PART_RefExplorerToFindItem) as MenuItem;
            refExplorerFromFindItem = GetTemplateChild(PART_RefExplorerFromFindItem) as MenuItem;
			
            // Bind Open Blueprint Editor Elements
            refExplorerToOpenBlueprintEditor = GetTemplateChild(PART_RefExplorerToOpenBlueprintEditor) as MenuItem;
            refExplorerFromOpenBlueprintEditor = GetTemplateChild(PART_RefExplorerFromOpenBlueprintEditor) as MenuItem;

            // Bind Copy GUID Elements
            refExplorerToCopyGUID = GetTemplateChild(PART_RefExplorerToCopyGUID) as MenuItem;
            refExplorerFromCopyGUID = GetTemplateChild(PART_RefExplorerFromCopyGUID) as MenuItem;

            // Bind Copy File Path Elements
            refExplorerToCopyFilePath = GetTemplateChild(PART_RefExplorerToCopyFilePath) as MenuItem;
            refExplorerFromCopyFilePath = GetTemplateChild(PART_RefExplorerFromCopyFilePath) as MenuItem;

            // Bind Display Asset Info Elements
            refExplorerToDisplayInfo = GetTemplateChild(PART_RefExplorerToDisplayInfo) as MenuItem;
            refExplorerFromDisplayInfo = GetTemplateChild(PART_RefExplorerFromDisplayInfo) as MenuItem;

            // Double Click Asset to Open
            refExplorerToList.SelectedAssetDoubleClick += ReferenceExplorerList_SelectedAssetDoubleClick;
            refExplorerFromList.SelectedAssetDoubleClick += ReferenceExplorerList_SelectedAssetDoubleClick;

            // Open Asset
            refExplorerToOpenItem.Click += contextMenuRefExplorerToOpen_Click;
            refExplorerFromOpenItem.Click += contextMenuRefExplorerFromOpen_Click;

            // Find Item in Data Explorer
            refExplorerToFindItem.Click += contextMenuRefExplorerToFind_Click;
            refExplorerFromFindItem.Click += contextMenuRefExplorerFromFind_Click;
			
            // Open Blueprint Editor
            refExplorerToOpenBlueprintEditor.Click += contextMenuRefExplorerToOpenBlueprintEditor_Click;
            refExplorerFromOpenBlueprintEditor.Click += contextMenuRefExplorerFromOpenBlueprintEditor_Click;

            // Copy GUID
            refExplorerToCopyGUID.Click += contextMenuRefExplorerToCopyGUID_Click;
            refExplorerFromCopyGUID.Click += contextMenuRefExplorerFromCopyGUID_Click;

            // Copy file path
            refExplorerToCopyFilePath.Click += RefExplorerToCopyFilePath_Click;
            refExplorerFromCopyFilePath.Click += RefExplorerFromCopyFilePath_Click;

            // Display Asset Info
            refExplorerToDisplayInfo.Click += RefExplorerToDisplayInfo_Click;
            refExplorerFromDisplayInfo.Click += RefExplorerFromDisplayInfo_Click;

            Loaded += ReferenceTabItem_Loaded;

            App.EditorWindow.DataExplorer.SelectionChanged += dataExplorer_SelectionChanged;
        }

        private Dictionary<EbxAssetEntry, string> SpecialInfoTo = new Dictionary<EbxAssetEntry, string>();
        private Dictionary<EbxAssetEntry, string> SpecialInfoFrom = new Dictionary<EbxAssetEntry, string>();

        private void RefExplorerFromCopyFilePath_Click(object sender, EventArgs e)
        {
            EbxAssetEntry entry = refExplorerFromList.SelectedItem as EbxAssetEntry;
            if (entry == null)
                return;
            Clipboard.SetText(entry.Name);
        }

        private void RefExplorerToCopyFilePath_Click(object sender, EventArgs e)
        {
            EbxAssetEntry entry = refExplorerToList.SelectedItem as EbxAssetEntry;
            if (entry == null)
                return;
            Clipboard.SetText(entry.Name);
        }

        private void RefExplorerFromDisplayInfo_Click(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry entry = refExplorerFromList.SelectedItem as EbxAssetEntry;
            if (entry == null)
                return;
            refExplorerFromTextBlockInfo.Text = SpecialInfoFrom[entry];
        }

        private void RefExplorerToDisplayInfo_Click(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry entry = refExplorerToList.SelectedItem as EbxAssetEntry;
            if (entry == null)
                return;
            refExplorerToTextBlockInfo.Text = SpecialInfoTo[entry];
        }

        private void contextMenuRefExplorerToCopyGUID_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerToList.SelectedItem == null)
                return;
            if (refExplorerToList.SelectedItem is EbxAssetEntry entry)
            {
                Clipboard.SetText(entry.Guid.ToString());
            }
        }
        private void contextMenuRefExplorerFromCopyGUID_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerFromList.SelectedItem == null)
                return;
            if (refExplorerFromList.SelectedItem is EbxAssetEntry entry)
            {
                Clipboard.SetText(entry.Guid.ToString());
            }
        }

        private void contextMenuRefExplorerToOpenBlueprintEditor_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerToList.SelectedItem == null)
                return;

            if (refExplorerToList.SelectedItem is EbxAssetEntry) {
                App.Logger.Log("Opening blueprint editor (debug test)");
                blueprintEditorHandler.OpenAssetAsGraph((EbxAssetEntry)refExplorerToList.SelectedItem);
            }
        }
        private void contextMenuRefExplorerFromOpenBlueprintEditor_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerFromList.SelectedItem == null)
                return;

            if (refExplorerFromList.SelectedItem is EbxAssetEntry)
            {
                App.Logger.Log("Opening blueprint editor (debug test)");
                blueprintEditorHandler.OpenAssetAsGraph((EbxAssetEntry)refExplorerFromList.SelectedItem);
            }
        }

        private void ReferenceTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry selectedEntry = App.SelectedAsset;

            RefreshReferences(selectedEntry);
        }

        private void dataExplorer_SelectionChanged(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry selectedEntry = App.SelectedAsset;

            RefreshReferences(selectedEntry);
        }

        private void ReferenceExplorerList_SelectedAssetDoubleClick(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry entry = (sender as FrostyAssetListView).SelectedItem as EbxAssetEntry;
            if (entry == null)
                return;

            App.EditorWindow.OpenAsset(entry);
        }

        private void contextMenuRefExplorerToOpen_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerToList.SelectedItem == null)
                return;
            App.EditorWindow.OpenAsset(refExplorerToList.SelectedItem);
        }

        private void contextMenuRefExplorerFromOpen_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerFromList.SelectedItem == null)
                return;
            App.EditorWindow.OpenAsset(refExplorerFromList.SelectedItem);
        }

        private void contextMenuRefExplorerToFind_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerToList.SelectedItem == null)
                return;
            App.EditorWindow.DataExplorer.SelectAsset(refExplorerToList.SelectedItem);
        }

        private void contextMenuRefExplorerFromFind_Click(object sender, RoutedEventArgs e)
        {
            if (refExplorerFromList.SelectedItem == null)
                return;
            App.EditorWindow.DataExplorer.SelectAsset(refExplorerFromList.SelectedItem);
        }

        private void RefreshReferences(EbxAssetEntry entry)
        {
            LoadCache();
            SpecialInfoTo.Clear();
            SpecialInfoFrom.Clear();
            refExplorerToTextBlockInfo.Text = "";
            refExplorerFromTextBlockInfo.Text = "";
            if (entry == null)
            {
                refExplorerFromText.Text = "";
                refExplorerToText.Text = "No asset selected";
                refExplorerFromList.ItemsSource = null;
                refExplorerToList.ItemsSource = null;
                return;
            }

            refExplorerFromText.Text = "References from " + entry.Filename;
            refExplorerToText.Text = "References to " + entry.Filename;

            List<EbxAssetEntry> refToItems = new List<EbxAssetEntry>();
            List<EbxAssetEntry> refFromItems = new List<EbxAssetEntry>();

            foreach (EbxAssetEntry subEntry in App.AssetManager.EnumerateEbx())
            {
                if (subEntry.ContainsDependency(entry.Guid))
                {
                    refToItems.Add(subEntry);
                    if (!SpecialInfoTo.ContainsKey(subEntry))
                        SpecialInfoTo.Add(subEntry, string.Format("{0} directly references {1}", subEntry.Name, entry.Filename));
                }
            }
            foreach (Guid guid in entry.EnumerateDependencies())
            {
                refFromItems.Add(App.AssetManager.GetEbxEntry(guid));
                if (!SpecialInfoFrom.ContainsKey(App.AssetManager.GetEbxEntry(guid)))
                    SpecialInfoFrom.Add(App.AssetManager.GetEbxEntry(guid), string.Format("{0} directly references {1}", entry.Filename, App.AssetManager.GetEbxEntry(guid).Name));
            }
            if (entry.Type == "ShaderGraph")
            {
                foreach (ResAssetEntry resBlockEntry in App.AssetManager.EnumerateRes(resType: (uint)ResourceType.ShaderBlockDepot))
                {
                    if (resBlockEntry.Name == entry.Name.ToLower() + "_graph/blocks")
                    {
                        using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(resBlockEntry)))
                        {
                            for (int idx = 72; idx < Convert.ToInt32(reader.BaseStream.Length - 12); idx = idx + 4)
                            {
                                reader.BaseStream.Position = idx;
                                Guid ReadGuid = reader.ReadGuid();
                                if (TextureGuids.ContainsKey(ReadGuid))
                                {
                                    if (!refFromItems.Contains(App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid])))
                                    {
                                        refFromItems.Add(App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid]));
                                        SpecialInfoFrom.Add(App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid]), string.Format("{0} references {1} through res file", entry.Filename, App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid]).Filename));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (TypeLibrary.IsSubClassOf(entry.Type, "MeshAsset"))
            {
                foreach (EbxAssetEntry refEntry in entry.EnumerateDependencies().Select(o => App.AssetManager.GetEbxEntry(o)))
                {
                    foreach (ResAssetEntry resBlockEntry in App.AssetManager.EnumerateRes(resType: (uint)ResourceType.ShaderBlockDepot))
                    {
                        if (resBlockEntry.Name == refEntry.Name.ToLower() + "_graph/blocks")
                        {
                            using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(resBlockEntry)))
                            {
                                for (int idx = 72; idx < Convert.ToInt32(reader.BaseStream.Length - 12); idx = idx + 4)
                                {
                                    reader.BaseStream.Position = idx;
                                    Guid ReadGuid = reader.ReadGuid();
                                    if (TextureGuids.ContainsKey(ReadGuid))
                                    {
                                        if (!refFromItems.Contains(App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid])))
                                        {
                                            refFromItems.Add(App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid]));
                                            SpecialInfoFrom.Add(App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid]), string.Format("{0} references {1} through shadergraph {2} res file", entry.DisplayName, App.AssetManager.GetEbxEntry(TextureGuids[ReadGuid]).DisplayName, refEntry.DisplayName));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (metadataList.Count != 0 & entry.GetType().Name != "UIMetaDataAsset")
            {
                GetModifiedMetadata();
                EbxAsset refAsset = App.AssetManager.GetEbx(entry);
                Dictionary<UInt32, string> assetIdens = new Dictionary<uint, string>();
                dynamic refRoot = refAsset.RootObject;
                if (refRoot.GetType().GetProperty("Identifier") != null)
                    assetIdens.Add(refRoot.Identifier, "Identifier");
                UInt32 guidIden = FNVConvertGUID(refAsset.RootInstanceGuid.ToString());
                if (!assetIdens.ContainsKey(guidIden))
                    assetIdens.Add(guidIden, "Root Instance GUID");
                else
                    assetIdens[guidIden] = "Identifier & Root Instance GUID";
                if (refRoot.GetType().Name == "VisualUnlockRootAsset")
                {
                    Dictionary<EbxAssetEntry, string> bpbsToRef = new Dictionary<EbxAssetEntry, string>();
                    foreach (dynamic bpbRef in refRoot.ThirdPersonBundles)
                    {
                        EbxAssetEntry primBpbEntry = App.AssetManager.GetEbxEntry(bpbRef.Name);
                        if (primBpbEntry != null)
                        {
                            if (!bpbsToRef.ContainsKey(primBpbEntry))
                                bpbsToRef.Add(primBpbEntry, "Third Person Bpb");
                        }
                        foreach (dynamic par in bpbRef.Parents)
                        {
                            EbxAssetEntry bpbEntry = App.AssetManager.GetEbxEntry(par.Name);
                            if (bpbEntry != null)
                            {
                                if (!bpbsToRef.ContainsKey(bpbEntry))
                                    bpbsToRef.Add(bpbEntry, "Third Person Bpb Parent");
                            }
                        }

                    }
                    foreach (dynamic bpbRef in refRoot.FirstPersonBundles)
                    {
                        EbxAssetEntry primBpbEntry = App.AssetManager.GetEbxEntry(bpbRef.Name);
                        if (primBpbEntry != null)
                        {
                            if (!bpbsToRef.ContainsKey(primBpbEntry))
                                bpbsToRef.Add(primBpbEntry, "First Person Bpb");
                        }
                        foreach (dynamic par in bpbRef.Parents)
                        {
                            EbxAssetEntry bpbEntry = App.AssetManager.GetEbxEntry(par.Name);
                            if (bpbEntry != null)
                            {
                                if (!bpbsToRef.ContainsKey(bpbEntry))
                                    bpbsToRef.Add(bpbEntry, "First Person Bpb Parent");
                            }
                        }

                    }
                    foreach (dynamic skinInfo in refRoot.SkinInfos)
                    {
                        if (!assetIdens.ContainsKey(skinInfo.Identifier))
                            assetIdens.Add(skinInfo.Identifier, "SkinInfos Identifiers");
                        foreach (EbxAssetEntry bpbEntry in new List<string> { skinInfo.ThirdPersonBundle.Name, skinInfo.FirstPersonBundle.Name }.Select(o => App.AssetManager.GetEbxEntry(o)))
                        {
                            if (bpbEntry != null)
                            {
                                if (!bpbsToRef.ContainsKey(bpbEntry))
                                    bpbsToRef.Add(bpbEntry, "SkinInfo Bpbs");
                            }

                        }
                    }
                    foreach (EbxAssetEntry bpbEntry in bpbsToRef.Keys)
                    {
                        if (!refFromItems.Contains(bpbEntry))
                        {
                            refFromItems.Add(bpbEntry);
                            SpecialInfoFrom.Add(bpbEntry, string.Format("{0} references {1} through {2}", entry.DisplayName, bpbEntry.DisplayName, bpbsToRef[bpbEntry]));
                        }
                    }
                }
                foreach (UInt32 assetIden in assetIdens.Keys)
                {
                    if (modifiedMetadataList.ContainsKey(assetIden))
                    {
                        foreach (Tuple<string, int> tup in modifiedMetadataList[assetIden])
                        {
                            if (!refToItems.Contains(App.AssetManager.GetEbxEntry(tup.Item1)))
                            {
                                refToItems.Add(App.AssetManager.GetEbxEntry(tup.Item1));
                                if (!SpecialInfoTo.ContainsKey(App.AssetManager.GetEbxEntry(tup.Item1)))
                                    SpecialInfoTo.Add(App.AssetManager.GetEbxEntry(tup.Item1), string.Format("{0}[{2}] references {1} through metadata identifier using the asset {3}", App.AssetManager.GetEbxEntry(tup.Item1).Filename, entry.Filename, tup.Item2, assetIdens[assetIden]));
                            }
                        }
                    }
                    if (metadataList.ContainsKey(assetIden))
                    {
                        foreach (Tuple<string, int> tup in metadataList[assetIden])
                        {
                            if (!refToItems.Contains(App.AssetManager.GetEbxEntry(tup.Item1)) & !modifiedMetadataFiles.Contains(tup.Item1))
                            {
                                refToItems.Add(App.AssetManager.GetEbxEntry(tup.Item1));
                                if (!SpecialInfoTo.ContainsKey(App.AssetManager.GetEbxEntry(tup.Item1)))
                                    SpecialInfoTo.Add(App.AssetManager.GetEbxEntry(tup.Item1), string.Format("{0}[{2}] references {1} through metadata identifier using the asset {3}", App.AssetManager.GetEbxEntry(tup.Item1).Filename, entry.Filename, tup.Item2, assetIdens[assetIden]));
                            }
                        }
                    }
                }
            }

            refExplorerToList.ItemsSource = refToItems;
            refExplorerFromList.ItemsSource = refFromItems;
        }

        private void LoadCache()
        {
            if (TextureGuids.Count == 0)
            {
                if (File.Exists(CacheFileDir))
                {
                    using (NativeReader reader = new NativeReader(new FileStream(CacheFileDir, FileMode.Open)))
                    {
                        int texGuidCount = reader.ReadInt();
                        int metaDataCount = reader.ReadInt();
                        for (int i = 0; i < texGuidCount; i++)
                        {
                            Guid texFileGuid = reader.ReadGuid();
                            Guid texRootGuid = reader.ReadGuid();
                            TextureGuids.Add(texRootGuid, texFileGuid);
                        }
                        for (int i = 0; i < metaDataCount; i++)
                        {
                            UInt32 key = reader.ReadUInt();
                            int keycount = reader.ReadInt();
                            List<Tuple<string, int>> tuples = new List<Tuple<string, int>>();
                            for (int i2 = 0; i2 < keycount; i2++)
                                tuples.Add(new Tuple<string, int>(reader.ReadNullTerminatedString(), reader.ReadInt()));
                            metadataList.Add(key, tuples);
                        }
                    }
                }
                else
                    App.Logger.Log(string.Format("Cannot find cache file {0}, please generate one through \"Tools>Create Reference Cache\"", CacheFileDir));
            }
        }
		
        private void GetModifiedMetadata()
        {
            modifiedMetadataFiles.Clear();
            modifiedMetadataList.Clear();
            AssetManager AM = App.AssetManager;
            foreach (EbxAssetEntry refEntry in AM.EnumerateEbx(type: "UIMetaDataAsset"))
            {
                if (refEntry.HasModifiedData)
                {
                    modifiedMetadataFiles.Add(refEntry.Name);
                    EbxAsset refAsset = AM.GetEbx(refEntry);
                    dynamic refRoot = refAsset.RootObject;
                    int idx2 = 0;
                    foreach (dynamic item in refRoot.Items)
                    {
                        if (item.Type == PointerRefType.Internal)
                        {
                            UInt32 TypeFNV = FNVConvertMetadata(item.Internal.GetType().Name);
                            foreach (UInt32 Identifier in item.Internal.Identifiers)
                            {
                                UInt32 newIden = TypeFNV ^ Identifier;
                                if (modifiedMetadataList.ContainsKey(newIden))
                                    modifiedMetadataList[newIden].Add(new Tuple<string, int>(refEntry.Name, idx2));
                                else
                                    modifiedMetadataList.Add(newIden, new List<Tuple<string, int>>() { new Tuple<string, int>(refEntry.Name, idx2) });
                            }
                        }
                        idx2++;
                    }
                }
            }
        }
		
        List<string> modifiedMetadataFiles = new List<string>();
        Dictionary<UInt32, List<Tuple<string, int>>> modifiedMetadataList = new Dictionary<UInt32, List<Tuple<string, int>>>();
		
        static UInt32 FNVConvertMetadata(string FNVInput)
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
        static UInt32 FNVConvertGUID(string FNVInput)
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
                FNV_offset_basis_Temp = Convert.ToInt64(Output, 2) + 1;
            }
            return Convert.ToUInt32(FNV_offset_basis_Temp);
        }
		
        Dictionary<Guid, Guid> TextureGuids = new Dictionary<Guid, Guid>();
        Dictionary<UInt32, List<Tuple<string, int>>> metadataList = new Dictionary<UInt32, List<Tuple<string, int>>>();
        public static string CacheFileDir = System.AppDomain.CurrentDomain.BaseDirectory + @"Plugins\Caches\" + Enum.GetName(typeof(ProfileVersion), ProfilesLibrary.DataVersion) + "_ReferencePlugin_Cache.cache";
    }
	
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
