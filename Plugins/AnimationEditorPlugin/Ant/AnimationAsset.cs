using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using AssetBankPlugin.Enums;
using AssetBankPlugin.Export;
using FrostySdk;
using Frosty.Core;

namespace AssetBankPlugin.Ant
{
    public class AnimationAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }

        public int CodecType;
        public int AnimId;
        public float TrimOffset;
        public ushort EndFrame;
        public bool Additive;
        public Guid ChannelToDofAsset;
        public Dictionary<string, BoneChannelType> Channels;
        public float FPS;

        public StorageType StorageType;

        // Hardcoded rig GUID for PvZ GW2 filtering
        private static readonly Guid HardcodedRigGuid = new Guid("00080608-0000-0000-0000-000000000000");

        public AnimationAsset() { }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = (string)data["__name"];
            ID = (Guid)data["__guid"];
            CodecType = Convert.ToInt32(data["CodecType"]);
            AnimId = Convert.ToInt32(data["AnimId"]);
            TrimOffset = (float)data["TrimOffset"];
            EndFrame = Convert.ToUInt16(data["EndFrame"]);

            if (data.TryGetValue("Additive", out object additive))
            {
                Additive = (bool)additive;
            }
            ChannelToDofAsset = (Guid)data["ChannelToDofAsset"];
        }

        public LayoutHierarchyAsset RecursiveHierarchySearch(LayoutHierarchyAsset initialHierarchy, LayoutHierarchyAsset TargetHierarchy)
        {
            // If initial or target is null, prevent recursion crash
            if (initialHierarchy == null || TargetHierarchy == null) return null;

            if (initialHierarchy.ID == TargetHierarchy.ID)
            {
                return initialHierarchy;
            }

            if (initialHierarchy.Children != null)
            {
                for (int i = 0; i < initialHierarchy.Children.Length; i++)
                {
                    AntAsset asset = AntRefTable.Get(initialHierarchy.Children[i]);
                    if (asset is LayoutHierarchyAsset childHierarchy)
                    {
                        LayoutHierarchyAsset returnAsset = RecursiveHierarchySearch(childHierarchy, TargetHierarchy);
                        // Only return if it actually matched, do not return blind initialHierarchy.
                        if (returnAsset != null && returnAsset.ID == TargetHierarchy.ID)
                        {
                            return returnAsset;
                        }
                    }
                }
            }
            return null; // Must return null if not found, otherwise loops break.
        }

        public Dictionary<string, BoneChannelType> GetChannels(Guid channelToDofAsset)
        {
            // 1. Cache the profile version
            ProfileVersion currentProfile = (ProfileVersion)ProfilesLibrary.DataVersion;

            LayoutHierarchyAsset hierarchy = null;
            ChannelToDofAsset dof = (ChannelToDofAsset)AntRefTable.Get(channelToDofAsset);

            // Null-guard to prevent the exporter from crashing if the Dof asset is in an unloaded reference bank.
            if (dof == null)
            {
                App.Logger.LogError($"[AnimationAsset] Missing ChannelToDofAsset ({channelToDofAsset}) for '{Name}'. Missing Reference Bank.");
                return new Dictionary<string, BoneChannelType>();
            }

            StorageType = dof.StorageType;
            // 2. Iterate over .Values to avoid KeyValuePair struct allocation overhead
            var refsValues = AntRefTable.Refs.Values;

            switch (currentProfile)
            {
                case ProfileVersion.PlantsVsZombiesGardenWarfare2:
                    foreach (var asset in refsValues)
                    {
                        if (asset is ClipControllerAsset cl && cl.Anims != null)
                        {
                            // Resolves InternalRefs to actual IDs
                            if (cl.Anims.Any(a => AntRefTable.Get(a)?.ID == this.ID))
                            {
                                var targetAsset = AntRefTable.Get(cl.Target);
                                if (targetAsset is LayoutHierarchyAsset lh)
                                {
                                    FPS = cl.FPS;
                                    hierarchy = lh;
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case ProfileVersion.PlantsVsZombiesGardenWarfare:
                    foreach (var asset in refsValues)
                    {
                        if (asset is ClipControllerAsset cl)
                        {
                            // Resolves InternalRefs
                            if (AntRefTable.Get(cl.Anim)?.ID == this.ID)
                            {
                                var targetAsset = AntRefTable.Get(cl.Target);
                                if (targetAsset is LayoutHierarchyAsset lh)
                                {
                                    FPS = cl.FPS;
                                    hierarchy = lh;
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case ProfileVersion.DeadSpace:
                case ProfileVersion.Battlefield4:
                default:
                    foreach (var asset in refsValues)
                    {
                        if (asset is ClipControllerData cl)
                        {
                            // Fix: Resolves InternalRefs
                            // 3. Cache dictionary lookup OUTSIDE the loop
                            if (AntRefTable.Get(cl.Anim)?.ID == this.ID)
                            {
                                var targetAsset = AntRefTable.Get(cl.Target);
                                if (targetAsset is LayoutHierarchyAsset lh)
                                {
                                    FPS = cl.FPS;
                                    hierarchy = lh;
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }

            if (hierarchy == null)
            {
                App.Logger.LogError($"[AnimationAsset] Could not find a ClipController pointing to '{Name}'. Export will be empty.");
                return new Dictionary<string, BoneChannelType>();
            }

            // 4. Calculate exact capacity for the Dictionary to prevent dynamic re-hashing
            int initialCapacity = 0;
            if (hierarchy.LayoutAssets != null)
            {
                for (int i = 0; i < hierarchy.LayoutAssets.Length; i++)
                {
                    AntAsset layoutAsset = AntRefTable.Get(hierarchy.LayoutAssets[i]);
                    if (layoutAsset is LayoutAsset la) initialCapacity += la.Slots.Count;
                    else if (layoutAsset is DeltaTrajLayoutAsset) initialCapacity += 8;
                }
            }

            var channelNames = new Dictionary<string, BoneChannelType>(initialCapacity);

            if (hierarchy.LayoutAssets != null)
            {
                for (int i = 0; i < hierarchy.LayoutAssets.Length; i++)
                {
                    AntAsset layoutAsset = AntRefTable.Get(hierarchy.LayoutAssets[i]);

                    if (layoutAsset is LayoutAsset la)
                    {
                        // Cache the collection and count for faster loop execution
                        var slots = la.Slots;
                        int slotsCount = slots.Count;
                        for (int x = 0; x < slotsCount; x++)
                        {
                            channelNames[slots[x].Name] = slots[x].Type;
                        }
                    }
                    else if (layoutAsset is DeltaTrajLayoutAsset)
                    {
                        for (int x = 0; x < 8; x++) channelNames[x.ToString()] = BoneChannelType.Rotation;
                    }
                }
            }

            // 5. Extract loop invariants into fast local variables
            uint[] data = dof.IndexData;
            int dataLength = data != null ? data.Length : 0;
            var channelNamesList = channelNames.ToArray();
            int channelNamesLength = channelNamesList.Length;

            // 6. Use primitive Arrays instead of List<T> wherever the size is fixed
            string[] channelsArray = null;
            List<string> channelsList = null;
            bool useArray = true;

            // Cross-bank Rig Search for GW2 (removed Bank.Equals restriction)
            if (currentProfile == ProfileVersion.PlantsVsZombiesGardenWarfare2 && dof.rigId == Guid.Empty)
            {
                foreach (var asset in refsValues)
                {
                    // if (asset.Bank != null && asset.Bank.Equals(this.Bank))
                    // We must search ALL loaded banks, especially the Reference Banks.
                    if (asset is RigAsset ri && ri.DofSetLists != null)
                    {
                        for (int j = 0; j < ri.DofSetLists.Length; j++)
                        {
                            AntAsset resolvedAsset = AntRefTable.Get(ri.DofSetLists[j]);
                            if (resolvedAsset is LayoutHierarchyAsset layoutHierarchyAsset)
                            {
                                var found = RecursiveHierarchySearch(layoutHierarchyAsset, hierarchy);
                                if (found != null && found.ID == hierarchy.ID)
                                {
                                    dof.rigId = ri.ID;
                                    break;
                                }
                            }
                        }
                        if (dof.rigId != Guid.Empty) break;
                    }
                }
            }

            if (currentProfile == ProfileVersion.PlantsVsZombiesGardenWarfare2)
            {
                if (dof.rigId == Guid.Empty)
                {
                    App.Logger.LogError($"[AnimationAsset] RigId for DofAsset is missing. GW2 extraction aborted.");
                    return new Dictionary<string, BoneChannelType>();
                }

                RigAsset rig = (RigAsset)AntRefTable.Get(dof.rigId);
                if (rig == null) return new Dictionary<string, BoneChannelType>();

                channelsArray = new string[dataLength];
                ushort[] dofIds = rig.DofIds;
                int dofIdsLength = dofIds?.Length ?? 0;

                var dofIdToIndex = new Dictionary<ushort, int>(dofIdsLength);
                for (int i = 0; i < dofIdsLength; i++) dofIdToIndex[dofIds[i]] = i;

                int validChannelCount = 0;
                for (int i = 0; i < dataLength; i++)
                {
                    // Streamlined type casting and lookups
                    if (dofIdToIndex.TryGetValue((ushort)data[i], out int idx) && idx >= 0 && idx < channelNamesLength)
                    {
                        channelsArray[i] = channelNamesList[idx].Key;
                        validChannelCount++;
                    }
                }

                if (validChannelCount == 0) return new Dictionary<string, BoneChannelType>();
            }
            else if (currentProfile == ProfileVersion.PlantsVsZombiesGardenWarfare || StorageType == StorageType.Overwrite)
            {
                channelsArray = new string[dataLength];
                for (int i = 0; i < dataLength; i++)
                {
                    int channelId = (int)data[i];
                    if (channelId >= 0 && channelId < channelNamesLength)
                    {
                        channelsArray[i] = channelNamesList[channelId].Key;
                    }
                }
            }
            else if (StorageType == StorageType.Append)
            {
                useArray = false;
                channelsList = new List<string>(dataLength);
                var offsets = new Dictionary<int, int>(dataLength / 2);
                int offset = 0;

                for (int i = 0; i < dataLength; i += 2)
                {
                    int appendTo = (int)data[i];
                    int channelId = (int)data[i + 1];

                    offsets[appendTo] = offset++;
                    int targetIndex = offsets[appendTo];

                    string channelKey = string.Empty;
                    if (channelId >= 0 && channelId < channelNamesLength)
                    {
                        channelKey = channelNamesList[channelId].Key;
                    }

                    if (targetIndex <= channelsList.Count) channelsList.Insert(targetIndex, channelKey);
                    else channelsList.Add(channelKey);
                }
            }

            var output = new Dictionary<string, BoneChannelType>();

            // Used by GW1, GW2, and Overwrite
            if (useArray)
            {
                for (int i = 0; i < dataLength; i++)
                {
                    string ch = channelsArray[i];
                    if (!string.IsNullOrEmpty(ch) && channelNames.TryGetValue(ch, out BoneChannelType bct))
                    {
                        output[ch] = bct;
                    }
                }
            }
            else // Used ONLY by Append
            {
                int listCount = channelsList.Count;
                for (int i = 0; i < listCount; i++)
                {
                    string ch = channelsList[i];
                    if (!string.IsNullOrEmpty(ch) && channelNames.TryGetValue(ch, out BoneChannelType bct))
                    {
                        output[ch] = bct;
                    }
                }
            }

            return output;
        }

        public virtual InternalAnimation ConvertToInternal() { return null; }
    }

    public enum BoneChannelType
    {
        None = 0,
        Rotation = 14,
        Position = 2049856663,
        Scale = 2049856454,
    }
}
