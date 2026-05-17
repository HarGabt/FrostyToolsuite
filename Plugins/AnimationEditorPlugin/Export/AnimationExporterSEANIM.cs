using SELib;
using System.IO;

namespace AssetBankPlugin.Export
{
    public class AnimationExporterSEANIM : IAnimationExporter
    {
        public override void Export(InternalAnimation animation, InternalSkeleton skeleton, string path)
        {
            // Start writing to disk.
            Directory.CreateDirectory(path);
            var s = File.Open(path + "\\" + animation.Name + ".seanim", FileMode.Create);

            SEAnim anim = new SEAnim();

            for (int frame = 0; frame < animation.Frames.Count; frame++)
            {
                for (int i = 0; i < animation.Frames[frame].Positions.Count; i++)
                {
                    System.Numerics.Vector3 pos = animation.Frames[frame].Positions[i];
                    anim.AddTranslationKey(animation.PositionChannels[i], animation.Frames[frame].FrameIndex, pos.X, pos.Y, pos.Z);
                }
                for (int i = 0; i < animation.Frames[frame].Scales.Count; i++)
                {
                    System.Numerics.Vector3 scale = animation.Frames[frame].Scales[i];
                    anim.AddScaleKey(animation.ScaleChannels[i], animation.Frames[frame].FrameIndex, scale.X, scale.Y, scale.Z);
                }
                for (int i = 0; i < animation.Frames[frame].Rotations.Count; i++)
                {
                    System.Numerics.Quaternion rot = animation.Frames[frame].Rotations[i];
                    anim.AddRotationKey(animation.RotationChannels[i], animation.Frames[frame].FrameIndex, rot.X, rot.Y, rot.Z, rot.W);
                }
            }

            anim.AnimType = animation.Additive ? AnimationType.Additive : AnimationType.Absolute;

            anim.Write(s, false);
        }
    }
}