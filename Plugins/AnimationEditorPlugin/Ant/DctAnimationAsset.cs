using AssetBankPlugin.Ant;
using AssetBankPlugin.Export;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace AssetBankPlugin.Ant
{
    public partial class DctAnimationAsset : AnimationAsset
    {
        public ushort[] KeyTimes = new ushort[0];
        public byte[] Data = new byte[0];
        public ushort NumKeys;
        public ushort NumVec3;
        public ushort NumFloat;
        public int DataSize;
        public bool Cycle;

        public ushort NumQuats;
        public ushort NumFloatVec;
        public ushort QuantizeMultBlock;
        public byte QuantizeMultSubblock;
        public byte CatchAllBitCount;
        public byte[] DofTableDescBytes;
        public short[] DeltaBaseX;
        public short[] DeltaBaseY;
        public short[] DeltaBaseZ;
        public short[] DeltaBaseW;
        public ushort[] BitsPerSubblock;

        private List<Vector4> DecompressedData = new List<Vector4>();

        public DctAnimationAsset() { }

        public override void SetData(Dictionary<string, object> data)
        {
            Name = (string)data["__name"];
            ID = (Guid)data["__guid"];
            KeyTimes = data["KeyTimes"] as ushort[];
            Data = data["Data"] as byte[];
            NumKeys = (ushort)data["NumKeys"];
            NumVec3 = (ushort)data["NumVec3"];
            NumFloat = (ushort)data["NumFloat"];
            DataSize = (int)data["DataSize"];
            Cycle = (bool)data["Cycle"];

            NumQuats = (ushort)data["NumQuats"];
            NumFloatVec = (ushort)data["NumFloatVec"];
            QuantizeMultBlock = (ushort)data["QuantizeMultBlock"];
            QuantizeMultSubblock = (byte)data["QuantizeMultSubblock"];
            CatchAllBitCount = (byte)data["CatchAllBitCount"];

            DofTableDescBytes = data["DofTableDescBytes"] as byte[];

            DeltaBaseX = data["DeltaBaseX"] as short[];
            DeltaBaseY = data["DeltaBaseY"] as short[];
            DeltaBaseZ = data["DeltaBaseZ"] as short[];
            DeltaBaseW = data["DeltaBaseW"] as short[];
            BitsPerSubblock = data["BitsPerSubblock"] as ushort[];
            DecompressedData = Decompress();
            base.SetData(data);

            // Decompress the animation.
            
        }

        public override InternalAnimation ConvertToInternal()
        {
            var ret = new InternalAnimation();

            List<string> posChannels = new List<string>();
            List<string> rotChannels = new List<string>();
            List<string> scaleChannels = new List<string>();

            // Get all names.
            foreach (var channel in Channels)
            {
                if (channel.Value == BoneChannelType.Rotation)
                    rotChannels.Add(channel.Key);
                else if (channel.Value == BoneChannelType.Position)
                    posChannels.Add(channel.Key);
                else if (channel.Value == BoneChannelType.Scale)
                    scaleChannels.Add(channel.Key);
            }

            // Assign values to Channels.

            var dofCount = NumQuats + NumVec3 + NumFloatVec;

            for (int i = 0; i < KeyTimes.Length; i++)
            {
                Frame frame = new Frame();

                var rotations = new List<Quaternion>();
                var positions = new List<Vector3>();

                for (int channelIdx = 0; channelIdx < NumQuats; channelIdx++)
                {
                    int pos = (int)(i * dofCount + channelIdx);
                    Vector4 element = DecompressedData[pos];

                    rotations.Add(Quaternion.Normalize(new Quaternion(element.X, element.Y, element.Z, element.W)));
                }
                // We need to differentiate between Scale and Position.
                for (int channelIdx = 0; channelIdx < NumVec3; channelIdx++)
                {
                    if (Channels.ElementAt(NumQuats + channelIdx).Value == BoneChannelType.Position)
                    {
                        int pos = (int)(i * dofCount + NumQuats + channelIdx);
                        Vector4 element = DecompressedData[pos];

                        positions.Add(new Vector3(element.X, element.Y, element.Z));
                    }
                }

                frame.Rotations = rotations;
                frame.Positions = positions;

                ret.Frames.Add(frame);
            }

            for (int i = 0; i < KeyTimes.Length; i++)
            {
                Frame f = ret.Frames[i];
                f.FrameIndex = KeyTimes[i];
                ret.Frames[i] = f;
            }

            for (int r = 0; r < rotChannels.Count; r++)
                rotChannels[r] = rotChannels[r].Replace(".q", "");
            for (int r = 0; r < posChannels.Count; r++)
                posChannels[r] = posChannels[r].Replace(".t", "");
            for (int r = 0; r < scaleChannels.Count; r++)
                scaleChannels[r] = scaleChannels[r].Replace(".s", "");

            ret.Name = Name;
            ret.PositionChannels = posChannels;
            ret.RotationChannels = rotChannels;
            ret.Additive = Additive;
            return ret;
        }
    }
}