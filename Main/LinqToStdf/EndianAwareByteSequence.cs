// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System.Buffers;

namespace LinqToStdf;

/// <summary>
/// This type makes it easier for the codegen to read values
/// from a sequence.
/// TODO: this goes away
/// </summary>
public readonly struct EndianAwareByteSequence
{
    readonly ReadOnlySequence<byte> _Sequence;
    readonly Endian _Endian;
    public EndianAwareByteSequence(ReadOnlySequence<byte> sequence, Endian endianness)
    {
        _Sequence = sequence;
        _Endian = endianness;
    }

    public ushort ReadUShort(uint offset = 0) => _Sequence.Slice(offset).ReadStdfUShort(_Endian);
}
