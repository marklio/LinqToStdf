// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;
using System.Diagnostics;

namespace LinqToStdf {

    /// <summary>
    /// Knows how to write STDF-relevant binary data to a stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Due to the lack of any endian-aware binary writer class in the framework,
    /// this class exists to abstract endian-ness issues.
    /// In addition, it adds some STDF-specific datatype writing
    /// such as variable-length strings and dates.
    /// </para>
    /// <para>
    /// As an implementation detail, record writing is done "backwards"
    /// from the end of the record.  For this reason, the writer
    /// has a constructor arg to support this.  This makes the implementation of
    /// optional fields at the end of a record far simpler
    /// </para>
    /// </remarks>
    public class BinaryWriter {

        //TODO: consolidate between reader/writers
        /// <summary>
        /// The epoch used for STDF dates
        /// </summary>
        static readonly DateTime _Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Encoder used to encoding strings and characters
        /// </summary>
        static Encoding _Encoding = CreateEncoding();

        /// <summary>
        /// Creates an ASCII encoder that throws if we can't encode the string to ASCII
        /// </summary>
        static Encoding CreateEncoding() {
            var encoding = (ASCIIEncoding)Encoding.ASCII.Clone();
            encoding.EncoderFallback = EncoderFallback.ExceptionFallback;
            return encoding;
        }

        /// <summary>
        /// Constructs a <see cref="BinaryWriter"/> on the given stream.  The stream is
        /// assumed to contain little endian data
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to Write from</param>
        public BinaryWriter(Stream stream) : this(stream, Endian.Little, writeBackwards: false) { }

        /// <summary>
        /// Constructs a <see cref="BinaryWriter"/> on the given stream with
        /// the given endian-ness
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to Write to</param>
        /// <param name="streamEndian">The endian-ness of the stream</param>
        /// <param name="writeBackwards">Whether to write to the stream "backwards"</param>
        public BinaryWriter(Stream stream, Endian streamEndian, bool writeBackwards) {
            Debug.Assert(stream != null, "The provided stream is null");
            _Stream = stream;
            _StreamEndian = streamEndian;
            _WriteBackwards = writeBackwards;
        }

        private Stream _Stream;
        private Endian _StreamEndian;
        private byte[] _Buffer;
        private bool _WriteBackwards;

        /// <summary>
        /// Writes from the buffer to the stream
        /// </summary>
        /// <param name="length">The number of bytes to write</param>
        void WriteToStream(int length) {
            if (_WriteBackwards) {
                Array.Reverse(_Buffer, 0, length);
            }
            _Stream.Write(_Buffer, 0, length);
        }

        /// <summary>
        /// Writes an STDF record header to the stream
        /// </summary>
        /// <returns></returns>
        public void WriteHeader(RecordHeader header) {
            if (_WriteBackwards) {
                WriteByte(header.RecordType.Subtype);
                WriteByte(header.RecordType.Type);
                WriteUInt16(header.Length);
            }
            else {
                WriteUInt16(header.Length);
                WriteByte(header.RecordType.Type);
                WriteByte(header.RecordType.Subtype);
            }
        }

        /// <summary>
        /// Helper function capable of writing any array type,
        /// given an element writer function (which must honor
        /// "write backwards" state.)
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <param name="arr">The array to write</param>
        /// <param name="writeFunc">The function that will write a single element</param>
        void WriteArray<T>(T[] arr, Action<T> writeFunc) {
            Debug.Assert(writeFunc != null, "The provided writeFunc delegate is null");
            if (arr == null || arr.Length == 0) return;
            if (_WriteBackwards) {
                arr = (T[])arr.Clone();
                Array.Reverse(arr);
            }
            for (var i = 0; i < arr.Length; i++) {
                writeFunc(arr[i]);
            }
        }

        /// <summary>
        /// Writes a byte
        /// </summary>
        public void WriteByte(byte value) {
            _Stream.WriteByte(value);
        }

