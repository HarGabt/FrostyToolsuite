using AssetBankPlugin.Extensions;
using FrostySdk.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AssetBankPlugin.GenericData
{
    public class SectionData : Section
    {
        public const string Identifier = "GD.DATA";
        public override Endian Endianness { get; set; }
        public override uint DataSize { get; set; }
        public override uint DataOffset { get; set; }
        public uint IndicesOffset { get; set; }

        public SectionData(NativeReader r, Endian endian)
        {
            Endianness = endian;

            _ = r.ReadSizedString(8);
            DataSize = r.ReadUInt(Endianness);
            IndicesOffset = r.ReadUInt(Endianness);
            DataOffset = (uint)r.BaseStream.Position;
            IndicesOffset += DataOffset;

            r.BaseStream.Position += DataSize - 16;
        }

        public Dictionary<string, object> ReadValues(
            NativeReader r,
            Dictionary<uint, GenericClass> classes,
            uint baseOffset,
            uint type)
        {
            GenericClass cl = classes[type];
            int count = cl.Elements.Count;

            /// Pre size to avoid internal rehashing on every Add.
            var data = new Dictionary<string, object>(count);

            for (int x = 0; x < count; x++)
            {
                GenericField field = cl.Elements[x];
                object fieldData = null;

                /// Seek only when necessary avoids invalidating the FileStream
                /// read ahead buffer on every field when reads are already sequential.
                SeekTo(r, baseOffset + field.Offset);

                switch (field.Type)
                {
                    case "Bool":
                        if (!field.IsArray)
                            fieldData = r.ReadBoolean();
                        break;

                    case "UInt8":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadByte();
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint u8Size, out long u8Pos);
                            SeekTo(r, u8Pos);
                            fieldData = r.ReadByteArray((int)u8Size);
                        }
                        break;

                    case "Int8":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadSByte();
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint i8Size, out long i8Pos);
                            SeekTo(r, i8Pos);
                            fieldData = r.ReadSByteArray((int)i8Size);
                        }
                        break;

                    case "Int16":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadShort(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint i16Size, out long i16Pos);
                            SeekTo(r, i16Pos);
                            fieldData = r.ReadShortArray((int)i16Size, Endianness);
                        }
                        break;

                    case "UInt16":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadUShort(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint u16Size, out long u16Pos);
                            SeekTo(r, u16Pos);
                            fieldData = r.ReadUShortArray((int)u16Size, Endianness);
                        }
                        break;

                    case "Int32":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadInt(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint i32Size, out long i32Pos);
                            SeekTo(r, i32Pos);
                            fieldData = r.ReadIntArray((int)i32Size, Endianness);
                        }
                        break;

                    case "UInt32":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadInt(Endianness); // intentional: Frostbite quirk, preserved as-is
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint u32Size, out long u32Pos);
                            SeekTo(r, u32Pos);
                            fieldData = r.ReadUIntArray((int)u32Size, Endianness);
                        }
                        break;

                    case "Int64":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadLong(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint i64Size, out long i64Pos);
                            SeekTo(r, i64Pos);
                            fieldData = r.ReadLongArray((int)i64Size, Endianness);
                        }
                        break;

                    case "UInt64":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadLong(Endianness); /// preserved as-is
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint u64Size, out long u64Pos);
                            SeekTo(r, u64Pos);
                            fieldData = r.ReadULongArray((int)u64Size, Endianness);
                        }
                        break;

                    case "Float":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadFloat(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint fSize, out long fPos);
                            SeekTo(r, fPos);
                            fieldData = r.ReadSingleArray((int)fSize, Endianness);
                        }
                        break;

                    case "Double":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadDouble(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint dSize, out long dPos);
                            SeekTo(r, dPos);
                            fieldData = r.ReadDoubleArray((int)dSize, Endianness);
                        }
                        break;

                    case "DataRef":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadLong(Endianness); /// preserved as-is
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint drSize, out long drPos);
                            SeekTo(r, drPos);
                            fieldData = r.ReadGuidArray((int)drSize, Endianness);
                        }
                        break;

                    case "String":
                        if (!field.IsArray) /// preserved as-is
                        {
                            ReadArrayHeader(r, out uint strSize, out long strPos);
                            SeekTo(r, strPos);
                            fieldData = r.ReadSizedString((int)strSize);
                        }
                        break;

                    case "Guid":
                        if (!field.IsArray)
                        {
                            fieldData = r.ReadGuid(Endianness);
                        }
                        else
                        {
                            ReadArrayHeader(r, out uint gSize, out long gPos);
                            SeekTo(r, gPos);
                            fieldData = r.ReadGuidArray((int)gSize, Endianness);
                        }
                        break;

                    default:
                        if (!field.IsArray)
                        {
                            /// Nested struct; the fields absolute position is already the base
                            fieldData = ReadValues(r, classes, (uint)(field.Offset + baseOffset), field.TypeHash);
                        }
                        else
                        {
                            /// Array of nested structs.
                            ReadArrayHeader(r, out uint arrSize, out long arrDataPos);

                            var arr = new Dictionary<string, object>[arrSize];

                            uint alignedSize = GetAlignedSize(field.Size, field.Alignment);

                            for (uint i = 0; i < arrSize; i++)
                            {
                                arr[i] = ReadValues(
                                    r,
                                    classes,
                                    (uint)(arrDataPos + alignedSize * i),
                                    field.TypeHash);
                            }

                            fieldData = arr;
                        }
                        break;
                }

                data[field.Name] = fieldData;
            }

            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadArrayHeader(NativeReader r, out uint size, out long dataPosition)
        {
            _ = r.ReadUInt(Endianness);
            size = r.ReadUInt(Endianness);
            dataPosition = DataOffset + r.ReadLong(Endianness);
        }

        /// Conditionally seeks the stream only when it is not already at <paramref name="position"/>.
        /// This prevents unnecessary invalidation of the FileStream internal read ahead buffer
        /// on sequential field reads, which is the primary cause of I/O thrashing in hot parse loops.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SeekTo(NativeReader r, long position)
        {
            if (r.BaseStream.Position != position)
                r.BaseStream.Position = position;
        }

        /// Gets the total amount of bytes a block would need if it were to be aligned by <paramref name="alignBy"/>.
        private static uint GetAlignedSize(uint size, uint alignBy)
        {
            if (size % alignBy != 0)
                size += alignBy - (size % alignBy);
            return size;
        }

        public override string ToString()
        {
            return $"GD.DATA [{Endianness}-Endian, Offset {DataOffset}, DataSize {DataSize}]";
        }
    }
}
