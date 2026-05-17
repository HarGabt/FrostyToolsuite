using System;
using System.Collections.Generic;

namespace AssetBankPlugin.Ant
{
    public class LayoutAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }

        public List<Slot> Slots { get; set; }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];

            var slots = data["Slots"] as Dictionary<string, object>[];

            Slots = new List<Slot>();
            foreach (var slot in slots)
            {
                Slots.Add(new Slot()
                {
                    Name = Convert.ToString(slot["Name"]),
                    Type = (BoneChannelType)Convert.ToInt32(slot["Type"])
                });
            }
        }
    }

    public struct Slot
    {
        public string Name;
        public BoneChannelType Type;

        public override string ToString()
        {
            return Name + ", " + Type.ToString();
        }
    }
}