        /// <summary>
        /// Writes a byte array
        /// </summary>
        public void WriteByteArray(byte[] value) {
            WriteArray(value, WriteByte);
        }

        /// <summary>
        /// Writes a nibble array
        /// </summary>
        public void WriteNibbleArray(byte[] value) {
            if (value == null || value.Length == 0) return;
            var newArray = new byte[(value.Length + 1) / 2];
            for (var i = 0; i < newArray.Length; i++) {
                var temp = value[(2 * i)];
                if (((2 * i) + 1) < newArray.Length) {
                    temp |= (byte)(value[(2 * i) + 1] << 4);
                }
                newArray[i] = temp;
            }
            newArray.CopyTo(_Buffer, 0);
            WriteToStream(newArray.Length);
        }

        /// <summary>
        /// Writes a bit array
        /// </summary>
        public void WriteBitArray(BitArray value) {
            value = value ?? new BitArray(0);
            var length = (ushort)value.Length;
            if (!_WriteBackwards) {
                WriteUInt16(length);
            }
            if (length > 0) {
                var bufferLength = (length + 31) / 32;
                EnsureBufferLength(bufferLength);
                value.CopyTo(_Buffer, 0);
                WriteToStream(bufferLength);
            }
            if (_WriteBackwards) {
                WriteUInt16(length);
            }
        }

