using AssetBankPlugin.Enums;
using AssetBankPlugin.GenericData;
using FrostySdk.IO;
using System;
using System.Runtime.InteropServices;

namespace AssetBankPlugin.Extensions
{
    public static class NativeReaderExtensions
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatUnion
        {
            [FieldOffset(0)] public uint UIntValue;
            [FieldOffset(0)] public float FloatValue;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct DoubleUnion
        {
            [FieldOffset(0)] public ulong ULongValue;
            [FieldOffset(0)] public double DoubleValue;
        }

        public static ReflLayout ReadReflLayoutEntry(this NativeReader r, Endian bigEndian, out int fieldSize)
        {
            var layout = new ReflLayout();

            layout.mMinSlot = r.ReadInt(bigEndian);
            layout.mMaxSlot = r.ReadInt(bigEndian);

            fieldSize = (layout.mMinSlot * -1) + layout.mMaxSlot + 1;

            layout.mDataSize = r.ReadUInt(bigEndian);
            layout.mAlignment = r.ReadUInt(bigEndian);
            layout.mStringTableOffset = r.ReadUInt(bigEndian);
            layout.mStringTableLength = r.ReadUInt(bigEndian);
            layout.mReordered = r.ReadBoolean();
            layout.mNative = r.ReadBoolean();
            _ = r.ReadBytes(2); // Pad
            layout.mHash = r.ReadUInt(bigEndian);

            // Fill data entries.
            layout.mEntries = new ReflEntry[fieldSize];
            for (int j = 0; j < fieldSize; j++)
            {
            LABEL_1:
                var entry = new ReflEntry();
                entry.mLayoutHash = r.ReadUInt(bigEndian);
                entry.mElementSize = r.ReadUInt(bigEndian);
                entry.mOffset = r.ReadUInt(bigEndian);
                entry.mName = r.ReadUInt(bigEndian);
                entry.mCount = r.ReadUShort(bigEndian);
                entry.mFlags = (EFlags)r.ReadUShort(bigEndian);
                entry.mElementAlign = r.ReadUShort(bigEndian);
                entry.mRLE = r.ReadShort(bigEndian);
                entry.mLayout = r.ReadLong(bigEndian);

                if (entry.mElementSize != 0 && entry.mCount != 0)
                {
                    layout.mEntries[j] = entry;
                }
                else
                {
                    fieldSize -= 1;
                    goto LABEL_1;
                }
            }

            r.ReadBytes(1); // Pad
            layout.mName = r.ReadNullTerminatedString();

            layout.mFieldNames = new string[fieldSize];
            for (int j = 0; j < fieldSize; j++)
            {
                layout.mFieldNames[j] = r.ReadNullTerminatedString();
            }

            return layout;
        }

        public static void ReadDataHeader(this NativeReader r, Endian bigEndian, out uint hash, out uint type, out uint offset)
        {
            hash = (uint)r.ReadULong(bigEndian);
            r.ReadBytes(8);
            type = (uint)r.ReadULong(bigEndian);
            r.ReadBytes(4);
            offset = r.ReadUShort(bigEndian);
            r.ReadBytes(2);
        }

        // Array methods , optimized

        public static byte[] ReadByteArray(this NativeReader r, int count)
            => r.ReadBytes(count);

        public static sbyte[] ReadSByteArray(this NativeReader r, int count)
        {
            byte[] raw = r.ReadBytes(count);
            sbyte[] array = new sbyte[count];
            Buffer.BlockCopy(raw, 0, array, 0, count);
            return array;
        }

        public static short[] ReadShortArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 2);
            short[] array = new short[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 2;
                    array[i] = (short)(raw[b] | raw[b + 1] << 8);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 2;
                    array[i] = (short)(raw[b + 1] | raw[b] << 8);
                }
            }

            return array;
        }

        public static ushort[] ReadUShortArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 2);
            ushort[] array = new ushort[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 2;
                    array[i] = (ushort)(raw[b] | raw[b + 1] << 8);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 2;
                    array[i] = (ushort)(raw[b + 1] | raw[b] << 8);
                }
            }

            return array;
        }

        public static int[] ReadIntArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 4);
            int[] array = new int[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 4;
                    array[i] = raw[b] | raw[b + 1] << 8 | raw[b + 2] << 16 | raw[b + 3] << 24;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 4;
                    array[i] = raw[b + 3] | raw[b + 2] << 8 | raw[b + 1] << 16 | raw[b] << 24;
                }
            }

            return array;
        }

        public static uint[] ReadUIntArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 4);
            uint[] array = new uint[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 4;
                    array[i] = (uint)(raw[b] | raw[b + 1] << 8 | raw[b + 2] << 16 | raw[b + 3] << 24);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 4;
                    array[i] = (uint)(raw[b + 3] | raw[b + 2] << 8 | raw[b + 1] << 16 | raw[b] << 24);
                }
            }

            return array;
        }

        public static long[] ReadLongArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 8);
            long[] array = new long[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 8;
                    array[i] =
                        (long)(uint)(raw[b + 4] | raw[b + 5] << 8 | raw[b + 6] << 16 | raw[b + 7] << 24) << 32 |
                        (long)(uint)(raw[b] | raw[b + 1] << 8 | raw[b + 2] << 16 | raw[b + 3] << 24);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 8;
                    array[i] =
                        (long)(uint)(raw[b + 3] | raw[b + 2] << 8 | raw[b + 1] << 16 | raw[b] << 24) << 32 |
                        (long)(uint)(raw[b + 7] | raw[b + 6] << 8 | raw[b + 5] << 16 | raw[b + 4] << 24);
                }
            }

            return array;
        }

        public static ulong[] ReadULongArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 8);
            ulong[] array = new ulong[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 8;
                    array[i] =
                        (ulong)(uint)(raw[b + 4] | raw[b + 5] << 8 | raw[b + 6] << 16 | raw[b + 7] << 24) << 32 |
                        (ulong)(uint)(raw[b] | raw[b + 1] << 8 | raw[b + 2] << 16 | raw[b + 3] << 24);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 8;
                    array[i] =
                        (ulong)(uint)(raw[b + 3] | raw[b + 2] << 8 | raw[b + 1] << 16 | raw[b] << 24) << 32 |
                        (ulong)(uint)(raw[b + 7] | raw[b + 6] << 8 | raw[b + 5] << 16 | raw[b + 4] << 24);
                }
            }

            return array;
        }

        public static float[] ReadSingleArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 4);
            float[] array = new float[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 4;
                    uint bits = (uint)(raw[b] | raw[b + 1] << 8 | raw[b + 2] << 16 | raw[b + 3] << 24);
                    array[i] = new FloatUnion { UIntValue = bits }.FloatValue;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 4;
                    uint bits = (uint)(raw[b + 3] | raw[b + 2] << 8 | raw[b + 1] << 16 | raw[b] << 24);
                    array[i] = new FloatUnion { UIntValue = bits }.FloatValue;
                }
            }

            return array;
        }

        public static double[] ReadDoubleArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 8);
            double[] array = new double[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 8;
                    uint lo = (uint)(raw[b] | raw[b + 1] << 8 | raw[b + 2] << 16 | raw[b + 3] << 24);
                    uint hi = (uint)(raw[b + 4] | raw[b + 5] << 8 | raw[b + 6] << 16 | raw[b + 7] << 24);
                    ulong bits = ((ulong)hi) << 32 | lo;
                    array[i] = new DoubleUnion { ULongValue = bits }.DoubleValue;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 8;
                    uint lo = (uint)(raw[b + 3] | raw[b + 2] << 8 | raw[b + 1] << 16 | raw[b] << 24);
                    uint hi = (uint)(raw[b + 7] | raw[b + 6] << 8 | raw[b + 5] << 16 | raw[b + 4] << 24);
                    ulong bits = ((ulong)hi) << 32 | lo;
                    array[i] = new DoubleUnion { ULongValue = bits }.DoubleValue;
                }
            }

            return array;
        }

        public static Guid[] ReadGuidArray(this NativeReader r, int count, Endian endian)
        {
            byte[] raw = r.ReadBytes(count * 16);
            Guid[] array = new Guid[count];

            if (endian == Endian.Little)
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 16;
                    array[i] = new Guid(new byte[]
                    {
                        raw[b],      raw[b + 1],  raw[b + 2],  raw[b + 3],
                        raw[b + 4],  raw[b + 5],  raw[b + 6],  raw[b + 7],
                        raw[b + 8],  raw[b + 9],  raw[b + 10], raw[b + 11],
                        raw[b + 12], raw[b + 13], raw[b + 14], raw[b + 15]
                    });
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int b = i * 16;
                    array[i] = new Guid(new byte[]
                    {
                        raw[b + 3],  raw[b + 2],  raw[b + 1],  raw[b],
                        raw[b + 5],  raw[b + 4],  raw[b + 7],  raw[b + 6],
                        raw[b + 8],  raw[b + 9],  raw[b + 10], raw[b + 11],
                        raw[b + 12], raw[b + 13], raw[b + 14], raw[b + 15]
                    });
                }
            }

            return array;
        }
    }
}
