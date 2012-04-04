// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Diagnostics;

namespace LinqToStdf {

    /// <summary>
    /// Knows how to read STDF-relevant binary data from a stream.
    /// </summary>
    /// <remarks>
    /// Due to the lack of any endian-aware binary reader class in the framework,
    /// this class exists to abstract endian-ness issues.
    /// In addition, it adds some STDF-specific datatype reading
    /// such as variable-length strings and dates.
    /// </remarks>
    public class BinaryReader : IDisposable {
        /// <summary>
        /// The epoch used for STDF dates
        /// </summary>
        static readonly DateTime _Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Constructs a <see cref="BinaryReader"/> on the given stream.  The stream is
        /// assumed to contain little endian data
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from</param>
        public BinaryReader(Stream stream) : this(stream, Endian.Little, false) { }

        /// <summary>
        /// Constructs a <see cref="BinaryReader"/> on the given stream with
        /// the given endian-ness
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from</param>
        /// <param name="streamEndian">The endian-ness of the stream</param>
        /// <param name="ownsStream">Indicates whether or not the stream should be disposed of with the reader.</param>
        public BinaryReader(Stream stream, Endian streamEndian, bool ownsStream) {
            Debug.Assert(stream != null, "The provided stream was null");
            _Stream = stream;
            _StreamEndian = streamEndian;
            _OwnsStream = ownsStream;
        }

        private Stream _Stream;
        private Endian _StreamEndian;
        private byte[] _Buffer;
        private bool _OwnsStream;

        /// <summary>
        /// Reads an STDF record header from the stream
        /// </summary>
        /// <returns></returns>
        public RecordHeader ReadHeader() {
            var length = ReadUInt16();
            var type = ReadByte();
            var subType = ReadByte();
            return new RecordHeader(length, new RecordType(type, subType));
        }

        /// <summary>
        /// Indicates whether we are at the end of the stream or not
        /// </summary>
        public bool AtEndOfStream {
            get { return this._Stream.Position >= this._Stream.Length; }
        }

        /// <summary>
        /// Helper function capable of reading an array of any type
        /// given a length and an element reader function.
        /// </summary>
        /// <typeparam name="T">The array element type</typeparam>
        /// <param name="length">The length of the array to read</param>
        /// <param name="readFunc">The function that will read a single element</param>
        /// <returns>The array that was read</returns>
        static T[] ReadArray<T>(int length, Func<T> readFunc) {
            Debug.Assert(readFunc != null, "The readFunc delegate is null.");
            var value = new T[length];
            for (var i = 0; i < value.Length; i++) {
                value[i] = readFunc();
            }
            return value;
        }

        /// <summary>
        /// Reads a byte
        /// </summary>
        public byte ReadByte() {
            var value = _Stream.ReadByte();
            if (value == -1) {
                throw new EndOfStreamException();
            }
            return (byte)value;
        }

        /// <summary>
        /// Reads a byte array
        /// </summary>
        public byte[] ReadByteArray(int length) {
            return ReadArray<byte>(length, ReadByte);
        }

        /// <summary>
        /// Reads a nibble array
        /// </summary>
        public byte[] ReadNibbleArray(int length) {
            var value = new byte[length];
            for (var i = 0; i < value.Length; i++) {
                var temp = ReadByte();
                value[i] = (byte)(temp & 0x0F);
                if (++i >= value.Length) break;
                value[i] = (byte)(temp >> 4);
            }
            return value;
        }

        /// <summary>
        /// Reads a bit array
        /// </summary>
        public BitArray ReadBitArray() {
            int length = ReadUInt16();
            if (length == 0) return null;
            var realLength = (length + 7) / 8;
            var bitArray = new BitArray(ReadByteArray(realLength));
            bitArray.Length = length;
            return bitArray;
        }

        /// <summary>
        /// Reads a signed byte
        /// </summary>
        public sbyte ReadSByte() {
            return (sbyte)ReadByte();
        }

        /// <summary>
        /// Reads an SByte array
        /// </summary>
        public sbyte[] ReadSByteArray(int length) {
            return ReadArray<sbyte>(length, ReadSByte);
        }

        /// <summary>
        /// Reads an unsigned 2-byte integer
        /// </summary>
        public ushort ReadUInt16() {
            ReadToBuffer(2);
            return BitConverter.ToUInt16(_Buffer, 0);
        }

