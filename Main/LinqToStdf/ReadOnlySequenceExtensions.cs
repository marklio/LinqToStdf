using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf
{
    static class ReadOnlySequenceExtensions
    {
        public static ushort ReadUInt16(this ReadOnlySequence<byte> sequence, Endian endian)
        {
            sequence = sequence.Slice(0, 2);
            if (sequence.IsSingleSegment) return endian switch
            {
                Endian.Big => BinaryPrimitives.ReadUInt16BigEndian(sequence.FirstSpan),
                Endian.Little => BinaryPrimitives.ReadUInt16LittleEndian(sequence.FirstSpan),
                _ => throw new InvalidOperationException($"Invalid endianness {endian}")
            };
            //fall back to reading from multiple spans
            var firstByte = sequence.FirstSpan[0];
            var secondByte = sequence.Slice(1).FirstSpan[0];
            return endian switch
            {
                Endian.Big => (ushort)((firstByte << 8) & secondByte),
                Endian.Little => (ushort)((secondByte << 8) & firstByte),
                _ => throw new InvalidOperationException($"Invalid endianness {endian}")
            };
        }

        public static RecordHeader ReadHeader(this ReadOnlySequence<byte> sequence, Endian endian)
        {
            sequence = sequence.Slice(0, 4);
            return new RecordHeader(sequence.ReadUInt16(endian), new RecordType(sequence.Slice(2).FirstSpan[0], sequence.Slice(3).FirstSpan[0]));
        }
    }
}
