using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Managers.Entries;
using FrostySdk.Resources;
using MeshSetPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static BundleEditPlugin.StrandHairBindAssetExtension;

namespace BundleEditPlugin
{
    #region RemoveFromBundleExtension
    public class RemoveMeshExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "MeshAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic meshAsset = asset.RootObject;

            //Add res to BUNDLES AND LINK
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(meshAsset.MeshSetResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);

            MeshSet meshSetRes = App.AssetManager.GetResAs<MeshSet>(resEntry);

            //Double check if there are any LODs the mesh, if there are, bundle and link them
            // J-Lyt | If chunk is in SuperBundle, do not add to bundle.
            if (meshSetRes.Lods.Count > 0)
            {
                foreach (MeshSetLod lod in meshSetRes.Lods)
                {
                    if (lod.ChunkId != Guid.Empty)
                    {
                        ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(lod.ChunkId);
                        if (chunkEntry != null && chunkEntry.SuperBundles.Count == 0 && chunkEntry.AddedSuperBundles.Count == 0)
                        {
                            chunkEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
                            resEntry.LinkAsset(chunkEntry);
                        }
                    }
                }
            }
        }
    }

    // J-Lyt | Remove ClothWrappingAsset from Bundle
    public class RemoveClothWrappingExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "ClothWrappingAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic clothWrappingAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(clothWrappingAsset.ClothWrappingAssetResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Remove ClothAsset from Bundle
    public class RemoveClothExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "ClothAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic clothAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(clothAsset.ClothAssetResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Remove ClothColliderSetAsset from Bundle
    public class RemoveClothColliderSetExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "ClothColliderSetAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic clothColliderSetAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(clothColliderSetAsset.ClothColliderSetAssetResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Remove DefaultGeometryModifier from Bundle
    public class RemoveDefaultGeometryModifierExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "DefaultGeometryModifier";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic defaultGeometryModifierAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(defaultGeometryModifierAsset.SourceSpaceResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

           entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Remove DynamicMorphHeadData from Bundle
    public class RemoveDynamicMorphHeadDataExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "DynamicMorphHeadData";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic dynamicMorphHeadDataAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(dynamicMorphHeadDataAsset.MeshWrapRemappingResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

  // J-Lyt | Remove MeshComputeAsset from Bundle
    public class RemoveMeshComputeExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "MeshComputeAsset";
      public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            dynamic runtimeNodesEntry = App.AssetManager.GetEbx(entry).RootObject;
            dynamic runtimeNodes = runtimeNodesEntry.RuntimeNodes;

            if (runtimeNodes.Count > 0)
            {
              for (int i = 0; i < runtimeNodes.Count; i++)
                {
                    var runtimeNode = runtimeNodes[i];

                    // Skip if NodeResource is 0
                    if (runtimeNode.NodeResource == 0)
                        continue;

                    ResAssetEntry resEntry = App.AssetManager.GetResEntry(runtimeNode.NodeResource);
                    resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
                    entry.LinkAsset(resEntry);
                }
          }
        }
    }

   // J-Lyt | Remove StrandHairAsset from Bundle
    public class RemoveStrandHairAssetExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "StrandHairAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
           dynamic strandHairAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(strandHairAsset.StrandHairAssetResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            ResAssetEntry resEntry2 = App.AssetManager.GetResEntry(strandHairAsset.StrandHairSetResource);
            resEntry2.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
            entry.LinkAsset(resEntry2);
        }
    }

    // J-Lyt | Remove StrandHairBindAsset from Bundle
    public class RemoveStrandHairBindAssetExtension : RemoveFromBundleExtension
    {
       public override string AssetType => "StrandHairBindAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic strandHairBindAsset = asset.RootObject;

           ResAssetEntry resEntry = App.AssetManager.GetResEntry(strandHairBindAsset.StrandHairBindAssetResource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    public class RemoveSvgImageExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "SvgImage";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic svgAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(svgAsset.Resource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    public class RemoveTextureBaseExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "TextureBaseAsset";
        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic textureAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(textureAsset.Resource);
            resEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));

            Texture texture = App.AssetManager.GetResAs<Texture>(resEntry);
            ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(texture.ChunkId);

            chunkEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
            chunkEntry.FirstMip = texture.FirstMip;

            resEntry.LinkAsset(chunkEntry);
            entry.LinkAsset(resEntry);
        }
    }

    public class RemoveMovieTextureExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "MovieTextureBaseAsset";

        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset movieasset = App.AssetManager.GetEbx(entry);
            dynamic movieobject = movieasset.RootObject;

            ChunkAssetEntry MovieChunkEntry = App.AssetManager.GetChunkEntry(movieobject.ChunkGuid);
            MovieChunkEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
            entry.LinkAsset(MovieChunkEntry);

            ChunkAssetEntry SubtitleChunkEntry = App.AssetManager.GetChunkEntry(movieobject.SubtitleChunkGuid);
            if (SubtitleChunkEntry != null)
            {
                SubtitleChunkEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
                entry.LinkAsset(SubtitleChunkEntry);
            }
        }
    }

    public class RemoveSoundWaveExtension : RemoveFromBundleExtension
    {
        public override string AssetType => "SoundWaveAsset";

        public override void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.RemoveFromBundle(entry, bentry);

            EbxAsset soundasset = App.AssetManager.GetEbx(entry);
            dynamic soundobject = soundasset.RootObject;

            foreach (var soundChunk in soundobject.Chunks)
            {
                ChunkAssetEntry ChunkEntry = App.AssetManager.GetChunkEntry(soundChunk.ChunkId);
                ChunkEntry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
                entry.LinkAsset(ChunkEntry);
            }
        }
    }

    public class RemoveFromBundleExtension
    {
        public virtual string AssetType => null;
        public virtual void RemoveFromBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            entry.AddedBundles.Remove(App.AssetManager.GetBundleId(bentry));
        }
    }
    #endregion

    #region AddToBundleExtension
    public class MeshExtension : AddToBundleExtension
    {
        public override string AssetType => "MeshAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic meshAsset = asset.RootObject;

            //Add res to BUNDLES AND LINK
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(meshAsset.MeshSetResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);

            MeshSet meshSetRes = App.AssetManager.GetResAs<MeshSet>(resEntry);

            //Double check if there are any LODs the mesh, if there are, bundle and link them
            // J-Lyt | If chunk is in SuperBundle, do not add to bundle.
            if (meshSetRes.Lods.Count > 0)
            {
                foreach (MeshSetLod lod in meshSetRes.Lods)
                {
                    if (lod.ChunkId != Guid.Empty)
                    {
                        ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(lod.ChunkId);
                        if (chunkEntry != null && chunkEntry.SuperBundles.Count == 0 && chunkEntry.AddedSuperBundles.Count == 0)
                        {
                            chunkEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));
                            resEntry.LinkAsset(chunkEntry);
                        }
                    }
                }
            }
        }
    }

    // J-Lyt | Add ClothWrappingAsset to Bundle
    public class ClothWrappingExtension : AddToBundleExtension
    {
        public override string AssetType => "ClothWrappingAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic clothWrappingAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(clothWrappingAsset.ClothWrappingAssetResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Add ClothAsset to Bundle
    public class ClothExtension : AddToBundleExtension
    {
        public override string AssetType => "ClothAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic clothAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(clothAsset.ClothAssetResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Add ClothColliderSetAsset to Bundle
    public class ClothColliderSetExtension : AddToBundleExtension
    {
        public override string AssetType => "ClothColliderSetAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic clothColliderSetAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(clothColliderSetAsset.ClothColliderSetAssetResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Add DefaultGeometryModifier to Bundle
    public class DefaultGeometryModifierExtension : AddToBundleExtension
    {
        public override string AssetType => "DefaultGeometryModifier";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic defaultGeometryModifierAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(defaultGeometryModifierAsset.SourceSpaceResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
   }

    // J-Lyt | Add DynamicMorphHeadData to Bundle
    public class DynamicMorphHeadDataExtension : AddToBundleExtension
    {
        public override string AssetType => "DynamicMorphHeadData";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic dynamicMorphHeadDataAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(dynamicMorphHeadDataAsset.MeshWrapRemappingResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

           entry.LinkAsset(resEntry);
        }
    }

    // J-Lyt | Add MeshComputeAsset to Bundle
   public class MeshComputeExtension : AddToBundleExtension
    {
        public override string AssetType => "MeshComputeAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            dynamic runtimeNodesEntry = App.AssetManager.GetEbx(entry).RootObject;
            dynamic runtimeNodes = runtimeNodesEntry.RuntimeNodes;

            if (runtimeNodes.Count > 0)
            {
                for (int i = 0; i < runtimeNodes.Count; i++)
                {
                    var runtimeNode = runtimeNodes[i];

                    // Skip if NodeResource is 0
                    if (runtimeNode.NodeResource == 0)
                      continue;

                    ResAssetEntry resEntry = App.AssetManager.GetResEntry(runtimeNode.NodeResource);
                    resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));
                    entry.LinkAsset(resEntry);
                }
            }
        }
    }

    // J-Lyt | Add StrandHairAsset to Bundle

    public class StrandHairAssetExtension : AddToBundleExtension
    {
        public override string AssetType => "StrandHairAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic strandHairAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(strandHairAsset.StrandHairAssetResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            ResAssetEntry resEntry2 = App.AssetManager.GetResEntry(strandHairAsset.StrandHairSetResource);
          resEntry2.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
            entry.LinkAsset(resEntry2);
        }
    }

   // J-Lyt | Add StrandHairBindAsset to Bundle
    public class StrandHairBindAssetExtension : AddToBundleExtension
    {
        public override string AssetType => "StrandHairBindAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
      {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic strandHairBindAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(strandHairBindAsset.StrandHairBindAssetResource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

          entry.LinkAsset(resEntry);
        }

    public class SvgImageExtension : AddToBundleExtension
    {
        public override string AssetType => "SvgImage";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic svgAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(svgAsset.Resource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            entry.LinkAsset(resEntry);
        }
    }

    public class TextureBaseExtension : AddToBundleExtension
    {
        public override string AssetType => "TextureBaseAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic textureAsset = asset.RootObject;

            ResAssetEntry resEntry = App.AssetManager.GetResEntry(textureAsset.Resource);
            resEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));

            Texture texture = App.AssetManager.GetResAs<Texture>(resEntry);
            ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(texture.ChunkId);

            chunkEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));
            chunkEntry.FirstMip = texture.FirstMip;

            resEntry.LinkAsset(chunkEntry);
            entry.LinkAsset(resEntry);
        }
    }

    public class MovieTextureExtension : AddToBundleExtension
    {
        public override string AssetType => "MovieTextureBaseAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic movieAsset = asset.RootObject;

            ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(movieAsset.ChunkGuid);
            chunkEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));
            entry.LinkAsset(chunkEntry);

            chunkEntry = App.AssetManager.GetChunkEntry(movieAsset.SubtitleChunkGuid);
            if (chunkEntry != null)
            {
                chunkEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));
                entry.LinkAsset(chunkEntry);
            }
        }
    }

    public class SoundWaveExtension : AddToBundleExtension
    {
        public override string AssetType => "SoundWaveAsset";
        public override void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            base.AddToBundle(entry, bentry);

            EbxAsset asset = App.AssetManager.GetEbx(entry);
            dynamic soundAsset = asset.RootObject;

            foreach (var soundDataChunk in soundAsset.Chunks)
            {
                ChunkAssetEntry chunkEntry = App.AssetManager.GetChunkEntry(soundDataChunk.ChunkId);
                chunkEntry.AddToBundle(App.AssetManager.GetBundleId(bentry));
                entry.LinkAsset(chunkEntry);
            }
        }
    }

    public class AddToBundleExtension
    {
        public virtual string AssetType => null;
        public virtual void AddToBundle(EbxAssetEntry entry, BundleEntry bentry)
        {
            entry.AddToBundle(App.AssetManager.GetBundleId(bentry));
        }
    }
