using FrostySdk.IO;
using System;
using System.IO;

namespace AssetBankPlugin.Ant
{
    public class BitReader : NativeReader
    {
        private readonly int _bitsPerSlice;
        private readonly int _bytesPerSlice;
        private readonly bool _shouldDispose;

        protected byte[] BitBuffer;
        protected int CurrentBitsLeft = 0;

        public Endian Endianness { get; set; }
        protected bool Disposed { get; set; }

        public override long Position => (BaseStream.Position << 3) - (CurrentBitsLeft >> 3);
        public override long Length => BaseStream.Length << 3;

        private bool HasReadHigh { get; set; } = false;
        private bool HasReadLow { get; set; } = false;

        public BitReader(Stream stream, int bitCountPerSlice = 8, Endian endianness = Endian.Big, bool shouldDispose = false) : base(stream)
        {
            if (!stream.CanRead)
                throw new ArgumentException("Stream isn't readable", nameof(stream));

            if ((bitCountPerSlice & 7) != 0)
                throw new ArgumentException("BitReader does not support bit slices that are not 8 bit aligned");

            _bitsPerSlice = bitCountPerSlice;
            _bytesPerSlice = bitCountPerSlice >> 3;
            BitBuffer = new byte[_bytesPerSlice];
            Endianness = endianness;
            _shouldDispose = shouldDispose;
        }

        public ulong ReadUIntLow(int p_BitCount)
        {
            ulong result = 0;
            for (int i = 0; i < p_BitCount; i++)
            {
                result = (result << 1) | (ReadLowBit() ? 1UL : 0UL);
            }
            return result;
        }

        public long ReadIntLow(int p_BitCount)
        {
            ulong s_Result = ReadUIntLow(p_BitCount);
            if ((s_Result & (1UL << (p_BitCount - 1))) != 0)
                return (long)(s_Result | (ulong.MaxValue << p_BitCount));

            return (long)s_Result;
        }

        public bool ReadLowBit()
        {
            if (CurrentBitsLeft == 0) UpdateBits();

            if (HasReadHigh)
                throw new Exception("Trying to read low bit after reading high bit. Will result in data loss. pLz fix!");
            HasReadLow = true;

            // Equivalent to (CurrentBitsLeft - BitsPerSlice) % 8
            int s_BitMask = 1 << ((CurrentBitsLeft - _bitsPerSlice) & 7);

            // CurrentByteIndexLow is (CurrentBitsLeft - 1) / 8
            bool result = (BitBuffer[(CurrentBitsLeft - 1) >> 3] & s_BitMask) != 0;

            CurrentBitsLeft--;
            return result;
        }

        public ulong ReadUIntHigh(int p_BitCount)
        {
            ulong result = 0;
            for (int i = 0; i < p_BitCount; i++)
            {
                result = (result << 1) | (ReadHighBit() ? 1UL : 0UL);
            }
            return result;
        }

        public long ReadIntHigh(int p_BitCount)
        {
            ulong s_Result = ReadUIntHigh(p_BitCount);
            if ((s_Result & (1UL << (p_BitCount - 1))) != 0)
                return (long)(s_Result | (ulong.MaxValue << p_BitCount));

            return (long)s_Result;
        }

        public bool ReadHighBit()
        {
            if (CurrentBitsLeft == 0) UpdateBits();

            if (HasReadLow)
                throw new Exception("Trying to read high bit after reading low bit. Will result in data loss. pLz fix!");
            HasReadHigh = true;

            int currentByteIndexLow = (CurrentBitsLeft - 1) >> 3;
            int currentByteIndexHigh = _bytesPerSlice - currentByteIndexLow - 1;
            int s_BitMask = 1 << (CurrentBitsLeft - (currentByteIndexLow << 3) - 1);

            bool result = (BitBuffer[currentByteIndexHigh] & s_BitMask) != 0;

            CurrentBitsLeft--;
            return result;
        }

        private void UpdateBits()
        {
            if (Disposed) throw new ObjectDisposedException("BitReader");

            if (ReadInternal(BitBuffer, 0, _bytesPerSlice) == 0)
                throw new Exception("End of stream bitreader");

            if (_bytesPerSlice > 1 && Endianness != Endian.Big)
            {
                // Optimized in-place swap (No allocation)
                for (int i = 0, j = _bytesPerSlice - 1; i < j; i++, j--)
                {
                    byte temp = BitBuffer[i];
                    BitBuffer[i] = BitBuffer[j];
                    BitBuffer[j] = temp;
                }
            }

            CurrentBitsLeft = _bitsPerSlice;
        }

        public new void Dispose()
        {
            if (Disposed) return;
            base.Dispose();
            Disposed = true;
            if (_shouldDispose)
                BaseStream?.Dispose();
        }

        public void Flush()
        {
            if (Disposed) throw new ObjectDisposedException("BitReader");
            BaseStream.Flush();
        }

        public override int Read(byte[] p_Buffer, int p_Offset, int p_Count) => throw new NotImplementedException();

        public void SetLength(long p_Value)
        {
            if (Disposed) throw new ObjectDisposedException("BitReader");
            BaseStream.SetLength(p_Value);
        }

        protected int ReadInternal(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            return BaseStream.Read(p_Buffer, p_Offset, p_Count);
        }
    }
}