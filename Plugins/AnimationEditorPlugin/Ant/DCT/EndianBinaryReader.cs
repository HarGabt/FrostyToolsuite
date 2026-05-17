using FrostySdk.IO;
using System;
using System.IO;

namespace AssetBankPlugin.Ant
{
    /// <summary>
    /// Equivalent of System.IO.BinaryReader, but with either endianness, depending on
    /// the EndianBitConverter it is constructed with. No data is buffered in the
    /// reader; the client may seek within the stream at will.
    /// </summary>
    public abstract class EndianBinaryReader : Stream, IDisposable
    {
        /// <summary>
        /// Current position of the stream.
        /// </summary>
        public override long Position
        {
            get => BaseStream.Position;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Length of the underlying stream.
        /// </summary>
        public override long Length => BaseStream.Length;

        /// <summary>
        /// The bit converter used to read values from the stream
        /// </summary>
        public EndianBitConverter BitConverter { get; private set; }

        /// <summary>
        /// Gets the underlying stream of the EndianBinaryReader.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// The endianness of the endian converter.
        /// </summary>
        public Endian Endianness
        {
            get => BitConverter.Endianness;
            set
            {
                if (value == Endian.Big)
                    BitConverter = EndianBitConverter.Big;
                else
                    BitConverter = EndianBitConverter.Little;
            }
        }

        // Declare our capabilities.
        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => BaseStream.CanWrite;

        /// <summary>
        /// Whether or not this reader has been disposed yet.
        /// </summary>
        protected bool m_Disposed;

        /// <summary>
        /// Buffer used for temporary storage before conversion into primitives
        /// </summary>
        protected readonly byte[] m_Buffer = new byte[16];

        /// <summary>
        /// Whether to dispose the input stream when disposing the reader.
        /// </summary>
        protected bool m_ShouldDispose;

        /// <summary>
        /// Constructs a new binary reader with the given bit converter, reading
        /// to the given stream.
        /// </summary>
        /// <param name="p_BitConverter">Converter to use when reading data</param>
        /// <param name="p_Stream">Stream to read data from</param>
        /// <param name="p_ShouldDispose">Whether to dispose the input stream when disposing the reader or not.</param>
        protected EndianBinaryReader(EndianBitConverter p_BitConverter, Stream p_Stream, bool p_ShouldDispose)
        {
            if (!p_Stream.CanRead)
                throw new ArgumentException("Stream isn't readable", nameof(p_Stream));

            m_ShouldDispose = p_ShouldDispose;

            BaseStream = p_Stream;
            BitConverter = p_BitConverter;
        }

        /// <summary>
        /// Reads a single byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public override int ReadByte()
        {
            ReadInternal(m_Buffer, 0, 1);
            return m_Buffer[0];
        }

        /// <summary>
        /// Reads a single byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public byte ReadUByte()
        {
            ReadInternal(m_Buffer, 0, 1);
            return m_Buffer[0];
        }

        /// <summary>
        /// Reads a single signed byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public sbyte ReadSByte()
        {
            ReadInternal(m_Buffer, 0, 1);
            return unchecked((sbyte)m_Buffer[0]);
        }

        /// <summary>
        /// Reads a boolean from the stream. 1 byte is read.
        /// </summary>
        /// <returns>The boolean read</returns>
        public bool ReadBool()
        {
            ReadInternal(m_Buffer, 0, 1);
            return BitConverter.ToBoolean(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream, using the bit converter
        /// for this reader. 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit integer read</returns>
        public short ReadInt16()
        {
            ReadInternal(m_Buffer, 0, 2);
            return BitConverter.ToInt16(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit integer read</returns>
        public int ReadInt32()
        {
            ReadInternal(m_Buffer, 0, 4);
            return BitConverter.ToInt32(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream, using the bit converter
        /// for this reader. 8 bytes are read.
        /// </summary>
        /// <returns>The 64-bit integer read</returns>
        public long ReadInt64()
        {
            ReadInternal(m_Buffer, 0, 8);
            return BitConverter.ToInt64(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit unsigned integer read</returns>
        public ushort ReadUInt16()
        {
            ReadInternal(m_Buffer, 0, 2);
            return BitConverter.ToUInt16(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit unsigned integer read</returns>
        public uint ReadUInt32()
        {
            ReadInternal(m_Buffer, 0, 4);
            return BitConverter.ToUInt32(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a 64-bit unsigned integer from the stream, using the bit converter
        /// for this reader. 8 bytes are read.
        /// </summary>
        /// <returns>The 64-bit unsigned integer read</returns>
        public ulong ReadUInt64()
        {
            ReadInternal(m_Buffer, 0, 8);
            return BitConverter.ToUInt64(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a single-precision floating-point value from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The floating point value read</returns>
        public float ReadSingle()
        {
            ReadInternal(m_Buffer, 0, 4);
            return BitConverter.ToSingle(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a double-precision floating-point value from the stream, using the bit converter
        /// for this reader. 8 bytes are read.
        /// </summary>
        /// <returns>The floating point value read</returns>
        public double ReadDouble()
        {
            ReadInternal(m_Buffer, 0, 8);
            return BitConverter.ToDouble(m_Buffer, 0);
        }

        /// <summary>
        /// Reads a decimal value from the stream, using the bit converter
        /// for this reader. 16 bytes are read.
        /// </summary>
        /// <returns>The decimal value read</returns>
        public decimal ReadDecimal()
        {
            ReadInternal(m_Buffer, 0, 16);
            return BitConverter.ToDecimal(m_Buffer, 0);
        }

        /// <summary>
        /// Reads the specified number of bytes into the given buffer, starting at
        /// the given offset.
        /// </summary>
        /// <param name="p_Data">The buffer to copy data into</param>
        /// <param name="p_Offset">The offset to copy data into</param>
        /// <param name="p_Count"></param>
        /// <returns>The number of bytes actually read. This will only be less than
        /// the requested number of bytes if the end of the stream is reached.
        /// </returns>
        public virtual int ReadBytes(byte[] p_Data, int p_Offset, int p_Count)
        {
            CheckDisposed();

            if (p_Offset < 0)
                throw new ArgumentOutOfRangeException(nameof(p_Offset));

            if (p_Count < 0)
                throw new ArgumentOutOfRangeException(nameof(p_Count));

            if (p_Count + p_Offset > p_Data.Length)
                throw new ArgumentException("Not enough space in buffer for specified number of bytes starting at specified offset.");

            return ReadInternal(p_Data, p_Offset, p_Count);
        }

        /// <summary>
        /// Reads the specified number of bytes, returning them in a new byte array.
        /// If not enough bytes are available before the end of the stream, this
        /// method will throw an exception.
        /// </summary>
        /// <param name="p_Count">The number of bytes to read</param>
        /// <returns>The bytes read</returns>
        public virtual byte[] ReadBytes(int p_Count)
        {
            CheckDisposed();

            if (p_Count < 0)
                throw new ArgumentOutOfRangeException(nameof(p_Count));

            var s_Buffer = new byte[p_Count];
            var s_BytesRead = ReadInternal(s_Buffer, 0, p_Count);

            // If we didn't read the required amount of bytes then throw an exception.
            if (s_BytesRead != p_Count)
                throw new EndOfStreamException($"End of stream reached with {p_Count - s_BytesRead} byte{(p_Count - s_BytesRead == 1 ? "s" : "")} left to read.");

            return s_Buffer;
        }

        /// <summary>
        /// Disposes of the underlying stream.
        /// </summary>
        public new virtual void Dispose()
        {
            base.Dispose();

            CheckDisposed();

            m_Disposed = true;

            if (m_ShouldDispose)
                BaseStream.Dispose();
        }

        public override void Flush()
        {
            CheckDisposed();
            BaseStream.Flush();
        }

        public override long Seek(long p_Offset, SeekOrigin p_Origin)
        {
            CheckDisposed();
            return BaseStream.Seek(p_Offset, p_Origin);
        }

        public override int Read(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            return ReadInternal(p_Buffer, p_Offset, p_Count);
        }

        public override void SetLength(long p_Value)
        {
            CheckDisposed();
            BaseStream.SetLength(p_Value);
        }

        public override void Write(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            CheckDisposed();
            BaseStream.Write(p_Buffer, p_Offset, p_Count);
        }

        /// <summary>
        /// Checks whether or not the reader has been disposed, throwing an exception if so.
        /// </summary>
        protected void CheckDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException("EndianBinaryReader");
        }

        /// <summary>
        /// Read the specified number of bytes into the provided buffer, starting at the
        /// specified offset.
        /// </summary>
        /// <param name="p_Buffer">The buffer to read the data into</param>
        /// <param name="p_Offset">The offset to start writing the data at</param>
        /// <param name="p_Count">The number of bytes to read</param>
        /// <returns>The number of bytes read</returns>
        protected virtual int ReadInternal(byte[] p_Buffer, int p_Offset, int p_Count)
        {
            CheckDisposed();
            return BaseStream.Read(p_Buffer, p_Offset, p_Count);
        }
    }
}