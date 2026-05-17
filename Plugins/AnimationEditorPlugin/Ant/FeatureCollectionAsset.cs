using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBankPlugin.Ant
{
    public class FeatureCollectionAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }
        public Guid[] FeatureAssets { get; set; }
        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];
            FeatureAssets = (Guid[])data["FeatureAssets"];
            //FeatureCollection = (Guid)data["FeatureCollection"];
            //Skeleton = (Guid)data["Skeleton"];
            //DofSetLists = (Guid[])data["DofSetLists"];
            //DefaultVector3Values = data["DefaultVector3Values"];
            //DefaultVector4Values = data["DefaultVector4Values"];
            

        }
    }
}
