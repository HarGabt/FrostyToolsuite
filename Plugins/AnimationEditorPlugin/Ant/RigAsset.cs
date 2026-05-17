using FrostySdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBankPlugin.Ant
{
    public class RigAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }
        public Guid FeatureCollection { get; set; }
        public  Guid Skeleton { get; set; }
        public Guid[] DofSetLists { get; set; }
        public Guid[] RigDofSets { get; set; }
        public Object DefaultVector3Values { get; set; }
        public Object DefaultVector4Values { get; set; }
        public UInt16[] DofIds { get; set; }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];

            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare2))
            {
                FeatureCollection = (Guid)data["FeatureCollection"];
                Skeleton = (Guid)data["Skeleton"];
                DofSetLists = (Guid[])data["DofSetLists"];
                RigDofSets = (Guid[])data["RigDofSets"];
                DefaultVector3Values = data["DefaultVector3Values"];
                DefaultVector4Values = data["DefaultVector4Values"];
                DofIds = (UInt16[])data["DofIds"];
            }
            else
            {
                // Dead Space and other Frostbite games: read fields that are present,
                // using TryGetValue to avoid crashes for missing fields.
                if (data.TryGetValue("FeatureCollection", out object fc)) FeatureCollection = (Guid)fc;
                if (data.TryGetValue("Skeleton", out object sk)) Skeleton = (Guid)sk;
                if (data.TryGetValue("DofSetLists", out object dsl)) DofSetLists = (Guid[])dsl;
                if (data.TryGetValue("RigDofSets", out object rds)) RigDofSets = (Guid[])rds;
                if (data.TryGetValue("DefaultVector3Values", out object dv3)) DefaultVector3Values = dv3;
                if (data.TryGetValue("DefaultVector4Values", out object dv4)) DefaultVector4Values = dv4;
                if (data.TryGetValue("DofIds", out object di)) DofIds = (UInt16[])di;
            }
        }
    }
}