        /// <summary>
        /// Reads a Uint16 array
        /// </summary>
        public ushort[] ReadUInt16Array(int length) {
            return ReadArray<ushort>(length, ReadUInt16);
        }

        /// <summary>
        /// Reads a 2-byte integer
        /// </summary>
        public short ReadInt16() {
            ReadToBuffer(2);
            return BitConverter.ToInt16(_Buffer, 0);
        }

        /// <summary>
        /// Reads an Int16 array
        /// </summary>
        public short[] ReadInt16Array(int length) {
            return ReadArray<short>(length, ReadInt16);
        }

        /// <summary>
        /// Reads an unsigned 4-byte integer
        /// </summary>
        public uint ReadUInt32() {
            ReadToBuffer(4);
            return BitConverter.ToUInt32(_Buffer, 0);
        }

        /// <summary>
        /// Reads a UInt32 array
        /// </summary>
        public uint[] ReadUInt32Array(int length) {
            return ReadArray<uint>(length, ReadUInt32);
        }

        /// <summary>
        /// Reads a 4-byte integer
        /// </summary>
        public int ReadInt32() {
            ReadToBuffer(4);
            return BitConverter.ToInt32(_Buffer, 0);
        }

        /// <summary>
        /// Reads an Int32 array
        /// </summary>
        public int[] ReadInt32Array(int length) {
            return ReadArray<int>(length, ReadInt32);
        }

        /// <summary>
        /// Reads an unsigned 8-byte integer
        /// </summary>
        public ulong ReadUInt64() {
            ReadToBuffer(8);
            return BitConverter.ToUInt64(_Buffer, 0);
        }

        /// <summary>
        /// Reads a UInt64 array
        /// </summary>
        public ulong[] ReadUInt64Array(int length) {
            return ReadArray<ulong>(length, ReadUInt64);
        }

        /// <summary>
        /// Reads an 8-byte integer
        /// </summary>
        public long ReadInt64() {
            ReadToBuffer(8);
            return BitConverter.ToInt64(_Buffer, 0);
        }

        /// <summary>
        /// Reads an Int64 array
        /// </summary>
        public long[] ReadInt64Array(int length) {
            return ReadArray<long>(length, ReadInt64);
        }

        /// <summary>
        /// Reads a 4-byte IEEE floating point number
        /// </summary>
        public float ReadSingle() {
            ReadToBuffer(4);
            return BitConverter.ToSingle(_Buffer, 0);
        }

        /// <summary>
        /// Reads an array of 4-byte IEEE floating point numbers
        /// </summary>
        public float[] ReadSingleArray(int length) {
            return ReadArray<float>(length, ReadSingle);
        }

        /// <summary>
        /// Reads an 8-byte IEEE floating point number
        /// </summary>
        public double ReadDouble() {
            ReadToBuffer(8);
            return BitConverter.ToDouble(_Buffer, 0);
        }

        /// <summary>
        /// Reads an array of 8-byte IEEE floating point numbers
        /// </summary>
        public double[] ReadDoubleArray(int length) {
            return ReadArray<double>(length, ReadDouble);
        }

        /// <summary>
        /// Reads a string of the given length
        /// </summary>
        public string ReadString(int length) {
            ReadToBuffer(length, false);
            return Encoding.UTF8.GetString(_Buffer, 0, length);
        }

        /// <summary>
        /// Reads a string where the first byte indicates the length
        /// </summary>
        public string ReadString() {
            var length = ReadByte();
            if (length > 0) {
                return ReadString(length);
            }
            else {
                return string.Empty;
            }
        }

        /// <summary>
        /// Reads an STDF datetime (4-byte integer seconds since the epoch)
        /// </summary>
        public DateTime ReadDateTime() {
            var seconds = ReadUInt32();
            return BinaryReader._Epoch + TimeSpan.FromSeconds((double)seconds);
        }

        /// <summary>
        /// Skips the indicated number of bytes
        /// </summary>
        public void Skip(long bytes) {
            if (bytes < 0) {
                throw new ArgumentOutOfRangeException("bytes", "bytes must be non-negative");
            }
            if (bytes == 0) return;
            if (_Stream.CanSeek) {
                _Stream.Seek(bytes, SeekOrigin.Current);
            }
            else {
                var tempBuffer = new byte[512];
                var fullTimes = bytes / tempBuffer.Length;
                for (int i = 0; i < fullTimes; i++) {
                    _Stream.Read(tempBuffer, 0, tempBuffer.Length);
                }
                var finalLength = bytes % tempBuffer.Length;
                if (finalLength != 0) {
                    _Stream.Read(tempBuffer, 0, (int)finalLength);
                }
            }
        }

