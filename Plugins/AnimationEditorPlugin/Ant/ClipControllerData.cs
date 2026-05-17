using System;
using System.Collections.Generic;

namespace AssetBankPlugin.Ant
{
    public class ClipControllerData : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }

        public Guid Anim { get; set; }
        public Guid Target { get; set; }
        public Guid ChannelToDofAsset { get; set; }
        public float FPS { get; set; }
        public float FPSScale { get; set; }
        public float TrimOffset { get; set; }
        public float NumTicks { get; set; }
        public float TickOffset { get; set; }
        public float Distance { get; set; }
        public int CodecType { get; set; }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];

            Anim = (Guid)data["Anim"];
            Target = (Guid)data["Target"];
            ChannelToDofAsset = (Guid)data["ChannelToDofAsset"];
            FPS = Convert.ToSingle(data["FPS"]);
            CodecType = Convert.ToInt32(data["CodecType"]);
            if (data.TryGetValue("FPSScale", out object fpsScale)) FPSScale = Convert.ToSingle(fpsScale);
            if (data.TryGetValue("TickOffset", out object tickOffset)) TickOffset = Convert.ToSingle(tickOffset);
            if (data.TryGetValue("NumTicks", out object numTicks)) NumTicks = Convert.ToSingle(numTicks);
            if (data.TryGetValue("TrimOffset", out object trimOffset)) TrimOffset = Convert.ToSingle(trimOffset);
            if (data.TryGetValue("Distance", out object distance)) Distance = Convert.ToSingle(distance);
        }
    }
}
