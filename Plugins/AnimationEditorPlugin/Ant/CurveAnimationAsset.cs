using AssetBankPlugin.Export;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AssetBankPlugin.Ant
{
    public class CurveAnimationAsset : AnimationAsset
    {
        public ushort NumRotations;
        public ushort NumVectors;
        public ushort NumFloats;
        public float[] Values;
        public ushort[] Keys;
        public ushort[] ChannelOffsets;

        public CurveAnimationAsset() { }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = (string)data["__name"];
            ID = (Guid)data["__guid"];
            FPS = (float)data["FPS"];
            NumRotations = (ushort)data["NumRotations"];
            NumVectors = (ushort)data["NumVectors"];
            NumFloats = (ushort)data["NumFloats"];
            Values = (float[])data["Values"];
            Keys = (ushort[])data["Keys"];
            ChannelOffsets = (ushort[])data["ChannelOffsets"];

            base.SetData(data);
        }

        public override InternalAnimation ConvertToInternal()
        {
            InternalAnimation ret = new InternalAnimation();

            List<string> posChannels = new List<string>();
            List<string> rotChannels = new List<string>();

            // Get all names.
            foreach (var channel in Channels)
            {
                if (channel.Value == BoneChannelType.Rotation)
                    rotChannels.Add(channel.Key.Replace(".q", ""));
                else if (channel.Value == BoneChannelType.Position)
                    posChannels.Add(channel.Key.Replace(".t", ""));
            }

            int dataIndex = 0;

            //// For each frame.
            //for (int frameIndex = 0; frameIndex < Keys.Length; frameIndex++)
            //{
            //    var frame = new Frame();
            //
            //    List<Vector3> positions = new List<Vector3>();
            //    List<Quaternion> rotations = new List<Quaternion>();
            //
            //    for (int i = 0; i < NumRotations; i++)
            //    {
            //        rotations.Add(new Quaternion(Values[dataIndex++], Values[dataIndex++], Values[dataIndex++], Values[dataIndex++]));
            //    }
            //    for (int i = 0; i < NumVectors; i++)
            //    {
            //        positions.Add(new Vector3(Values[dataIndex++], Values[dataIndex++], Values[dataIndex++]));
            //    }
            //
            //    frame.FrameIndex = Keys[frameIndex];
            //    frame.Positions = positions;
            //    frame.Rotations = rotations;
            //    ret.Frames.Add(frame);
            //}

            ret.Name = Name;
            ret.PositionChannels = posChannels;
            ret.RotationChannels = rotChannels;
            ret.Additive = Additive;

            return ret;
        }
    }
}