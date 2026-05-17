using FrostySdk.IO;
using System.IO;

namespace AssetBankPlugin.Ant
{

    public class DofTable
    {
        public class BitsPerComponent
        {
            public BitsPerComponent(ushort value)
            {
                Value = value;
            }

            public ushort Value { get; set; }


            public ushort BitsW => (ushort)(Value >> 4 * 0 & 0xF);
            public ushort BitsZ => (ushort)(Value >> 4 * 1 & 0xF);
            public ushort BitsY => (ushort)(Value >> 4 * 2 & 0xF);
            public ushort BitsX => (ushort)(Value >> 4 * 3 & 0xF);

            public ushort SafeBitsW(ushort catchAllBitCount) => BitsW == 0xF ? catchAllBitCount : BitsW;
            public ushort SafeBitsZ(ushort catchAllBitCount) => BitsZ == 0xF ? catchAllBitCount : BitsZ;
            public ushort SafeBitsY(ushort catchAllBitCount) => BitsY == 0xF ? catchAllBitCount : BitsY;
            public ushort SafeBitsX(ushort catchAllBitCount) => BitsX == 0xF ? catchAllBitCount : BitsX;


            public int BitSum => BitsX + BitsY + BitsZ + BitsW;

            public int SafeSum(ushort catchAllBitCount) => SafeBitsX(catchAllBitCount) + SafeBitsY(catchAllBitCount) + SafeBitsZ(catchAllBitCount) + SafeBitsW(catchAllBitCount);
        }
  


        public ushort SubBlockCount { get; set; } = 0;

        public short[] DeltaBase = new short[4];

        public BitsPerComponent[] BitsPerSubBlock = new BitsPerComponent[0];


        public DofTable(ushort p_SubBlockCount)
        {
            SubBlockCount = p_SubBlockCount;
        }

        public DofTable(NativeReader r, ushort subBlockCount)
            : this(subBlockCount)
        {
            Deserialize(r);
        }

        public void Deserialize(NativeReader r)
        {
            DeltaBase = new short[4]
            {
                r.ReadShort(),
                r.ReadShort(),
                r.ReadShort(),
                r.ReadShort(),
            };

            BitsPerSubBlock = new BitsPerComponent[SubBlockCount];

            for (var i = 0; i < SubBlockCount; i++)
                BitsPerSubBlock[i] = new BitsPerComponent(r.ReadUShort());
        }


        public void Deserialize(byte[] data)
        {
            using (var r = new NativeReader(new MemoryStream(data)))
            {
                Deserialize(r);
            }
        }
    }
}