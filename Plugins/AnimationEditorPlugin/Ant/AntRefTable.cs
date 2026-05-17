using Frosty.Core;
using Frosty.Core.Controls;
using System;
using System.Collections.Generic;

namespace AssetBankPlugin.Ant
{
    public class AntRefTable
    {
        public static Dictionary<Guid, Guid> InternalRefs = new Dictionary<Guid, Guid>();
        public static Dictionary<Guid, AntAsset> Refs = new Dictionary<Guid, AntAsset>();

        public static void Add(AntAsset asset)
        {
            Refs[asset.ID] = asset;
        }

        public static AntAsset Get(Guid refId, bool recurse = false)
        {
            // Immediately discard Empty GUIDs to prevent unnecessary cache checks
            if (refId == Guid.Empty)
                return null;

            if (Refs.TryGetValue(refId, out var guid))
            {
                return guid;
            }
            else if (InternalRefs.ContainsKey(refId))
            {
                if (Refs.TryGetValue(InternalRefs[refId], out var internalGuid))
                {
                    return internalGuid;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (recurse)
                {
                    return null;
                }
                else
                {
                    int bundleId;
                    if (Cache.AntStateBundleIndices.ContainsKey(refId))
                    {
                        bundleId = Cache.AntStateBundleIndices[refId];
                    }
                    else if (Cache.AntRefMap.ContainsKey(refId) && Cache.AntStateBundleIndices.ContainsKey(Cache.AntRefMap[refId]))
                    {
                        bundleId = Cache.AntStateBundleIndices[Cache.AntRefMap[refId]];
                    }
                    else
                    {
                        // Removed the FrostyExceptionBox / LogError spam here.
                        // If a garbage GUID (like 0000000d-...) hits this point, we just return null.
                        // If it was a critical missing asset, AnimationAsset.cs will log it specifically anyway.
                        return null;
                    }

                    var bundle = App.AssetManager.GetBundleEntry(bundleId);
                    AntStateAssetDefinition.LoadAntStateFromBundle(bundle);

                    return Get(refId, true);
                }
            }
        }
    }
}