        /// <summary>
        /// Skips 1 byte
        /// </summary>
        public void Skip1()
        {
            Skip(1);
        }

        /// <summary>
        /// Skips 2 bytes
        /// </summary>
        public void Skip2()
        {
            Skip(2);
        }

        /// <summary>
        /// Skips 4 bytes
        /// </summary>
        public void Skip4()
        {
            Skip(4);
        }

        /// <summary>
        /// Skips 8 bytes
        /// </summary>
        public void Skip8()
        {
            Skip(8);
        }

        /// <summary>
        /// Skips an array of 1-byte elements of the specified length
        /// </summary>
        public void Skip1Array(byte length)
        {
            Skip(length);
        }

        /// <summary>
        /// Skips an array of 2-byte elements of the specified length
        /// </summary>
        public void Skip2Array(byte length)
        {
            Skip(2 * length);
        }

        /// <summary>
        /// Skips an array of 4-byte elements of the specified length
        /// </summary>
        public void Skip4Array(byte length)
        {
            Skip(4 * length);
        }

        /// <summary>
        /// Skips an array of 8-byte elements of the specified length
        /// </summary>
        public void Skip8Array(byte length)
        {
            Skip(8 * length);
        }

        /// <summary>
        /// Skips a string
        /// </summary>
        public void SkipString()
        {
            var length = ReadByte();
            if (length > 0) {
                Skip(length);
            }
        }

        /// <summary>
        /// Skips a bit array
        /// </summary>
        public void SkipBitArray()
        {
            int length = ReadUInt16();
            if (length > 0) {
                var realLength = (length + 7) / 8;
                Skip(realLength);
            }
        }

        /// <summary>
        /// Skips a nibble array
        /// </summary>
        public void SkipNibbleArray(byte length)
        {
            Skip((length + 1) / 2);
        }

        #region Buffer Management

        /// <summary>
        /// Reads bytes into the buffer and takes care of any endian swapping necessary
        /// </summary>
        /// <param name="length">the number of bytes to read</param>
        void ReadToBuffer(int length) {
            ReadToBuffer(length, true);
        }

        /// <summary>
        /// Reads bytes into the buffer with an option to take care of any endian swapping necessary
        /// </summary>
        /// <param name="length">the number of bytes to read</param>
        /// <param name="endianize">true to take care of endian-ness</param>
        void ReadToBuffer(int length, bool endianize) {
            FillBuffer(length);
            if (endianize) {
                Endianize(length);
            }
        }

        /// <summary>
        /// Fills the buffer
        /// </summary>
        /// <param name="length">the number of bytes</param>
        void FillBuffer(int length) {
            if (length > 0) {
                EnsureBufferLength(length);
                var offset = 0;
                do {
                    var bytesRead = _Stream.Read(_Buffer, offset, length - offset);
                    if (bytesRead == 0) {
                        throw new EndOfStreamException(string.Format(Resources.EndOfStreamException, length - offset, length));
                    }
                    offset += bytesRead;
                } while (offset < length);
            }
            else {
                Debug.Assert(length >= 0, "length cannot be negative");
            }
        }

        /// <summary>
        /// Ensures the buffer is large enough to handle the specified length
        /// </summary>
        /// <param name="length">The length required</param>
        void EnsureBufferLength(int length) {
            Debug.Assert(length >= 0, "length should be >= 0");
            if (_Buffer == null || _Buffer.Length < length) {
                _Buffer = new byte[length];
            }
        }

        /// <summary>
        /// Reverses the content of the buffer (within the specified length from the beginning
        /// </summary>
        /// <param name="length">The relevant length</param>
        void SwapBuffer(int length) {
            Array.Reverse(_Buffer, 0, length);
        }

        /// <summary>
        /// Conditionally calls <see cref="SwapBuffer"/> depending on the endian-ness of the stream.
        /// </summary>
        /// <param name="length"></param>
        void Endianize(int length) {
            if (this._StreamEndian == Endian.Big) {
                SwapBuffer(length);
            }
        }

        #endregion


        #region IDisposable Members

        public void Dispose() {
            if (_OwnsStream) {
                _Stream.Dispose();
            }
        }

        #endregion
    }
}
