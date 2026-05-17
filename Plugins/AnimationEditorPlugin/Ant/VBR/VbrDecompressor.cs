using AssetBankPlugin.Export;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using FrostySdk;

namespace AssetBankPlugin.Ant
{
    public partial class VbrAnimationAsset : AnimationAsset
    {
        // DCT coefficient matrix (8x8) – unchanged
        public static float[,] DctCoeffs = new float[8, 8] {
            {  0.12500000f,  0.24519631f,  0.23096988f,  0.20786740f,  0.17677669f,  0.13889255f,  0.09567086f,  0.04877256f },
            {  0.12500000f,  0.20786740f,  0.09567086f, -0.04877258f, -0.17677669f, -0.24519633f, -0.23096988f, -0.13889250f },
            {  0.12500000f,  0.13889255f, -0.09567088f, -0.24519633f, -0.17677666f,  0.04877260f,  0.23096989f,  0.20786734f },
            {  0.12500000f,  0.04877256f, -0.23096991f, -0.13889250f,  0.17677675f,  0.20786734f, -0.09567098f, -0.24519630f },
            {  0.12500000f, -0.04877258f, -0.23096988f,  0.13889261f,  0.17677669f, -0.20786744f, -0.09567074f,  0.24519636f },
            {  0.12500000f, -0.13889259f, -0.09567078f,  0.24519631f, -0.17677681f, -0.04877255f,  0.23096983f, -0.20786740f },
            {  0.12500000f, -0.20786741f,  0.09567090f,  0.04877252f, -0.17677663f,  0.24519633f, -0.23096994f,  0.13889278f },
            {  0.12500000f, -0.24519633f,  0.23096989f, -0.20786744f,  0.17677671f, -0.13889271f,  0.09567098f, -0.04877289f },
        };

        // Pre‑computed log values for coefficients 0..7 (ln(k+2))
        private static readonly float[] LogVals = new float[8] {
            0.693147f,  // ln(2)
            1.098612f,  // ln(3)
            1.386294f,  // ln(4)
            1.609438f,  // ln(5)
            1.791759f,  // ln(6)
            1.945910f,  // ln(7)
            2.079442f,  // ln(8)
            2.197225f   // ln(9)
        };

        // Per‑block quantization parameters (read from the 3 header bytes)
        private (float rot, float trj, float tra)[] blockQuantParams;

        // Helper: compute quantisation scale for a given coefficient index
        private float ComputeQuantScale(int coeffIdx, float scale, float currentDct)
        {
            return (scale * LogVals[coeffIdx] + 1.0f) * currentDct / 32768.0f;
        }
        // Modified UnpackVec to take pre-computed delta and minus ranges
        private float UnpackVecWithQuant(short[] values, int coeffIdx, float[] quantScales, float delta, float minus)
        {
            float result = 0.5f;

            for (int i = 0; i < 8; i++)
            {
                result += (float)values[i] * DctCoeffs[coeffIdx, i] * quantScales[i];
            }

            return result * delta + minus;
        }

        static byte ReverseBits(byte b)
        {
            b = (byte)((b >> 4) | (b << 4));
            b = (byte)(((b & 0xCC) >> 2) | ((b & 0x33) << 2));
            b = (byte)(((b & 0xAA) >> 1) | ((b & 0x55) << 1));
            return b;
        }

        public static uint ReverseBits(uint value, int bitLength)
        {
            uint result = 0;
            for (int i = 0; i < bitLength; i++)
            {
                result = (result << 1) | (value & 1);
                value >>= 1;
            }
            return result;
        }

