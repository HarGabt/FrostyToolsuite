using AssetBankPlugin.Export;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AssetBankPlugin.Ant
{
    public class FrameAnimationAsset : AnimationAsset
    {
        public int FloatCount = 0;
        public int Vec3Count = 0;
        public int QuatCount = 0;
        public float[] Data = new float[0];

        public FrameAnimationAsset() { }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = (string)data["__name"];
            ID = (Guid)data["__guid"];

            if (data["Data"] is List<float> dataList)
                Data = dataList.ToArray();
            else
                Data = (float[])data["Data"];

            FloatCount = Convert.ToInt32(data["FloatCount"]);
            Vec3Count = Convert.ToInt32(data["Vec3Count"]);
            QuatCount = Convert.ToInt32(data["QuatCount"]);

            base.SetData(data);
        }

        public override InternalAnimation ConvertToInternal()
        {
            List<Vector3> positions = new List<Vector3>(Vec3Count);
            List<Quaternion> rotations = new List<Quaternion>(QuatCount);
            List<Vector3> scales = new List<Vector3>(Vec3Count);
            List<string> posChannels = new List<string>(Vec3Count);
            List<string> rotChannels = new List<string>(QuatCount);
            List<string> scaleChannels = new List<string>(Vec3Count);

            int dataIndex = 0;

            float[] d = Data;

            foreach (var channel in Channels)
            {
                string key = channel.Key;

                switch (channel.Value)
                {
                    case BoneChannelType.Rotation:
                        rotChannels.Add(key.Replace(".q", ""));
                        rotations.Add(new Quaternion(d[dataIndex], d[dataIndex + 1],
                                                     d[dataIndex + 2], d[dataIndex + 3]));
                        dataIndex += 4;
                        break;

                    case BoneChannelType.Position:
                        posChannels.Add(key.Replace(".t", ""));
                        positions.Add(new Vector3(d[dataIndex], d[dataIndex + 1], d[dataIndex + 2]));
                        dataIndex += 4;
                        break;

                    case BoneChannelType.Scale:
                        scaleChannels.Add(key.Replace(".s", ""));
                        scales.Add(new Vector3(d[dataIndex], d[dataIndex + 1], d[dataIndex + 2]));
                        dataIndex += 4;
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unhandled channel type {channel.Value} encountered at float index {dataIndex}. " +
                            "Data alignment is compromised.");
                }
            }

            InternalAnimation ret = new InternalAnimation();
            ret.Name = Name;

            var frame = new Frame();
            frame.FrameIndex = 0;
            frame.Positions = positions;
            frame.Rotations = rotations;
            frame.Scales = scales;

            ret.Frames.Add(frame);

            ret.PositionChannels = posChannels;
            ret.RotationChannels = rotChannels;
            ret.ScaleChannels = scaleChannels;
            ret.Additive = Additive;

            return ret;
        }
    }
}
