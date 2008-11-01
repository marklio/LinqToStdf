using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace LinqToStdf {
    public partial class StdfFile {
        /// <summary>
        /// This provides an abstraction over stream with a more app-specific API,
        /// that allows us to generalize Stream so that we can consume streams that
        /// don't support seeking, Position, or Length.
        /// </summary>
        class RewindableByteStream {
            /// <summary>
            /// The underlying Stream
            /// </summary>
            Stream _Stream;
            /// <summary>
            /// A stream we use for memoizing results of previous read operations,
            /// enabling us to rewind back into them
            /// </summary>
            MemoryStream _MemoizedData;
            /// <summary>
            /// Indicates how far we're rewound into the memoized data
            /// </summary>
            int _Rewound = 0;
            /// <summary>
            /// Tracks our current offset in the stream
            /// </summary>
            int _Offset = 0;
            /// <summary>
            /// buffer used for reading a record header
            /// </summary>
            byte[] _Buffer = new byte[2];

            public RewindableByteStream(Stream stream) {
                _Stream = stream;
                _MemoizedData = new MemoryStream(512);
            }

            public long? Length {
                get {
                    try {
                        return _Stream.Length;
                    }
                    catch (NotSupportedException) {
                        return null;
                    }
                }
            }

            /// <summary>
            /// Reads a record header from the underlying stream.
            /// </summary>
            /// <remarks>
            /// The record header is the only data structure
            /// we need to understand at the file level,
            /// so we have this specialization.
            /// If we return null, the stream will be at the begining
            /// of the header, otherwise, it will be at the content of
            /// the record.
            /// </remarks>
            /// <param name="endian">The endianess of the stream</param>
            /// <returns>A populated record header,
            /// or null if we reached the end of the stream while reading.</returns>
            public RecordHeader? ReadHeader(Endian endian) {
                //read the record length
                var read = Read(_Buffer, 2);
                if (read != 2) {
                    //rewind and return null if we couldn't read both bytes
                    Rewind(read);
                    return null;
                }
                //reverse if necessary
                if (endian == Endian.Big) {
                    Array.Reverse(_Buffer);
                }
                //convert to ushort
                var length = BitConverter.ToUInt16(_Buffer, 0);
                //read the type
                var type = ReadByte();
                //rewind if we are at EOS
                if (type == -1) {
                    Rewind(2);
                    return null;
                }
                //read the subType
                var subType = ReadByte();
                //rewind if we hit EOS
                if (subType == -1) {
                    Rewind(3);
                    return null;
                }
                return new RecordHeader(length, new RecordType((byte)type, (byte)subType));
            }

            /// <summary>
            /// Abstraction of <see cref="Stream.Read"/>.  Reads <paramref name="count"/>
            /// bytes into <paramref name="buffer"/>.
            /// </summary>
            /// <param name="buffer">the buffer to read into</param>
            /// <param name="count">the number of bytes to read</param>
            /// <returns>The number of bytes read</returns>
            public int Read(byte[] buffer, int count) {
                //the offset into buffer.  Start at 0
                int offset = 0;
                //the total bytes read
                int totalRead = 0;
                //if we have bytes to read, and we are rewound,
                //read an appropriate number of bytes from
                //the memoized stream.
                if (count > 0 && _Rewound > 0) {
                    int countToRead = Math.Min(count, _Rewound);
                    totalRead = _MemoizedData.Read(buffer, offset, countToRead);
                    //this should never be true by construction
                    if (totalRead != countToRead) {
                        //TODO: should this just be an assert?
                        throw new InvalidOperationException("Inconsistent count read from memoized buffer");
                    }
                    //increase our offset into buffer
                    offset += totalRead;
                    //reduce the number we need to read
                    count -= totalRead;
                }
                //if we have bytes to read
                if (count > 0) {
                    //read from the stream
                    int readBytes = _Stream.Read(buffer, offset, count);
                    //make note if we've past the end
                    if (readBytes == 0) PastEndOfStream = true;
                    //memoize the results (by construction, we should always be at the end of the stream)
                    Debug.Assert(_MemoizedData.Position == _MemoizedData.Length, "We are memoizing in the middle of the memory stream.");
                    _MemoizedData.Write(buffer, 0, readBytes);
                    //increment totalRead
                    totalRead += readBytes;
                }
                //update our global offset
                _Offset += totalRead;
                return totalRead;
            }

            /// <summary>
            /// This rewinds as far as we can.  This will be back to the last place we called <see cref="Flush()"/>
            /// </summary>
            public void RewindAll() {
                //TODO: reconcile types here
                _Rewound = (int)_MemoizedData.Length;
                _Offset -= _Rewound;
                _MemoizedData.Seek(0, SeekOrigin.Begin);
            }

            /// <summary>
            /// This rewinds a number of bytes from the current position
            /// </summary>
            /// <param name="offset"></param>
            public void Rewind(int offset) {
                if (offset == 0) return;
                if (offset > _MemoizedData.Length) {
                    //TODO: better message?
                    throw new InvalidOperationException("Cannot rewind further than memoized data");
                }
                _Rewound = offset;
                _Offset -= _Rewound;
                _MemoizedData.Seek(-offset, SeekOrigin.Current);
            }

            /// <summary>
            /// This flushes all the memoized data.  This is called when we know
            /// we won't need it (after we've successfully converted a record).
            /// </summary>
            public void Flush() {
                //if we're rewound, we need to remove a chunk from the beginning
                //of the stream.
                if (_Rewound > 0) {
                    //get rid of the already read memoizing buffer
                    //TODO: seems like there should be an easier way
                    var data = _MemoizedData.ToArray();
                    _MemoizedData.SetLength(0);
                    _MemoizedData.Write(data, data.Length - _Rewound, _Rewound);
                }
                //otherwise, we can just get rid of the whole thing
                else {
                    _MemoizedData.SetLength(0);
                }
            }

            /// <summary>
            /// The current offset
            /// </summary>
            public int Offset {
                get {
                    return _Offset;
                }
            }

            /// <summary>
            /// Indicates an operation has read beyond the end of the underlying stream
            /// </summary>
            public bool PastEndOfStream { get; private set; }

            /// <summary>
            /// Reads a single byte
            /// </summary>
            /// <returns>The read value, or -1 if we are past the end of the stream.</returns>
            public int ReadByte() {
                int value = -1;
                if (_Rewound > 0) {
                    value = _MemoizedData.ReadByte();
                    Debug.Assert(value != -1, "We read past the end memory stream.");
                    _Rewound--;
                }
                else {
                    value = _Stream.ReadByte();
                    if (value != -1) {
                        _MemoizedData.WriteByte((byte)value);
                    }
                }
                _Offset++;
                //make note if we have passed the end
                if (value == -1) PastEndOfStream = true;
                return value;
            }

            /// <summary>
            /// Convenient iterator to read bytes as a sequence.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<byte> ReadAsByteSequence() {
                while (true) {
                    var b = ReadByte();
                    if (b == -1) yield break;
                    yield return (byte)b;
                }
            }

            public byte[] DumpDataToCurrentOffset() {
                if (_Rewound > 0) {
                    var position = _MemoizedData.Position;
                    _MemoizedData.Seek(0, SeekOrigin.Begin);
                    var length = _MemoizedData.Length - _Rewound;
                    var bytes = new byte[length];
                    //TODO: reconcile types
                    _MemoizedData.Read(bytes, 0, (int)length);
                    _MemoizedData.Seek(position, SeekOrigin.Begin);
                    return bytes;
                }
                else return new byte[0];
            }
            public byte[] DumpRemainingData() {
                if (_Rewound > 0) {
                    var position = _MemoizedData.Position;
                    var bytes = new byte[_Rewound];
                    //TODO: reconcile types
                    _MemoizedData.Read(bytes, 0, _Rewound);
                    _Offset += _Rewound;
                    _Rewound = 0;
                    return bytes;
                }
                else return new byte[0];
            }
        }
    }
}
