using AssetBankPlugin.Enums;
using FrostySdk;
using System;
using System.Collections.Generic;

namespace AssetBankPlugin.Ant
{
    public class ChannelToDofAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }

        public StorageType StorageType { get; set; } = StorageType.Overwrite;
        public uint[] IndexData { get; set; }
        public Guid rigId { get; set; }
        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];

            switch ((ProfileVersion)ProfilesLibrary.DataVersion)
            {


                case ProfileVersion.Battlefield1:
                    {
                        byte[] dofIds = (byte[])data["IndexData"];
                        IndexData = Array.ConvertAll(dofIds, val => checked((uint)val));
                    }
                    break;
                case ProfileVersion.PlantsVsZombiesGardenWarfare:
                    {
                        StorageType = (StorageType)Convert.ToInt32(data["StorageType"]);
                        byte[] dofIds = (byte[])data["IndexData"];
                        if (dofIds.Length > 256)
                        {
                            ushort[] ushortDofIds = new ushort[dofIds.Length / 2];
                            for (int i = 0; i < dofIds.Length / 2; i++)
                            {
                                ushortDofIds[i] = (ushort)((dofIds[i * 2] << 8) | dofIds[i * 2 + 1]);
                            }

                            IndexData = Array.ConvertAll(ushortDofIds, val => checked((uint)val));
                        }
                        else
                        {
                            IndexData = Array.ConvertAll(dofIds, val => checked((uint)val));
                        }

                    }
                    break;
                case ProfileVersion.DeadSpace:
                case ProfileVersion.Battlefield4:
                    {
                        StorageType = (StorageType)Convert.ToInt32(data["StorageType"]);
                        var dofIds = (byte[])data["IndexData"];
                        IndexData = Array.ConvertAll(dofIds, val => checked((uint)val));
                    }
                    break;
                case ProfileVersion.PlantsVsZombiesGardenWarfare2:
                    {
                        UInt16[] dofIds = (UInt16[])data["DofIds"];
                        IndexData = Array.ConvertAll(dofIds, val => checked((uint)val));
                    }
                    break;

                default:
                    {
                        // Best-effort fallback for unrecognised profiles:
                        // Try BF4-style StorageType + byte[] IndexData first, then GW2-style UInt16[] DofIds.
                        if (data.TryGetValue("StorageType", out object st))
                            StorageType = (StorageType)Convert.ToInt32(st);

                        if (data.TryGetValue("IndexData", out object idRaw) && idRaw is byte[] idBytes)
                            IndexData = Array.ConvertAll(idBytes, val => checked((uint)val));
                        else if (data.TryGetValue("DofIds", out object dofRaw) && dofRaw is UInt16[] dofIds)
                            IndexData = Array.ConvertAll(dofIds, val => checked((uint)val));
                        else
                            IndexData = Array.Empty<uint>();
                    }
                    break;
            }
        }
        }
}
