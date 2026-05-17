using AssetBankPlugin.Export;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AssetBankPlugin.Ant
{
    public class RawAnimationAsset : AnimationAsset
    {
        public int NumKeys;
        public int FloatCount;
        public int Vec3Count;
        public int QuatCount;
        public ushort[] KeyTimes;
        public float[] Data;
        public bool Cycle;

        public RawAnimationAsset() { }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = (string)data["__name"];
            ID = (Guid)data["__guid"];
            NumKeys = Convert.ToInt32(data["NumKeys"]);
            FloatCount = Convert.ToInt32(data["FloatCount"]);
            Vec3Count = Convert.ToInt32(data["Vec3Count"]);
            QuatCount = Convert.ToInt32(data["QuatCount"]);
            KeyTimes = (ushort[])data["KeyTimes"];
            Data = (float[])data["Data"];
            Cycle = (bool)data["Cycle"];

            base.SetData(data);
        }

        public override InternalAnimation ConvertToInternal()
        {
            InternalAnimation ret = new InternalAnimation();

            List<string> posChannels = new List<string>();
            List<string> rotChannels = new List<string>();

            var frame = new Frame();

            // Get all names.
            foreach (var channel in Channels)
            {
                if (channel.Value == BoneChannelType.Rotation)
                    rotChannels.Add(channel.Key.Replace(".q", ""));
                else if (channel.Value == BoneChannelType.Position)
                    posChannels.Add(channel.Key.Replace(".t", ""));
            }

            int dataIndex = 0;

            // For each frame.
            for (int frameIndex = 0; frameIndex < KeyTimes.Length; frameIndex++)
            {
                List<Vector3> positions = new List<Vector3>();
                List<Quaternion> rotations = new List<Quaternion>();

                for (int i = 0; i < QuatCount; i++)
                {
                    rotations.Add(new Quaternion(Data[dataIndex++], Data[dataIndex++], Data[dataIndex++], Data[dataIndex++]));
                }
                for (int i = 0; i < Vec3Count; i++)
                {
                    positions.Add(new Vector3(Data[dataIndex++], Data[dataIndex++], Data[dataIndex++]));
                }

                frame.FrameIndex = KeyTimes[frameIndex];
                frame.Positions = positions;
                frame.Rotations = rotations;
                ret.Frames.Add(frame);
            }

            ret.Name = Name;
            ret.PositionChannels = posChannels;
            ret.RotationChannels = rotChannels;
            ret.Additive = Additive;

            return ret;
        }
    }
}