#endregion

    [TemplatePart(Name = PART_BundleTypeComboBox, Type = typeof(ComboBox))]
    [TemplatePart(Name = PART_BundlesListBox, Type = typeof(ListBox))]
    [TemplatePart(Name = PART_DataExplorer, Type = typeof(FrostyDataExplorer))]
    [TemplatePart(Name = PART_SuperBundleTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_BundleFilterTextBox, Type = typeof(TextBox))]
    public class BundleEditor : FrostyBaseEditor
    {
        private const string PART_BundleTypeComboBox = "PART_BundleTypeComboBox";
        private const string PART_BundlesListBox = "PART_BundlesListBox";
        private const string PART_DataExplorer = "PART_DataExplorer";
        private const string PART_SuperBundleTextBox = "PART_SuperBundleTextBox";
        private const string PART_BundleFilterTextBox = "PART_BundleFilterTextBox";

        public override ImageSource Icon => BundleEditorMenuExtension.iconImageSource;
        public RelayCommand AddToBundleCommand { get; }
        public RelayCommand RemoveFromBundleCommand { get; }

        private ComboBox bundleTypeComboBox;
        private ListBox bundlesListBox;
        private FrostyDataExplorer dataExplorer;
        private TextBox superBundleTextBox;
        private TextBox bundleFilterTextBox;

        private BundleType selectedBundleType = BundleType.SharedBundle;
        private Dictionary<string, AddToBundleExtension> addToBundleExtensions = new Dictionary<string, AddToBundleExtension>();
        private Dictionary<string, RemoveFromBundleExtension> removeFromBundleExtensions = new Dictionary<string, RemoveFromBundleExtension>();

        static BundleEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BundleEditor), new FrameworkPropertyMetadata(typeof(BundleEditor)));
        }

        public BundleEditor()
        {
            foreach (var type in Assembly.GetCallingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(AddToBundleExtension)))
                {
                    var extension = (AddToBundleExtension)Activator.CreateInstance(type);
                    addToBundleExtensions.Add(extension.AssetType, extension);
                }
                else if (type.IsSubclassOf(typeof(RemoveFromBundleExtension)))
                {
                    var extension = (RemoveFromBundleExtension)Activator.CreateInstance(type);
                    removeFromBundleExtensions.Add(extension.AssetType, extension);
                }
            }
            addToBundleExtensions.Add("null", new AddToBundleExtension());
            removeFromBundleExtensions.Add("null", new RemoveFromBundleExtension());

            AddToBundleCommand = new RelayCommand(
                (o) =>
                {
                    EbxAssetEntry entry = App.EditorWindow.DataExplorer.SelectedAsset as EbxAssetEntry;
                    BundleEntry bentry = bundlesListBox.SelectedItem as BundleEntry;

                    if (!entry.Bundles.Contains(App.AssetManager.GetBundleId(bentry)) && !entry.AddedBundles.Contains(App.AssetManager.GetBundleId(bentry)))
                    {
                        string key = entry.Type;
                        if (!addToBundleExtensions.ContainsKey(entry.Type))
                        {
                            key = "null";
                            foreach (string typekey in addToBundleExtensions.Keys)
                            {
                                if (TypeLibrary.IsSubClassOf(entry.Type, typekey))
                                {
                                    key = typekey;
                                    break;
                                }
                            }
                        }
                        addToBundleExtensions[key].AddToBundle(entry, bentry);
                    }

                    else
                    {
                        App.Logger.LogError("Asset is already in {0}", bentry.Name);
                    }

                    RefreshExplorer();
                    App.EditorWindow.DataExplorer.RefreshItems();

                    dataExplorer.SelectAsset(entry);
                },
                (o) =>
                {
                    return App.EditorWindow.DataExplorer.SelectedAsset != null && bundlesListBox.SelectedItem != null;
                });
            RemoveFromBundleCommand = new RelayCommand(
                (o) =>
                {
                    EbxAssetEntry entry = App.EditorWindow.DataExplorer.SelectedAsset as EbxAssetEntry;
                    BundleEntry bentry = bundlesListBox.SelectedItem as BundleEntry;

                    if (entry.AddedBundles.Contains(App.AssetManager.GetBundleId(bentry)))
                    {
                        string key = entry.Type;
                        if (!removeFromBundleExtensions.ContainsKey(entry.Type))
                        {
                            key = "null";
                            foreach (string typekey in removeFromBundleExtensions.Keys)
                            {
                                if (TypeLibrary.IsSubClassOf(entry.Type, typekey))
                                {
                                    key = typekey;
                                    break;
                                }
                            }
                        }
                        removeFromBundleExtensions[key].RemoveFromBundle(entry, bentry);
                    }

                    else
                    {
                        App.Logger.LogError("{0} cannot be removed from this asset, are you sure its an added bundle?", bentry.Name);
                    }

                    RefreshExplorer();
                    App.EditorWindow.DataExplorer.RefreshItems();
                },
                (o) =>
                {
                    return App.EditorWindow.DataExplorer.SelectedAsset != null && bundlesListBox.SelectedItem != null;
                });
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            bundleTypeComboBox = GetTemplateChild(PART_BundleTypeComboBox) as ComboBox;
            bundlesListBox = GetTemplateChild(PART_BundlesListBox) as ListBox;
            dataExplorer = GetTemplateChild(PART_DataExplorer) as FrostyDataExplorer;
            superBundleTextBox = GetTemplateChild(PART_SuperBundleTextBox) as TextBox;
            bundleFilterTextBox = GetTemplateChild(PART_BundleFilterTextBox) as TextBox;

            bundleTypeComboBox.SelectionChanged += bundleTypeComboBox_SelectionChanged;
            bundlesListBox.SelectionChanged += bundlesListBox_SelectionChanged;
            dataExplorer.SelectedAssetDoubleClick += dataExplorer_SelectedAssetDoubleClick;

            bundleFilterTextBox.KeyUp += BundleFilterTextBox_KeyUp;
            bundleFilterTextBox.LostFocus += BundleFilterTextBox_LostFocus;

            bundleTypeComboBox.SelectedIndex = 2;
        }

        private void BundleFilterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (bundleFilterTextBox.Text == "")
                bundlesListBox.Items.Filter = null;
            else
            {
                string filterText = bundleFilterTextBox.Text.ToLower();
                bundlesListBox.Items.Filter = (object a) => { return ((BundleEntry)a).Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0; };
            }
        }

        private void BundleFilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bundleFilterTextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void bundlesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshExplorer();
        }

        private void bundleTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bundlesListBox.Items.Filter = null;
            bundleFilterTextBox.Text = "";

            int index = bundleTypeComboBox.SelectedIndex;
            selectedBundleType = (new BundleType[] { BundleType.SubLevel, BundleType.BlueprintBundle, BundleType.SharedBundle })[index];
            RefreshList();
        }

        private void RefreshList()
        {
            bundlesListBox.ItemsSource = App.AssetManager.EnumerateBundles(selectedBundleType);
            bundlesListBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("DisplayName", System.ComponentModel.ListSortDirection.Ascending));
        }

        private void RefreshExplorer()
        {
            BundleEntry entry = bundlesListBox.SelectedItem as BundleEntry;
            if (entry == null)
                return;
            dataExplorer.ItemsSource = App.AssetManager.EnumerateEbx(entry);
            superBundleTextBox.Text = App.AssetManager.GetSuperBundle(entry.SuperBundleId).Name;
            if (entry.Type != BundleType.SharedBundle)
                dataExplorer.SelectAsset(entry.Blueprint);
        }

        private void dataExplorer_SelectedAssetDoubleClick(object sender, RoutedEventArgs e)
        {
            EbxAssetEntry entry = dataExplorer.SelectedAsset as EbxAssetEntry;
            App.EditorWindow.OpenAsset(entry);
        }
    }
}
