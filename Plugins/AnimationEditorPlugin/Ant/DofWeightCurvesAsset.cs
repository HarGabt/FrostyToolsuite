using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBankPlugin.Ant
{
    public class DofWeightCurvesAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }
        public Guid DofSetList { get; set; }
        public Object DofSetCurves { get; set; }
        public byte[] DefaultCurveBytes { get; set; }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];
            DofSetList = (Guid)data["DofSetList"];
            DofSetCurves = data["DofSetCurves"];
            DefaultCurveBytes =(byte[]) data["DefaultCurveBytes"];

            
            

        }
    }
}