        /// <summary>
        /// Writes a signed byte
        /// </summary>
        public void WriteSByte(sbyte value) {
            _Stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a signed byte array
        /// </summary>
        /// <param name="value"></param>
        public void WriteSByteArray(sbyte[] value) {
            WriteArray(value, WriteSByte);
        }

        /// <summary>
        /// Writes an unsigned 2-byte integer
        /// </summary>
        public void WriteUInt16(ushort value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(2);
        }

        /// <summary>
        /// Writes an unsigned 2-byte integer array
        /// </summary>
        public void WriteUInt16Array(ushort[] value) {
            WriteArray(value, WriteUInt16);
        }

        /// <summary>
        /// Writes a 2-byte integer
        /// </summary>
        public void WriteInt16(short value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(2);
        }

        /// <summary>
        /// Writes a 2-byte integer array
        /// </summary>
        public void WriteInt16Array(short[] value) {
            WriteArray(value, WriteInt16);
        }

        /// <summary>
        /// Writes an unsigned 4-byte integer
        /// </summary>
        public void WriteUInt32(uint value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(4);
        }

        /// <summary>
        /// Writes an unsigned 4-byte integer array
        /// </summary>
        public void WriteUInt32Array(uint[] value) {
            WriteArray(value, WriteUInt32);
        }

        /// <summary>
        /// Writes a 4-byte integer
        /// </summary>
        public void WriteInt32(int value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(4);
        }

        /// <summary>
        /// Writes a 4-byte integer array
        /// </summary>
        public void WriteInt32Array(int[] value) {
            WriteArray(value, WriteInt32);
        }

        /// <summary>
        /// Writes an unsigned 8-byte integer
        /// </summary>
        public void WriteUInt64(ulong value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(8);
        }

        /// <summary>
        /// Writes an unsigned 8-byte integer array
        /// </summary>
        public void WriteUInt64Array(ulong[] value) {
            WriteArray(value, WriteUInt64);
        }

        /// <summary>
        /// Writes an 8-byte integer
        /// </summary>
        public void WriteInt64(long value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(8);
        }

        /// <summary>
        /// Writes an 8-byte integer array
        /// </summary>
        public void WriteInt64Array(long[] value) {
            WriteArray(value, WriteInt64);
        }

        /// <summary>
        /// Writes a 4-byte IEEE floating point number
        /// </summary>
        public void WriteSingle(float value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(4);
        }

        /// <summary>
        /// Writes a 4-byte IEEE floating point number array
        /// </summary>
        public void WriteSingleArray(float[] value) {
            WriteArray(value, WriteSingle);
        }

        /// <summary>
        /// Writes an 8-byte IEEE floating point number
        /// </summary>
        public void WriteDouble(double value) {
            WriteToBuffer(BitConverter.GetBytes(value));
            WriteToStream(8);
        }

        /// <summary>
        /// Writes an 8-byte IEEE floating point number array
        /// </summary>
        public void WriteDoubleArray(double[] value) {
            WriteArray(value, WriteDouble);
        }

        /// <summary>
        /// Writes a single character
        /// </summary>
        public void WriteCharacter(char value) {
            EnsureBufferLength(1);
            _Encoding.GetBytes(value.ToString(), 0, 1, _Buffer, 0);
            WriteToStream(1);
        }

        /// <summary>
        /// Writes an array of single characters
        /// </summary>
        public void WriteCharacterArray(char[] value) {
            WriteArray(value, WriteCharacter);
        }

        /// <summary>
        /// Writes a string of the given length, truncating the rest
        /// </summary>
        public void WriteString(string value, int length) {
            EnsureBufferLength(length);
            _Encoding.GetBytes(value, 0, length, _Buffer, 0);
            WriteToStream(length);
        }

        /// <summary>
        /// Writes a string where the first byte indicates the length
        /// </summary>
        public void WriteString(string value) {
            value = value ?? String.Empty;
            if (value.Length > 255) throw new InvalidOperationException(Resources.StringTooLong);
            if (!_WriteBackwards) {
                WriteByte((byte)value.Length);
            }
            if (value.Length > 0) {
                EnsureBufferLength(value.Length);
                _Encoding.GetBytes(value, 0, value.Length, _Buffer, 0);
                WriteToStream(value.Length);
            }
            if (_WriteBackwards) {
                WriteByte((byte)value.Length);
            }
        }

        /// <summary>
        /// Writes a string where the first byte indicates the length
        /// </summary>
        public void WriteStringArray(string[] value) {
            WriteArray(value, WriteString);
        }

        // TODO: The current STDF spec indicates no need for this, but do we want a WriteStringArray method for non-single-character fixed-length strings?

        /// <summary>
        /// Writes an STDF datetime (4-byte integer seconds since the epoch)
        /// </summary>
        public void WriteDateTime(DateTime value) {
            var seconds = (uint)(value - BinaryWriter._Epoch).TotalSeconds;
            WriteUInt32(seconds);
        }

        #region Buffer Management

        /// <summary>
        /// Writes bytes into the buffer and takes care of any endian swapping necessary
        /// </summary>
        /// <param name="value">the bytes to Write</param>
        void WriteToBuffer(byte[] value) {
            WriteToBuffer(value, true);
        }

        /// <summary>
        /// Writes bytes into the buffer with an option to take care of any endian swapping necessary
        /// </summary>
        /// <param name="value">the bytes to Write</param>
        /// <param name="endianize">true to take care of endian-ness</param>
        void WriteToBuffer(byte[] value, bool endianize) {
            FillBuffer(value);
            if (endianize) {
                Endianize(value.Length);
            }
        }

        /// <summary>
        /// Fills the buffer
        /// </summary>
        /// <param name="value">the bytes</param>
        void FillBuffer(byte[] value) {
            Debug.Assert(value != null, "value was null");
            if (value.Length > 0) {
                EnsureBufferLength(value.Length);
                value.CopyTo(_Buffer, 0);
            }
        }

        /// <summary>
        /// Ensures the buffer is large enough to handle the specified length
        /// </summary>
        /// <param name="length">The length required</param>
        void EnsureBufferLength(int length) {
            Debug.Assert(length >= 0, "length must be >= 0");
            if (_Buffer == null || _Buffer.Length < length) {
                _Buffer = new byte[length];
            }
        }

        /// <summary>
        /// Reverses the content of the buffer (within the specified length from the beginning)
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

    }
}
