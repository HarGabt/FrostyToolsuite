using FrostySdk.IO;
using System;
using System.Runtime.InteropServices;

namespace AssetBankPlugin.Ant
{

    /// <summary>
    /// Equivalent of System.BitConverter, but with either endianness.
    /// </summary>
    public abstract class EndianBitConverter
    {
        #region Endianness of this converter
        /// <summary>
        /// Indicates the byte order ("endianness") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public abstract bool IsLittleEndian();

        /// <summary>
        /// Indicates the byte order ("endianness") in which data is converted using this class.
        /// </summary>
        public abstract Endian Endianness { get; }
        #endregion

        #region Factory properties
        /// <summary>
        /// Returns a little-endian bit converter instance. The same instance is
        /// always returned.
        /// </summary>
        public static LittleEndianBitConverter Little { get; } = new LittleEndianBitConverter();

        /// <summary>
        /// Returns a big-endian bit converter instance. The same instance is
        /// always returned.
        /// </summary>
        public static BigEndianBitConverter Big { get; } = new BigEndianBitConverter();

        #endregion

        #region Double/primitive conversions
        /// <summary>
        /// Converts the specified double-precision floating point number to a 
        /// 64-bit signed integer. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="p_Value">The number to convert. </param>
        /// <returns>A 64-bit signed integer whose value is equivalent to value.</returns>
        public long DoubleToInt64Bits(double p_Value)
        {
            return BitConverter.DoubleToInt64Bits(p_Value);
        }

        /// <summary>
        /// Converts the specified 64-bit signed integer to a double-precision 
        /// floating point number. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="p_Value">The number to convert. </param>
        /// <returns>A double-precision floating point number whose value is equivalent to value.</returns>
        public double Int64BitsToDouble(long p_Value)
        {
            return BitConverter.Int64BitsToDouble(p_Value);
        }

        /// <summary>
        /// Converts the specified single-precision floating point number to a 
        /// 32-bit signed integer. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="p_Value">The number to convert. </param>
        /// <returns>A 32-bit signed integer whose value is equivalent to value.</returns>
        public int SingleToInt32Bits(float p_Value)
        {
            return new Int32SingleUnion(p_Value).AsInt32;
        }

        /// <summary>
        /// Converts the specified 32-bit signed integer to a single-precision floating point 
        /// number. Note: the endianness of this converter does not
        /// affect the returned value.
        /// </summary>
        /// <param name="p_Value">The number to convert. </param>
        /// <returns>A single-precision floating point number whose value is equivalent to value.</returns>
        public float Int32BitsToSingle(int p_Value)
        {
            return new Int32SingleUnion(p_Value).AsSingle;
        }
        #endregion

        #region To(PrimitiveType) conversions
        /// <summary>
        /// Returns a Boolean value converted from one byte at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>true if the byte at startIndex in value is nonzero; otherwise, false.</returns>
        public bool ToBoolean(byte[] p_Value, int p_StartIndex)
        {
            CheckByteArgument(p_Value, p_StartIndex, 1);
            return BitConverter.ToBoolean(p_Value, p_StartIndex);
        }

        /// <summary>
        /// Returns a Unicode character converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A character formed by two bytes beginning at startIndex.</returns>
        public char ToChar(byte[] p_Value, int p_StartIndex)
        {
            return unchecked((char)CheckedFromBytes(p_Value, p_StartIndex, 2));
        }

        /// <summary>
        /// Returns a double-precision floating point number converted from eight bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A double precision floating point number formed by eight bytes beginning at startIndex.</returns>
        public double ToDouble(byte[] p_Value, int p_StartIndex)
        {
            return Int64BitsToDouble(ToInt64(p_Value, p_StartIndex));
        }