        public List<Vector4> Decompress()
        {
            var channelCount = (QuaternionCount * 4) + (Vector3Count * 3) + NumFloat;
            var DofCount = (QuaternionCount) + (Vector3Count) + NumFloat;
            var constDofCount = (ConstQuaternionCount * 4) + (ConstVector3Count * 3) + ConstFloatCount;

            for (var i = 0; i < FrameBlockSize; i++)
            {
                offset += FrameBlockSizes[i];
            }
            offset = Data.Length - offset;
            CatchAllBitCount = 15;
            byte[] fData = new byte[Data.Length - offset];
            byte[] MetaData = new byte[offset];
            byte[] numbitsByte = new byte[channelCount * 4];
            Array.Copy(Data, offset, fData, 0, fData.Length);
            Array.Copy(Data, 0, MetaData, 0, offset);

            PaletteIndexes = new byte[constDofCount];
            Array.Copy(MetaData, PaletteIndexes, constDofCount);

            ConstChanMap = new byte[ConstChanMapSize];
            Array.Copy(MetaData, constDofCount, ConstChanMap, 0, ConstChanMapSize);

            offset = constDofCount + ConstChanMapSize;
            Array.Copy(MetaData, offset, numbitsByte, 0, numbitsByte.Length);

            VectorOffsets = new byte[VectorOffsetSize + FloatOffsetSize];
            Array.Copy(MetaData, constDofCount + ConstChanMapSize + channelCount * 4, VectorOffsets, 0, VectorOffsets.Length);

            byte[] numbitsNibble = new byte[numbitsByte.Length * 2];
            for (int i = 0; i < numbitsByte.Length; i++)
            {
                numbitsNibble[i * 2] = (byte)((numbitsByte[i] >> 4) & 0x0F);
                numbitsNibble[i * 2 + 1] = (byte)(numbitsByte[i] & 0x0F);
            }

            byte[][] frameBlockData = new byte[FrameBlockSize][];
            blockQuantParams = new (float rot, float trj, float tra)[FrameBlockSize];
            offset = 0;
            for (var i = 0; i < FrameBlockSize; i++)
            {
                frameBlockData[i] = new byte[FrameBlockSizes[i]];
                Array.Copy(fData, offset, frameBlockData[i], 0, FrameBlockSizes[i]);

                float rotTable = (float)frameBlockData[i][0];
                float trjTable = (float)frameBlockData[i][1];
                float traTable = (float)frameBlockData[i][2];
                blockQuantParams[i] = (rotTable, trjTable, traTable);

                byte[] blockWithoutHeader = new byte[FrameBlockSizes[i] - 3];
                Array.Copy(frameBlockData[i], 3, blockWithoutHeader, 0, blockWithoutHeader.Length);
                Array.Reverse(blockWithoutHeader);

                for (int j = 0; j < blockWithoutHeader.Length; j++)
                {
                    blockWithoutHeader[j] = ReverseBits(blockWithoutHeader[j]);
                }
                frameBlockData[i] = blockWithoutHeader;

                offset += FrameBlockSizes[i];
            }

            var blocks = new List<short[]>(((NumKeys + 7) / 8) * channelCount);
            try
            {
                for (var blockFrame = 0; blockFrame < (NumKeys + 7) / 8; blockFrame++)
                {
                    var r = new BitReader(new MemoryStream(frameBlockData[blockFrame]), 8, FrostySdk.IO.Endian.Little);
                    for (var chanIdx = 0; chanIdx < channelCount; chanIdx++)
                    {
                        short[] block = new short[8];
                        int[] numbitsOrdered = new int[8];
                        int numbitIndex = chanIdx * 8;

                        numbitsOrdered[0] = numbitsNibble[numbitIndex + 1];
                        numbitsOrdered[1] = numbitsNibble[numbitIndex];
                        numbitsOrdered[2] = numbitsNibble[numbitIndex + 3];
                        numbitsOrdered[3] = numbitsNibble[numbitIndex + 2];
                        numbitsOrdered[4] = numbitsNibble[numbitIndex + 5];
                        numbitsOrdered[5] = numbitsNibble[numbitIndex + 4];
                        numbitsOrdered[6] = numbitsNibble[numbitIndex + 7];
                        numbitsOrdered[7] = numbitsNibble[numbitIndex + 6];

                        int[] presence = new int[8];
                        int[] sign = new int[8];

                        for (int j = 0; j < 8; j++)
                        {
                            if (numbitsOrdered[j] > 0) presence[j] = (int)r.ReadUIntHigh(1);
                        }

                        for (int j = 0; j < 8; j++)
                        {
                            if (presence[j] > 0 && numbitsOrdered[j] > 0) sign[j] = (int)r.ReadUIntHigh(1);
                        }

                        for (int j = 0; j < 8; j++)
                        {
                            if (presence[j] > 0)
                            {
                                uint readValue = (uint)r.ReadUIntHigh(numbitsOrdered[j]);
                                short value = (short)ReverseBits(readValue, numbitsOrdered[j]);
                                if (sign[j] < 1) value = (short)-value;
                                block[j] = value;
                            }
                        }
                        blocks.Add(block);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {this.Name}: {ex.Message}");
            }

            var blockRotQuant = new float[FrameBlockSize][];
            var blockTrjQuant = new float[FrameBlockSize][];
            var blockTraQuant = new float[FrameBlockSize][];
            float currentDct = (Dct <= 0f) ? 4.0f : Dct;

            for (int b = 0; b < FrameBlockSize; b++)
            {
                var (rotTable, trjTable, traTable) = blockQuantParams[b];
                float rotScale = (rotTable + 1.0f) * 0.2f * currentDct;
                float trjScale = (trjTable + 1.0f) * 0.2f * currentDct;
                float traScale = (traTable + 1.0f) * 0.2f * currentDct;

                blockRotQuant[b] = new float[8];
                blockTrjQuant[b] = new float[8];
                blockTraQuant[b] = new float[8];

                for (int c = 0; c < 8; c++)
                {
                    blockRotQuant[b][c] = ComputeQuantScale(c, rotScale, currentDct);
                    blockTrjQuant[b][c] = ComputeQuantScale(c, trjScale, currentDct);
                    blockTraQuant[b][c] = ComputeQuantScale(c, traScale, currentDct);
                }
            }

            var result = new List<float>(NumKeys * channelCount);
            int framesLeft = NumKeys % 8;
            int lastBlockStart = NumKeys - framesLeft;

            for (var frame = 0; frame < NumKeys; frame++)
            {
                var blockIdx = frame / 8;
                int coeffIdx = (frame >= lastBlockStart && frame > 7) ? frame - (NumKeys - 8) : frame % 8;
                float[] rotQuantForBlock = blockRotQuant[blockIdx];
                float[] trjQuantForBlock = blockTrjQuant[blockIdx];
                float[] traQuantForBlock = blockTraQuant[blockIdx];

                for (var chanIdx = 0; chanIdx < channelCount; chanIdx++)
                {
                    var dataIdx = blockIdx * channelCount + chanIdx;
                    if (dataIdx >= blocks.Count) break;
                    short[] blockc = blocks[dataIdx];

                    float[] quantScales;
                    float delta;
                    float minus;

                    int quatComponents = QuaternionCount * 4;
                    int trajStart = quatComponents;
                    int trajEnd = trajStart + 3;
                    int vec3End = trajStart + Vector3Count * 3;

                    if (chanIdx < quatComponents)
                    {
                        quantScales = rotQuantForBlock;
                        delta = QuatMax - QuatMin;
                        minus = QuatMin;
                    }
                    else if (chanIdx < trajEnd)
                    {
                        quantScales = trjQuantForBlock;
                        delta = TrajMax - TrajMin;
                        minus = TrajMin;
                    }
                    else if (chanIdx < vec3End)
                    {
                        quantScales = traQuantForBlock;
                        delta = Vec3Max - Vec3Min;
                        minus = Vec3Min;
                    }
                    else
                    {
                        quantScales = traQuantForBlock;
                        delta = FloatMax - FloatMin;
                        minus = FloatMin;
                    }
                    result.Add(UnpackVecWithQuant(blockc, coeffIdx, quantScales, delta, minus));
                }
            }

            // --- Simple Curves Pass (Both Vector and Float) ---
            void ApplyCurveSet(int startOffset, float rawScale, int startChannel)
            {
                if (startOffset >= VectorOffsets.Length || rawScale == 0f) return;

                // The division by 127.0f must happen here to normalize the sbyte range
                float normalizedScale = rawScale / 127.0f;
                int vOffset = startOffset;
                int numCurves = VectorOffsets[vOffset++];
                int cppChannel = startChannel;

                for (int i = 0; i < numCurves && vOffset < VectorOffsets.Length; i++)
                {
                    int skip = VectorOffsets[vOffset++];
                    cppChannel += skip;

                    int pointCount = VectorOffsets[vOffset++];
                    if (pointCount == 0) { cppChannel++; continue; }

                    int csharpChannel = -1;
                    int vec3RegionStart = QuaternionCount * 4;
                    int floatRegionStart = vec3RegionStart + Vector3Count * 4;

                    if (cppChannel >= floatRegionStart)
                    {
                        csharpChannel = QuaternionCount * 4 + Vector3Count * 3 + (cppChannel - floatRegionStart);
                    }
                    else if (cppChannel >= vec3RegionStart)
                    {
                        int vecIdx = (cppChannel - vec3RegionStart) / 4;
                        int compIdx = (cppChannel - vec3RegionStart) % 4;
                        if (compIdx < 3) csharpChannel = QuaternionCount * 4 + vecIdx * 3 + compIdx;
                    }

                    sbyte currentYInt = unchecked((sbyte)VectorOffsets[vOffset++]);
                    float currentY = currentYInt * normalizedScale;
                    int currentFrame = 0;

                    for (int p = 0; p < pointCount - 1 && vOffset < VectorOffsets.Length; p++)
                    {
                        int spanBlocks = VectorOffsets[vOffset++];
                        sbyte nextYInt = unchecked((sbyte)VectorOffsets[vOffset++]);
                        float nextY = nextYInt * normalizedScale;
                        int spanFrames = spanBlocks * 8;

                        if (csharpChannel != -1)
                        {
                            for (int f = 0; f < spanFrames; f++)
                            {
                                int frameIndex = currentFrame + f;
                                if (frameIndex >= NumKeys) break;
                                float t = (float)f / spanFrames;
                                int resultIdx = frameIndex * channelCount + csharpChannel;
                                if (resultIdx < result.Count) result[resultIdx] += currentY + (nextY - currentY) * t;
                            }
                        }
                        currentFrame += spanFrames;
                        currentY = nextY;
                    }

                    if (csharpChannel != -1)
                    {
                        for (int frameIndex = currentFrame; frameIndex < NumKeys; frameIndex++)
                        {
                            int resultIdx = frameIndex * channelCount + csharpChannel;
                            if (resultIdx < result.Count) result[resultIdx] += currentY;
                        }
                    }
                }
            }

            // Apply curves using the normalized scale logic
            ApplyCurveSet(0, VectorOffsetScale, QuaternionCount * 4);
            ApplyCurveSet(VectorOffsetSize, FloatOffsetScale, QuaternionCount * 4 + Vector3Count * 4);

            // --- Pack into Vector4 list ---
            List<Vector4> resultDof = new List<Vector4>(NumKeys * DofCount);
            for (int frame = 0; frame < NumKeys; frame++)
            {
                for (int dofidx = 0; dofidx < QuaternionCount; dofidx++)
                {
                    var resultIdx = dofidx * 4 + (frame * channelCount);
                    if (resultIdx + 3 < result.Count) resultDof.Add(new Vector4(result[resultIdx], result[resultIdx + 1], result[resultIdx + 2], result[resultIdx + 3]));
                    else resultDof.Add(new Vector4(0.0f));
                }
                for (int dofidx = 0; dofidx < Vector3Count; dofidx++)
                {
                    var resultIdx = (dofidx * 3) + (frame * channelCount) + (QuaternionCount * 4);
                    if (resultIdx + 2 < result.Count) resultDof.Add(new Vector4(result[resultIdx], result[resultIdx + 1], result[resultIdx + 2], 0.0f));
                    else resultDof.Add(new Vector4(0.0f));
                }
                for (int dofidx = 0; dofidx < NumFloat; dofidx++)
                {
                    var resultIdx = (QuaternionCount * 4) + (Vector3Count * 3) + dofidx + (frame * channelCount);
                    if (resultIdx < result.Count) resultDof.Add(new Vector4(result[resultIdx], 0, 0, 0));
                    else resultDof.Add(new Vector4(0.0f));
                }
            }
            return resultDof;
        }
    }
}