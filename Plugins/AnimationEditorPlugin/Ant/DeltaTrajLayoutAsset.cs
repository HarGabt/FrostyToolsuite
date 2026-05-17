using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBankPlugin.Ant
{
    public class DeltaTrajLayoutAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];
        }
    }
}
