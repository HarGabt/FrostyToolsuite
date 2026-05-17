using AssetBankPlugin.Ant;
using AssetBankPlugin.Enums;
using FrostySdk;
using FrostySdk.IO;
using System;
using System.Collections.Generic;

namespace AssetBankPlugin.GenericData
{
    public class Bank
    {
        // Not an enum because it differs between games.
        public uint PackagingType { get; set; }
        // Raw Sections
        public List<Section> Sections { get; set; } = new List<Section>();
        // Deserialized Sections
        public Dictionary<uint, GenericClass> Classes { get; set; } = new Dictionary<uint, GenericClass>();
        public Dictionary<string, Guid> DataNames { get; set; } = new Dictionary<string, Guid>();

        public Bank(NativeReader r, int bundleId)
        {
            PackagingType = r.ReadUInt(Endian.Big);

            switch ((ProfileVersion)ProfilesLibrary.DataVersion)
            {
                case ProfileVersion.PlantsVsZombiesGardenWarfare2:
                case ProfileVersion.PlantsVsZombiesGardenWarfare:
                case ProfileVersion.Battlefield4:
                case ProfileVersion.Battlefield1:
                case ProfileVersion.DeadSpace:
                    {
                        // AnimationSets have special AntRef mappings (Guid followed by AntRefId).
                        if (PackagingType == 3)
                        {
                            r.BaseStream.Position = 56;
                            uint antRefMapCount = r.ReadUInt(Endian.Big) / 20;

                            for (int i = 0; i < antRefMapCount; i++)
                            {
                                Guid a = r.ReadGuid();
                                byte[] bytes = new byte[16];
                                bytes[0] = r.ReadByte();
                                bytes[1] = r.ReadByte();
                                bytes[2] = r.ReadByte();
                                bytes[3] = r.ReadByte();

                                Guid b = new Guid(bytes);
                                AntRefTable.InternalRefs[a] = b;
                                Cache.AntRefMap[a] = b;
                            }
                            r.BaseStream.Position = 4;
                        }
                    }
                    break;
            }

            uint headerStart = (uint)r.BaseStream.Position;
            uint headerSize;

            // Some AntPackages seem to have no header. In that case, jump directly to reading the sections.
            string str = r.ReadSizedString(3);
            bool hasHeader = str != "GD.";
            r.BaseStream.Position -= 3;
            if (hasHeader)
                headerSize = r.ReadUInt(Endian.Big);
            else
                headerSize = 0;

            r.BaseStream.Position = headerStart + headerSize;

            // Read all sections.
            while (r.BaseStream.Position < r.BaseStream.Length)
            {
                Sections.Add(Section.ReadSection(r));
            }

            // Get the data out of all sections.
            // Theoretically there should only be one STRM and one REFL section but if we have multiple, just take the last one.
            for (int i = 0; i < Sections.Count; i++)
            {
                var section = Sections[i];
                if (section is SectionStrm strmSection)
                {
                    
                }
                else if (section is SectionRefl reflSection)
                {
                    Classes = reflSection.Classes;
                }
                else if (section is SectionData dataSection)
                {
                    var asset = AntAsset.Deserialize(r, dataSection, Classes, this);
                    if (asset != null)
                    {
                        int index = 0;
                        string name = asset.Name;
                        while (DataNames.ContainsKey(name))
                        {
                            name = asset.Name + " [" + index + "]";
                            index++;
                        }
                        DataNames.Add(name, asset.ID);
                        Cache.AntStateBundleIndices[asset.ID] = bundleId;
                    }
                }
            }
        }
    }
}
