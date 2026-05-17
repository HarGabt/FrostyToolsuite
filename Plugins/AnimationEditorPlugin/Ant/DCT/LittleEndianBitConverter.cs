using FrostySdk.IO;

namespace AssetBankPlugin.Ant
{

    /// <summary>
    /// Implementation of EndianBitConverter which converts to/from little-endian
    /// byte arrays.
    /// </summary>
    public sealed class LittleEndianBitConverter : EndianBitConverter
    {
        /// <summary>
        /// Indicates the byte order ("endianness") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public override bool IsLittleEndian()
        {
            return true;
        }

        /// <summary>
        /// Indicates the byte order ("endianness") in which data is converted using this class.
        /// </summary>
        public override Endian Endianness => Endian.Little;

        /// <summary>
        /// Copies the specified number of bytes from value to buffer, starting at index.
        /// </summary>
        /// <param name="p_Value">The value to copy</param>
        /// <param name="p_Bytes">The number of bytes to copy</param>
        /// <param name="p_Buffer">The buffer to copy the bytes into</param>
        /// <param name="p_Index">The index to start at</param>
        protected override void CopyBytesImpl(long p_Value, int p_Bytes, byte[] p_Buffer, int p_Index)
        {
            for (var i = 0; i < p_Bytes; ++i)
            {
                p_Buffer[i + p_Index] = unchecked((byte)(p_Value & 0xff));
                p_Value >>= 8;
            }
        }

        /// <summary>
        /// Returns a value built from the specified number of bytes from the given buffer,
        /// starting at index.
        /// </summary>
        /// <param name="p_Buffer">The data in byte array format</param>
        /// <param name="p_StartIndex">The first index to use</param>
        /// <param name="p_BytesToConvert">The number of bytes to use</param>
        /// <returns>The value built from the given bytes</returns>
        protected override long FromBytes(byte[] p_Buffer, int p_StartIndex, int p_BytesToConvert)
        {
            long s_Ret = 0;

            for (var i = 0; i < p_BytesToConvert; ++i)
                s_Ret = unchecked(s_Ret << 8 | p_Buffer[p_StartIndex + p_BytesToConvert - 1 - i]);

            return s_Ret;
        }
    }
}