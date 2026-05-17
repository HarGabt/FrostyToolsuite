using AssetBankPlugin.Enums;
using AssetBankPlugin.Extensions;
using FrostySdk.IO;
using System.Collections.Generic;
using System.Windows.Automation;

namespace AssetBankPlugin.GenericData
{
    public class SectionRefl : Section
    {
        public const string Identifier = "GD.REFL";
        public override Endian Endianness { get; set; }
        public override uint DataSize { get; set; }
        public override uint DataOffset { get; set; }
        public uint IndicesOffset { get; set; }
        public Dictionary<uint, GenericClass> Classes { get; set; } = new Dictionary<uint, GenericClass>();

        public SectionRefl(NativeReader r, Endian endian)
        {
            var startPos = r.BaseStream.Position;
            Endianness = endian;

            _ = r.ReadSizedString(8);
            DataSize = r.ReadUInt(Endianness);
            IndicesOffset = r.ReadUInt(Endianness);
            DataOffset = (uint)r.BaseStream.Position;

            long size = r.ReadLong(endian);
            long[] offsets = r.ReadLongArray((int)size, endian);

            ReflLayout[] gdLayout = new ReflLayout[size];

            // For every class defined in the REFL block.
            for (int i = 0; i < size; i++)
            {
                // Set the stream position to the offset of the current class.
                r.BaseStream.Position = offsets[i] + DataOffset;

                // Read the GenericData layout entry.
                gdLayout[i] = r.ReadReflLayoutEntry(endian, out int fieldSize);

                // Convert this info to our intermediate format for easier use.
                var cl = new GenericClass();
                cl.Name = gdLayout[i].mName;
                cl.Alignment = (int)gdLayout[i].mAlignment;
                cl.Size = (int)gdLayout[i].mDataSize;

                // Loop through all fields of the class.
                for (int j = 0; j < fieldSize; j++)
                {
                    var item = new GenericField();

                    // Set the field values to our intermediate field.
                    // Get the correct name from the string table.
                    var curOffset = r.BaseStream.Position;
                    r.BaseStream.Position = offsets[i] + DataOffset + gdLayout[i].mStringTableOffset + gdLayout[i].mEntries[j].mName;
                    item.Name = r.ReadNullTerminatedString();
                    r.BaseStream.Position = curOffset;

                    item.Offset = (int)gdLayout[i].mEntries[j].mOffset;
                    item.IsArray = gdLayout[i].mEntries[j].mFlags == EFlags.Array;

                    // Get the type of the field by querying the mLayout offset and getting that type.
                    long offset = r.BaseStream.Position;
                    if (gdLayout[i].mEntries[j] != null)
                    {
                        // Set the stream position to the GDLE that we want to read.
                        r.BaseStream.Position = gdLayout[i].mEntries[j].mLayout + DataOffset;
                        // Read the GDLE.
                        var glde = r.ReadReflLayoutEntry(endian, out int _fieldSize);
                        // Get the field's type name.
                        item.Type = glde.mName;
                        item.TypeHash = glde.mHash;
                        item.IsNative = glde.mNative;
                        item.Size = glde.mDataSize;
                        item.Alignment = glde.mAlignment;
                        // Go back to our original stream position.
                        r.BaseStream.Position = offset;
                        // Add the read element.
                        cl.Elements.Add(item);
                    }
                }
                // Finally, add the read class.
                Classes.Add(gdLayout[i].mHash, cl);
            }

            r.Pad(4);

            // Type table
            int typeTableSize = r.ReadInt(endian);
            int[] typeTable = r.ReadIntArray(typeTableSize, endian);

            r.BaseStream.Position = startPos + DataSize;
        }

        public override string ToString()
        {
            return $"GD.REFL [{Endianness}-Endian, Offset {DataOffset}, DataSize {DataSize}, Defines {Classes.Count} Classes]";
        }
    }

    public class ReflLayout
    {
        public int mMinSlot;
        public int mMaxSlot;
        public uint mDataSize;
        public uint mAlignment;
        public uint mStringTableOffset;
        public uint mStringTableLength;
        public bool mReordered;
        public bool mNative;
        public uint mHash;
        public ReflEntry[] mEntries;
        public string mName;
        public string[] mFieldNames;

        public override string ToString()
        {
            return $"Class \"{mName}\", Size {mDataSize}";
        }
    }

    public class ReflEntry
    {
        public uint mLayoutHash;
        public uint mElementSize;
        public uint mOffset;
        public uint mName;
        public ushort mCount;
        public EFlags mFlags;
        public ushort mElementAlign;
        public short mRLE;
        public long mLayout; // Layout = Field Type Offset, relative to the start of the REFL section.
    }
}
