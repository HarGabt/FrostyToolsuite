using FrostySdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetBankPlugin.Ant
{
    public class ClipControllerAsset : AntAsset
    {
        public override string Name { get; set; }
        public override Guid ID { get; set; }


        public Guid[] Anims { get; set; }

        public Guid Anim { get; set; }
        public Guid Target { get; set; }
        public float NumTicks { get; set; }
        public float TickOffset { get; set; }
        public float FPS { get; set; }
        public float TimeScale { get; set; }
        public float Distance { get; set; }
        public int TrajectoryAnimIndex { get; set; }
        public int Modes { get; set; }
        public Guid TagCollectionSet { get; set; }
        
        
        public override void SetData(Dictionary<string, object> data)
        {
            Name = Convert.ToString(data["__name"]);
            ID = (Guid)data["__guid"];
            if (ProfilesLibrary.IsLoaded(ProfileVersion.PlantsVsZombiesGardenWarfare))
            {
                Anim = (Guid)data["Anim"];
                Target = (Guid)data["Target"];
                NumTicks = Convert.ToSingle(data["NumTicks"]);
                FPS = Convert.ToSingle(data["FPS"]);
                TimeScale = Convert.ToSingle(data["FPSScale"]);
                Distance = Convert.ToSingle(data["Distance"]);
                TrajectoryAnimIndex = Convert.ToInt32(data["DeltaTrajectory"]);
                TagCollectionSet = (Guid)data["TagCollectionSet"];
            }
            else
            {
                Anims = (Guid[])data["Anims"];
                Target = (Guid)data["Target"];
                NumTicks = Convert.ToSingle(data["NumTicks"]);
                TickOffset = Convert.ToSingle(data["TickOffset"]);
                FPS = Convert.ToSingle(data["FPS"]);
                TimeScale = Convert.ToSingle(data["TimeScale"]);
                Distance = Convert.ToSingle(data["Distance"]);
                TrajectoryAnimIndex = Convert.ToInt32(data["TrajectoryAnimIndex"]);
                Modes = Convert.ToInt32(data["Modes"]);
                TagCollectionSet = (Guid)data["TagCollectionSet"];
            }
        }
    }
}