        /// <summary>
        /// Returns a single-precision floating point number converted from four bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A single precision floating point number formed by four bytes beginning at startIndex.</returns>
        public float ToSingle(byte[] p_Value, int p_StartIndex)
        {
            return Int32BitsToSingle(ToInt32(p_Value, p_StartIndex));
        }

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A 16-bit signed integer formed by two bytes beginning at startIndex.</returns>
        public short ToInt16(byte[] p_Value, int p_StartIndex)
        {
            return unchecked((short)CheckedFromBytes(p_Value, p_StartIndex, 2));
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A 32-bit signed integer formed by four bytes beginning at startIndex.</returns>
        public int ToInt32(byte[] p_Value, int p_StartIndex)
        {
            return unchecked((int)CheckedFromBytes(p_Value, p_StartIndex, 4));
        }

        /// <summary>
        /// Returns a 64-bit signed integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A 64-bit signed integer formed by eight bytes beginning at startIndex.</returns>
        public long ToInt64(byte[] p_Value, int p_StartIndex)
        {
            return CheckedFromBytes(p_Value, p_StartIndex, 8);
        }

        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A 16-bit unsigned integer formed by two bytes beginning at startIndex.</returns>
        public ushort ToUInt16(byte[] p_Value, int p_StartIndex)
        {
            return unchecked((ushort)CheckedFromBytes(p_Value, p_StartIndex, 2));
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from four bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A 32-bit unsigned integer formed by four bytes beginning at startIndex.</returns>
        public uint ToUInt32(byte[] p_Value, int p_StartIndex)
        {
            return unchecked((uint)CheckedFromBytes(p_Value, p_StartIndex, 4));
        }

        /// <summary>
        /// Returns a 64-bit unsigned integer converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A 64-bit unsigned integer formed by eight bytes beginning at startIndex.</returns>
        public ulong ToUInt64(byte[] p_Value, int p_StartIndex)
        {
            return unchecked((ulong)CheckedFromBytes(p_Value, p_StartIndex, 8));
        }

        /// <summary>
        /// Checks the given argument for validity.
        /// </summary>
        /// <param name="p_Value">The byte array passed in</param>
        /// <param name="p_StartIndex">The start index passed in</param>
        /// <param name="p_BytesRequired">The number of bytes required</param>
        /// <exception cref="ArgumentNullException">value is a null reference</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// startIndex is less than zero or greater than the length of value minus bytesRequired.
        /// </exception>
        protected static void CheckByteArgument(byte[] p_Value, int p_StartIndex, int p_BytesRequired)
        {
            if (p_Value == null)
                throw new ArgumentNullException(nameof(p_Value));

            if (p_StartIndex < 0 || p_StartIndex > p_Value.Length - p_BytesRequired)
                throw new ArgumentOutOfRangeException(nameof(p_StartIndex));
        }

        /// <summary>
        /// Checks the arguments for validity before calling FromBytes
        /// (which can therefore assume the arguments are valid).
        /// </summary>
        /// <param name="p_Value">The bytes to convert after checking</param>
        /// <param name="p_StartIndex">The index of the first byte to convert</param>
        /// <param name="p_BytesToConvert">The number of bytes to convert</param>
        /// <returns></returns>
        protected long CheckedFromBytes(byte[] p_Value, int p_StartIndex, int p_BytesToConvert)
        {
            CheckByteArgument(p_Value, p_StartIndex, p_BytesToConvert);
            return FromBytes(p_Value, p_StartIndex, p_BytesToConvert);
        }

        /// <summary>
        /// Convert the given number of bytes from the given array, from the given start
        /// position, into a long, using the bytes as the least significant part of the long.
        /// By the time this is called, the arguments have been checked for validity.
        /// </summary>
        /// <param name="p_Value">The bytes to convert</param>
        /// <param name="p_StartIndex">The index of the first byte to convert</param>
        /// <param name="p_BytesToConvert">The number of bytes to use in the conversion</param>
        /// <returns>The converted number</returns>
        protected abstract long FromBytes(byte[] p_Value, int p_StartIndex, int p_BytesToConvert);
        #endregion

        #region ToString conversions
        /// <summary>
        /// Returns a String converted from the elements of a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <remarks>All the elements of value are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(byte[] p_Value)
        {
            return BitConverter.ToString(p_Value);
        }

        /// <summary>
        /// Returns a String converted from the elements of a byte array starting at a specified array position.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <remarks>The elements from array position startIndex to the end of the array are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(byte[] p_Value, int p_StartIndex)
        {
            return BitConverter.ToString(p_Value, p_StartIndex);
        }

        /// <summary>
        /// Returns a String converted from a specified number of bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <param name="p_Length">The number of bytes to convert.</param>
        /// <remarks>The length elements from array position startIndex are converted.</remarks>
        /// <returns>
        /// A String of hexadecimal pairs separated by hyphens, where each pair 
        /// represents the corresponding element in value; for example, "7F-2C-4A".
        /// </returns>
        public static string ToString(byte[] p_Value, int p_StartIndex, int p_Length)
        {
            return BitConverter.ToString(p_Value, p_StartIndex, p_Length);
        }
        #endregion

        #region	Decimal conversions
        /// <summary>
        /// Returns a decimal value converted from sixteen bytes 
        /// at a specified position in a byte array.
        /// </summary>
        /// <param name="p_Value">An array of bytes.</param>
        /// <param name="p_StartIndex">The starting position within value.</param>
        /// <returns>A decimal  formed by sixteen bytes beginning at startIndex.</returns>
        public decimal ToDecimal(byte[] p_Value, int p_StartIndex)
        {
            // HACK: This always assumes four parts, each in their own endianness,
            // starting with the first part at the start of the byte array.
            // On the other hand, there's no real format specified...
            var s_Parts = new int[4];

            for (var i = 0; i < 4; i++)
                s_Parts[i] = ToInt32(p_Value, p_StartIndex + i * 4);

            return new decimal(s_Parts);
        }

        /// <summary>
        /// Returns the specified decimal value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 16.</returns>
        public byte[] GetBytes(decimal p_Value)
        {
            var s_Bytes = new byte[16];
            var s_Parts = decimal.GetBits(p_Value);

            for (var i = 0; i < 4; i++)
                CopyBytesImpl(s_Parts[i], 4, s_Bytes, i * 4);

            return s_Bytes;
        }

        /// <summary>
        /// Copies the specified decimal value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">A character to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(decimal p_Value, byte[] p_Buffer, int p_Index)
        {
            var s_Parts = decimal.GetBits(p_Value);

            for (var i = 0; i < 4; i++)
                CopyBytesImpl(s_Parts[i], 4, p_Buffer, i * 4 + p_Index);
        }
        #endregion

        #region GetBytes conversions
        /// <summary>
        /// Returns an array with the given number of bytes formed
        /// from the least significant bytes of the specified value.
        /// This is used to implement the other GetBytes methods.
        /// </summary>
        /// <param name="p_Value">The value to get bytes for</param>
        /// <param name="p_Bytes">The number of significant bytes to return</param>
        private byte[] GetBytes(long p_Value, int p_Bytes)
        {
            var s_Buffer = new byte[p_Bytes];
            CopyBytes(p_Value, p_Bytes, s_Buffer, 0);
            return s_Buffer;
        }

        /// <summary>
        /// Returns the specified Boolean value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">A Boolean value.</param>
        /// <returns>An array of bytes with length 1.</returns>
        public byte[] GetBytes(bool p_Value)
        {
            return BitConverter.GetBytes(p_Value);
        }

        /// <summary>
        /// Returns the specified Unicode character value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">A character to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(char p_Value)
        {
            return GetBytes(p_Value, 2);
        }

        /// <summary>
        /// Returns the specified double-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(double p_Value)
        {
            return GetBytes(DoubleToInt64Bits(p_Value), 8);
        }

        /// <summary>
        /// Returns the specified 16-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(short p_Value)
        {
            return GetBytes(p_Value, 2);
        }

        /// <summary>
        /// Returns the specified 32-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(int p_Value)
        {
            return GetBytes(p_Value, 4);
        }

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(long p_Value)
        {
            return GetBytes(p_Value, 8);
        }

        /// <summary>
        /// Returns the specified single-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(float p_Value)
        {
            return GetBytes(SingleToInt32Bits(p_Value), 4);
        }

        /// <summary>
        /// Returns the specified 16-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public byte[] GetBytes(ushort p_Value)
        {
            return GetBytes(p_Value, 2);
        }

        /// <summary>
        /// Returns the specified 32-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public byte[] GetBytes(uint p_Value)
        {
            return GetBytes(p_Value, 4);
        }

        /// <summary>
        /// Returns the specified 64-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public byte[] GetBytes(ulong p_Value)
        {
            return GetBytes(unchecked((long)p_Value), 8);
        }

        #endregion

        #region CopyBytes conversions
        /// <summary>
        /// Copies the given number of bytes from the least-specific
        /// end of the specified value into the specified byte array, beginning
        /// at the specified index.
        /// This is used to implement the other CopyBytes methods.
        /// </summary>
        /// <param name="p_Value">The value to copy bytes for</param>
        /// <param name="p_Bytes">The number of significant bytes to copy</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        private void CopyBytes(long p_Value, int p_Bytes, byte[] p_Buffer, int p_Index)
        {
            if (p_Buffer.Length < p_Index + p_Bytes)
                throw new ArgumentOutOfRangeException(nameof(p_Buffer), "Buffer not big enough for value");

            CopyBytesImpl(p_Value, p_Bytes, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the given number of bytes from the least-specific
        /// end of the specified value into the specified byte array, beginning
        /// at the specified index.
        /// This must be implemented in concrete derived classes, but the implementation
        /// may assume that the value will fit into the buffer.
        /// </summary>
        /// <param name="p_Value">The value to copy bytes for</param>
        /// <param name="p_Bytes">The number of significant bytes to copy</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        protected abstract void CopyBytesImpl(long p_Value, int p_Bytes, byte[] p_Buffer, int p_Index);

        /// <summary>
        /// Copies the specified Boolean value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">A Boolean value.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(bool p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value ? 1 : 0, 1, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified Unicode character value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">A character to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(char p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value, 2, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified double-precision floating point value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(double p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(DoubleToInt64Bits(p_Value), 8, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified 16-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(short p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value, 2, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified 32-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(int p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value, 4, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified 64-bit signed integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(long p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value, 8, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified single-precision floating point value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(float p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(SingleToInt32Bits(p_Value), 4, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified 16-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(ushort p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value, 2, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified 32-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(uint p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(p_Value, 4, p_Buffer, p_Index);
        }

        /// <summary>
        /// Copies the specified 64-bit unsigned integer value into the specified byte array,
        /// beginning at the specified index.
        /// </summary>
        /// <param name="p_Value">The number to convert.</param>
        /// <param name="p_Buffer">The byte array to copy the bytes into</param>
        /// <param name="p_Index">The first index into the array to copy the bytes into</param>
        public void CopyBytes(ulong p_Value, byte[] p_Buffer, int p_Index)
        {
            CopyBytes(unchecked((long)p_Value), 8, p_Buffer, p_Index);
        }

        #endregion

        #region Private struct used for Single/Int32 conversions
        /// <summary>
        /// Union used solely for the equivalent of DoubleToInt64Bits and vice versa.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Int32SingleUnion
        {
            /// <summary>
            /// Returns the value of the instance as an integer.
            /// </summary>
            internal int AsInt32 => m_IntValue;

            /// <summary>
            /// Returns the value of the instance as a floating point number.
            /// </summary>
            internal float AsSingle => m_FloatValue;

            /// <summary>
            /// Int32 version of the value.
            /// </summary>
            [FieldOffset(0)]
            private readonly int m_IntValue;

            /// <summary>
            /// Single version of the value.
            /// </summary>
            [FieldOffset(0)]
            private readonly float m_FloatValue;

            /// <summary>
            /// Creates an instance representing the given integer.
            /// </summary>
            /// <param name="p_IntValue">The integer value of the new instance.</param>
            internal Int32SingleUnion(int p_IntValue)
            {
                m_FloatValue = 0; // Just to keep the compiler happy
                m_IntValue = p_IntValue;
            }

            /// <summary>
            /// Creates an instance representing the given floating point number.
            /// </summary>
            /// <param name="p_FloatValue">The floating point value of the new instance.</param>
            internal Int32SingleUnion(float p_FloatValue)
            {
                m_IntValue = 0; // Just to keep the compiler happy
                m_FloatValue = p_FloatValue;
            }
        }
        #endregion
    }
}