using System.Collections.Generic;
using System.Numerics;

namespace AssetBankPlugin.Export
{
    public class InternalAnimation
    {
        public string Name { get; set; }
        public List<Frame> Frames = new List<Frame>();
        public List<string> RotationChannels = new List<string>();
        public List<string> PositionChannels = new List<string>();
        public List<string> ScaleChannels = new List<string>();
        public bool Additive = false;

    }
    public class Frame
    {
        public int FrameIndex = 0;
        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Scales = new List<Vector3>();
        public List<Quaternion> Rotations = new List<Quaternion>();

        public Frame()
        {

        }
    }
}