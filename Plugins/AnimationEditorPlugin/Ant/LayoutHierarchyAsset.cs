using System;
using System.Collections.Generic;

namespace AssetBankPlugin.Ant
{
    public class LayoutHierarchyAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }

        public Guid[] LayoutAssets { get; set; }
        public Guid[] Children { get; set; }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];

            LayoutAssets = data["LayoutAssets"] as Guid[];
            Children = data["Children"] as Guid[];
        }
    }
}
