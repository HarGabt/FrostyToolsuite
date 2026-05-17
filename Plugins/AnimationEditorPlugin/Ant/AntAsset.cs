using AssetBankPlugin.Extensions;
using AssetBankPlugin.GenericData;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AssetBankPlugin.Ant
{
    public abstract class AntAsset
    {
        public abstract string Name { get; set; }
        public abstract Guid ID { get; set; }

        public Bank Bank { get; set; }

        public abstract void SetData(Dictionary<string, object> data);

        // This stores a compiled, direct delegate to the constructor.
        // It bypasses both Type.GetType string scanning AND Activator.CreateInstance overhead
        private static readonly Dictionary<string, Func<AntAsset>> s_activatorCache = new Dictionary<string, Func<AntAsset>>();

        public static AntAsset Deserialize(NativeReader r, SectionData section, Dictionary<uint, GenericClass> classes, Bank bank)
        {
            r.BaseStream.Position = section.DataOffset;
            r.ReadDataHeader(section.Endianness, out uint hash, out uint type, out uint offset);

            var values = section.ReadValues(r, classes, section.DataOffset + offset, type);

            // Add the base values if class inherits from another class.
            if ((long)values["__base"] != 0)
            {
                r.BaseStream.Position = section.DataOffset + (long)values["__base"];

                r.ReadDataHeader(section.Endianness, out uint base_hash, out uint base_type, out uint base_offset);

                var baseValues = section.ReadValues(r,
                    classes,
                    section.DataOffset + base_offset + Convert.ToUInt32(values["__base"]),
                    base_type);

                foreach (var value in baseValues)
                {
                    if (!values.ContainsKey(value.Key))
                        values.Add(value.Key, value.Value);
                }
            }

            string typeName = classes[type].Name;

            // Grab the compiled constructor from the cache
            if (!s_activatorCache.TryGetValue(typeName, out Func<AntAsset> activator))
            {
                // First time seeing this type = resolve it and compile a constructor delegate
                Type assetType = Type.GetType("AssetBankPlugin.Ant." + typeName);
                if (assetType != null)
                {
                    var newExp = Expression.New(assetType);
                    var lambda = Expression.Lambda<Func<AntAsset>>(newExp);
                    activator = lambda.Compile();
                }
                else
                {
                    activator = null;
                }
                s_activatorCache[typeName] = activator;
            }

            // If activator is not null, invoke the compiled delegate directly.
            if (activator != null)
            {
                AntAsset asset = activator(); // Identical speed to writing 'new Asset()'
                asset.Bank = bank;
                asset.SetData(values);

                AntRefTable.Add(asset);
                return asset;
            }
            else
            {
                return null;
            }
        }
    }
}
