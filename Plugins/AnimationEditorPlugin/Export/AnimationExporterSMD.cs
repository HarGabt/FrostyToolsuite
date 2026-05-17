using System.IO;
using System.Numerics;

namespace AssetBankPlugin.Export
{
    public class AnimationExporterSMD : IAnimationExporter
    {
        public override void Export(InternalAnimation animation, InternalSkeleton skeleton, string path)
        {
            // Start writing to disk.
            Directory.CreateDirectory(path);
            using (var w = new StreamWriter(File.Open(path + "\\" + animation.Name + ".smd", FileMode.Create)))
            {
                w.WriteLine($"// {animation.Name}");
                w.WriteLine($"// Additive: {animation.Additive}");
                w.WriteLine($"// Frame Count: {animation.Frames.Count}");

                // Write version header.
                w.WriteLine("version 1");

                // Export any nodes.
                if (skeleton != null)
                {
                    w.WriteLine("nodes");

                    for (int i = 0; i < skeleton.BoneTransforms.Count; i++)
                    {
                        w.WriteLine($"{i} \"{skeleton.BoneNames[i]}\" {skeleton.BoneParents[i]}");
                    }

                    w.WriteLine("end");

                    w.WriteLine("skeleton\ntime 0");

                    for (int x = 0; x < animation.Frames.Count; x++)
                    {
                        if (animation.Frames.Count >= 2 && x >= 1)
                        {
                            for (int y = 0; y < animation.Frames[x].FrameIndex - animation.Frames[x - 1].FrameIndex; y++)
                            {
                                w.WriteLine($"time {animation.Frames[x - 1].FrameIndex + y + 1}");
                            }
                        }

                        for (int i = 0; i < skeleton.BoneNames.Count; i++)
                        {
                            int rotationIndex = i;
                            int positionIndex = i;
                            if (animation.PositionChannels.Count > 0)
                            {
                                rotationIndex = animation.RotationChannels.IndexOf(skeleton.BoneNames[i]);
                                positionIndex = animation.PositionChannels.IndexOf(skeleton.BoneNames[i]);
                            }
                            var lBone = skeleton.LocalTransforms[i];
                            Vector3 rot = lBone.EulerAngles;
                            Vector3 pos = lBone.Position;

                            if (rotationIndex != -1)
                            {
                                rot = Transform.ToEulerAngles(animation.Frames[x].Rotations[rotationIndex]);
                            }
                            if (positionIndex != -1)
                            {
                                pos = animation.Frames[x].Positions[positionIndex];
                            }

                            w.WriteLine($"{i} {pos.X} {pos.Y} {pos.Z} {rot.X} {rot.Y} {rot.Z}");
                        }
                    }
                    w.WriteLine("end");
                }
            }
        }
    }
}