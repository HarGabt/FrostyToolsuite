using FrostySdk.IO;

namespace AssetBankPlugin.GenericData
{
    public class SectionStrm : Section
    {
        public const string Identifier = "GD.STRM";
        public override Endian Endianness { get; set; }
        public override uint DataSize { get; set; }
        public override uint DataOffset { get; set; }

        public SectionStrm(NativeReader r, Endian endian)
        {
            Endianness = endian;

            DataOffset = (uint)r.BaseStream.Position;
            _ = r.ReadSizedString(8);
            DataSize = r.ReadUInt(Endianness);
            _ = r.ReadUInt(Endianness); // Unknown + irrelevant
        }

        public override string ToString()
        {
            return $"GD.STRM [{Endianness}-Endian, Offset {DataOffset}, DataSize {DataSize}]";
        }
    }
}
