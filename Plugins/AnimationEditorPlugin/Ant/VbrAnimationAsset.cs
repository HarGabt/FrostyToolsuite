using AssetBankPlugin.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AssetBankPlugin.Ant
{
    public partial class VbrAnimationAsset : AnimationAsset
    {
        public byte[] Data = new byte[0];
        public ushort NumKeys;
        public ushort NumVec3;
        public ushort NumFloat;
        public ushort ConstFloatCount;
        public int DataSize;
        public bool Cycle;
        public float QuatMin;
        public float TrajMin;
        public float Vec3Min;
        public float FloatMin;
        public float QuatMax;
        public float TrajMax;
        public float Vec3Max;
        public float FloatMax;
        public float VectorOffsetScale;
        public float FloatOffsetScale;
        public float Dct;
        public ushort NumQuats;
        public ushort NumFloatVec;
        public ushort ConstChanMapSize;
        public ushort QuaternionCount;
        public ushort ConstPaletteSize;
        public ushort Vector3Count;
        public ushort ConstQuaternionCount;
        public ushort ConstVector3Count;
        public ushort VectorOffsetSize;
        public ushort FloatOffsetSize;
        public ushort CodecTypeID;
        public ushort EndFrame2;
        public ushort Flags;
        public int offset;
        public int FrameBlockSize;
        public byte[] PaletteIndexes;
        public byte[] ConstChanMap;
        public byte[] VectorOffsets;
        public byte QuantizeMultSubblock;
        public byte CatchAllBitCount;
        public float[] ConstantPalette;
        public ushort[] FrameBlockSizes;

        public short[] DeltaBaseX;
        public short[] DeltaBaseY;
        public short[] DeltaBaseZ;
        public short[] DeltaBaseW;
        public ushort[] BitsPerSubblock;

        public ushort[] KeyTimes;
        public ushort QuantizeMultBlock;

        private List<Vector4> DecompressedData = new List<Vector4>();
        public VbrAnimationAsset() { }

        public override void SetData(Dictionary<string, object> data)
        {
            ID = (Guid)data["__guid"];
            Name = (string)data["__name"];

            QuatMin = (float)data["QuatMin"];
            TrajMin = (float)data["TrajMin"];
            Vec3Min = (float)data["Vec3Min"];
            FloatMin = (float)data["FloatMin"];
            QuatMax = (float)data["QuatMax"];
            TrajMax = (float)data["TrajMax"];
            Vec3Max = (float)data["Vec3Max"];
            FloatMax = (float)data["FloatMax"];
            VectorOffsetScale = (float)data["VectorOffsetScale"];
            FloatOffsetScale = (float)data["FloatOffsetScale"];
            Dct = (float)data["Dct"];
            QuaternionCount = (ushort)data["QuaternionCount"];
            Vector3Count = (ushort)data["Vector3Count"];
            ConstQuaternionCount = (ushort)data["ConstQuaternionCount"];
            ConstVector3Count = (ushort)data["ConstVector3Count"];
            NumKeys = (ushort)data["NumKeys"];
            ConstChanMapSize = (ushort)data["ConstChanMapSize"];
            ConstPaletteSize = (ushort)data["ConstPaletteSize"];
            VectorOffsetSize = (ushort)data["VectorOffsetSize"];
            FloatOffsetSize = (ushort)data["FloatOffsetSize"];
            Flags = (ushort)data["Flags"];
            ConstantPalette = (float[])data["ConstantPalette"];
            FrameBlockSizes = (ushort[])data["FrameBlockSizes"];
            Data = data["Data"] as byte[];
            FrameBlockSize = FrameBlockSizes.Length;

            if (data.ContainsKey("FloatCount")) NumFloat = Convert.ToUInt16(data["FloatCount"]);
            else if (data.ContainsKey("NumFloat")) NumFloat = Convert.ToUInt16(data["NumFloat"]);
            if (data.ContainsKey("ConstFloatCount")) ConstFloatCount = Convert.ToUInt16(data["ConstFloatCount"]);

            // CRITICAL FIX: Removed DecompressedData = Decompress(); from here!!
            // We only call base.SetData(data) to finish metadata assignment
            base.SetData(data);
        }

        public override InternalAnimation ConvertToInternal()
        {
            // CRITICAL FIX: Lazy Load.
            // If the list is empty, decompress now (on the export thread).
            if (DecompressedData == null || DecompressedData.Count == 0)
            {
                DecompressedData = Decompress();
            }

            var ret = new InternalAnimation();

            List<string> posChannels = new List<string>();
            List<string> rotChannels = new List<string>();
            List<string> scaleChannels = new List<string>();

            var channelList = Channels.ToList();

            foreach (var channel in channelList)
            {
                if (channel.Value == BoneChannelType.Rotation) rotChannels.Add(channel.Key);
                else if (channel.Value == BoneChannelType.Position) posChannels.Add(channel.Key);
                else if (channel.Value == BoneChannelType.Scale) scaleChannels.Add(channel.Key);
            }

            // --- OPTIMIZATION 1: Arrays with -1 Sentinel Values ---
            int[] globalToLocalRot = new int[channelList.Count];
            int[] globalToLocalPos = new int[channelList.Count];
            int[] globalToLocalScale = new int[channelList.Count];

            // Initialize with -1 so missing/invalid channels don't default to bone 0
            for (int i = 0; i < channelList.Count; i++)
            {
                globalToLocalRot[i] = -1;
                globalToLocalPos[i] = -1;
                globalToLocalScale[i] = -1;
            }

            int localRot = 0, localPos = 0, localScale = 0;
            for (int g = 0; g < channelList.Count; g++)
            {
                switch (channelList[g].Value)
                {
                    case BoneChannelType.Rotation: globalToLocalRot[g] = localRot++; break;
                    case BoneChannelType.Position: globalToLocalPos[g] = localPos++; break;
                    case BoneChannelType.Scale: globalToLocalScale[g] = localScale++; break;
                }
            }

            var chanMapOffset = 1;
            var chanMapCount = 0;

            int mapping = ConstChanMap[0];
            int valuesToMap = ConstChanMap[chanMapOffset];
            int[] constQuatMap = new int[ConstQuaternionCount];
            int[] QuatMap = new int[QuaternionCount];
            int[] constVectorMap = new int[ConstVector3Count];
            int[] VectorMap = new int[Vector3Count];
            try
            {
                for (var j = 0; j < ConstQuaternionCount; j++)
                {
                    if (chanMapCount >= valuesToMap)
                    {
                        mapping += ConstChanMap[chanMapOffset + 1];
                        chanMapOffset += 2;
                        valuesToMap += ConstChanMap[chanMapOffset];
                    }
                    constQuatMap[j] = mapping;
                    mapping++;
                    chanMapCount++;
                }
                for (var j = 0; j < ConstVector3Count; j++)
                {
                    if (chanMapCount >= valuesToMap)
                    {
                        mapping += ConstChanMap[chanMapOffset + 1];
                        chanMapOffset += 2;
                        valuesToMap += ConstChanMap[chanMapOffset];
                    }
                    constVectorMap[j] = mapping;
                    mapping++;
                    chanMapCount++;
                }

                // --- OPTIMIZATION 2: HashSets for instant mapping resolution ---
                var constQuatSet = new HashSet<int>(constQuatMap);
                var constVectorSet = new HashSet<int>(constVectorMap);

                var QuatIndex = 0;
                for (var j = 0; j < (QuaternionCount + ConstQuaternionCount); j++)
                {
                    if (!constQuatSet.Contains(j))
                    {
                        QuatMap[QuatIndex] = j;
                        QuatIndex++;
                    }
                }

                var Vector3Index = 0;
                for (var j = ConstQuaternionCount + QuaternionCount; j < ConstQuaternionCount + QuaternionCount + (Vector3Count + ConstVector3Count); j++)
                {
                    if (!constVectorSet.Contains(j))
                    {
                        VectorMap[Vector3Index] = j;
                        Vector3Index++;
                    }
                }

                var dofCount = QuaternionCount + Vector3Count + NumFloat;

                if (NumFloat > 0)
                {
                    Console.WriteLine($"Animation {Name} has {NumFloat} float channels that are currently ignored.");
                }
                // --- OPTIMIZATION 4: Pre-allocate Frames to prevent memory thrashing ---
                ret.Frames = new List<Frame>(NumKeys);

                for (int i = 0; i < NumKeys; i++)
                {
                    Frame frame = new Frame();

                    // --- OPTIMIZATION 3: Using Arrays inside the loop prevents garbage collector stutters ---
                    var rotations = new Quaternion[rotChannels.Count];
                    var positions = new Vector3[posChannels.Count];
                    var scales = new Vector3[scaleChannels.Count];

                    var PaletteIndex = 0;
                    Vector4 qDelta = new Vector4(QuatMax - QuatMin);
                    Vector4 qMin = new Vector4(QuatMin);

                    for (int channelIdx = 0; channelIdx < ConstQuaternionCount; channelIdx++)
                    {
                        int globalIdx = constQuatMap[channelIdx];
                        Vector4 element = new Vector4(ConstantPalette[PaletteIndexes[PaletteIndex]], ConstantPalette[PaletteIndexes[PaletteIndex + 1]], ConstantPalette[PaletteIndexes[PaletteIndex + 2]], ConstantPalette[PaletteIndexes[PaletteIndex + 3]]);
                        element *= qDelta;
                        element += qMin;

                        // Arrays safeguard: checks bounds and checks if bone exists (!= -1)
                        if (globalIdx < channelList.Count)
                        {
                            int localIdx = globalToLocalRot[globalIdx];
                            if (localIdx != -1)
                            {
                                rotations[localIdx] = Quaternion.Normalize(new Quaternion(element.X, element.Y, element.Z, element.W));
                            }
                        }
                        PaletteIndex += 4;
                    }
                    for (int channelIdx = 0; channelIdx < QuaternionCount; channelIdx++)
                    {
                        int globalIdx = QuatMap[channelIdx];
                        int pos = i * dofCount + channelIdx;
                        Vector4 element = DecompressedData[pos];

                        if (globalIdx < channelList.Count)
                        {
                            int localIdx = globalToLocalRot[globalIdx];
                            if (localIdx != -1)
                            {
                                rotations[localIdx] = Quaternion.Normalize(new Quaternion(element.X, element.Y, element.Z, element.W));
                            }
                        }
                    }

                    Vector3 vDelta = new Vector3(Vec3Max - Vec3Min);
                    Vector3 vMin = new Vector3(Vec3Min);
                    for (int channelIdx = 0; channelIdx < ConstVector3Count; channelIdx++)
                    {
                        int globalIdx = constVectorMap[channelIdx];
                        Vector3 element = new Vector3(
                            ConstantPalette[PaletteIndexes[PaletteIndex]],
                            ConstantPalette[PaletteIndexes[PaletteIndex + 1]],
                            ConstantPalette[PaletteIndexes[PaletteIndex + 2]]);

                        element = element * vDelta + vMin;

                        if (globalIdx < channelList.Count)
                        {
                            if (channelList[globalIdx].Value == BoneChannelType.Position)
                            {
                                int localIdx = globalToLocalPos[globalIdx];
                                if (localIdx != -1)
                                {
                                    positions[localIdx] = element;
                                }
                            }
                            else // Scale channel
                            {
                                int localIdx = globalToLocalScale[globalIdx];
                                if (localIdx != -1)
                                {
                                    scales[localIdx] = element;
                                }
                            }
                        }
                        PaletteIndex += 3;
                    }

                    for (int channelIdx = 0; channelIdx < Vector3Count; channelIdx++)
                    {
                        int globalIdx = VectorMap[channelIdx];
                        int pos = i * dofCount + QuaternionCount + channelIdx;
                        Vector4 element = DecompressedData[pos];

                        if (globalIdx < channelList.Count)
                        {
                            if (channelList[globalIdx].Value == BoneChannelType.Position)
                            {
                                int localIdx = globalToLocalPos[globalIdx];
                                if (localIdx != -1)
                                {
                                    positions[localIdx] = new Vector3(element.X, element.Y, element.Z);
                                }
                            }
                            else
                            {
                                int localIdx = globalToLocalScale[globalIdx];
                                if (localIdx != -1)
                                {
                                    scales[localIdx] = new Vector3(element.X, element.Y, element.Z);
                                }
                            }
                        }
                    }

                    frame.Rotations = rotations.ToList();
                    frame.Positions = positions.ToList();
                    frame.Scales = scales.ToList();
                    frame.FrameIndex = i * 3;

                    ret.Frames.Add(frame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {this.Name}: {ex.Message}");
            }

            for (int r = 0; r < rotChannels.Count; r++) rotChannels[r] = rotChannels[r].Replace(".q", "");
            for (int r = 0; r < posChannels.Count; r++) posChannels[r] = posChannels[r].Replace(".t", "");
            for (int r = 0; r < scaleChannels.Count; r++) scaleChannels[r] = scaleChannels[r].Replace(".s", "");

            ret.Name = Name;
            ret.PositionChannels = posChannels;
            ret.RotationChannels = rotChannels;
            ret.ScaleChannels = scaleChannels;
            ret.Additive = Additive;
            return ret;
        }
    }
}
