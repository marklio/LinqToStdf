using LinqToStdf.Records;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToStdf
{
    public static class StdfRecordReader
    {
        public static async IAsyncEnumerable<StdfRecord> ReadRecordsAsync(StdfFile stdfFile, Func<Memory<byte>, CancellationToken, ValueTask<int>> dataReader, long? expectedLength, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var pipe = new Pipe();
            var reader = pipe.Reader;
            var fillTask = FillPipeAsync(pipe.Writer, dataReader, cancellationToken);

            Endian endian;

            //we need to read the FAR before we can make much progress on anything else
            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                //TODO: could we get bytes will a completed result?
                if (readResult.IsCompleted)
                {
                    //we didn't get enough data to even start
                    yield return new StartOfStreamRecord(stdfFile) { Endian = Endian.Unknown, ExpectedLength = expectedLength };
                    yield return new FormatErrorRecord(stdfFile)
                    {
                        Message = Resources.FarReadError,
                        Recoverable = false
                    };
                    yield return new EndOfStreamRecord(stdfFile);
                    reader.Complete();
                    yield break;
                };
                if (readResult.Buffer.Length < 6)
                {
                    //we need more to read the FAR, send it back to the kitchen
                    continue;
                }
                var buffer = readResult.Buffer;
                //we have at least the first 6 bytes to read.
                //make sure it is a well-formed FAR that we can base the rest of processing on
                var cpuType = buffer.Slice(4).FirstSpan[0];
                endian = cpuType < 2 ? Endian.Big : Endian.Little;
                var stdfVersion = buffer.Slice(5).FirstSpan[0];
                var length = buffer.ReadUInt16(endian);
                if (length != 2)
                {
                    yield return new StartOfStreamRecord(stdfFile) { Endian = endian, ExpectedLength = expectedLength };
                    yield return new FormatErrorRecord(stdfFile)
                    {
                        Message = Resources.FarLengthError,
                        Recoverable = false
                    };
                    yield return new EndOfStreamRecord(stdfFile) { Offset = 2 };
                    reader.Complete();
                    yield break;
                }
                var recordType = buffer.Slice(2).FirstSpan[0];
                var recordSubType = buffer.Slice(3).FirstSpan[0];
                if (recordType != 0)
                {
                    yield return new StartOfStreamRecord(stdfFile) { Endian = endian, ExpectedLength = expectedLength };
                    yield return new FormatErrorRecord(stdfFile)
                    {
                        Offset = 2,
                        Message = Resources.FarRecordTypeError,
                        Recoverable = false
                    };
                    yield return new EndOfStreamRecord(stdfFile) { Offset = 6 };
                    reader.Complete();
                    yield break;
                }
                if (recordSubType != 10)
                {
                    yield return new StartOfStreamRecord(stdfFile) { Endian = endian, ExpectedLength = expectedLength };
                    yield return new FormatErrorRecord(stdfFile)
                    {
                        Offset = 3,
                        Message = Resources.FarRecordSubTypeError,
                        Recoverable = false
                    };
                    yield return new EndOfStreamRecord(stdfFile) { Offset = 3 };
                    reader.Complete();
                    yield break;
                }
                reader.AdvanceTo(buffer.GetPosition(6));
                //OK we're satisfied, let's go
                yield return new StartOfStreamRecord(stdfFile) { Endian = endian, ExpectedLength = expectedLength };
                yield return new Records.V4.Far(stdfFile) { CpuType = cpuType, StdfVersion = stdfVersion };
                break;
            }

            long position = 6;
            //now, we can start reading records for real
            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                //TODO: could we have data in the completed case?
                if (readResult.IsCompleted) break;
                var buffer = readResult.Buffer;
                //throw it back if there's not enough to read the header
                if (buffer.Length < 4) continue;
                var header = buffer.ReadHeader(endian);
                //we know how much we need. Keep trying to get it
                reader.AdvanceTo(buffer.GetPosition(4));
                while (true)
                {
                    readResult = await reader.ReadAsync(cancellationToken);
                    if (readResult.IsCompleted) break;
                    if (readResult.Buffer.Length < header.Length) continue;
                }
                //OK, we have the whole record now
                var recordBuffer = readResult.Buffer.Slice(0, header.Length);
                //TODO: if this gets leaked in a real UnknownRecord, we need to transfer ownership of this buffer somehow
                //TODO: should we just copy it in that case?
                var converted = stdfFile.ConverterFactory.Convert(new UnknownRecord(stdfFile, header.RecordType, recordBuffer, endian, memoryOwner: null));
                if (converted is UnknownRecord)
                {
                    //TODO: we need to somehow indicate how/that ownership of the buffer is transferred.
                    // for now, we'll create a new copy
                    var memoryOwner = MemoryPool<byte>.Shared.Rent(checked((int)recordBuffer.Length));
                    converted = new UnknownRecord(stdfFile, header.RecordType, new ReadOnlySequence<byte>(memoryOwner.Memory), endian, memoryOwner);
                }
                converted.Offset = position;
                position += header.Length + 4;
                yield return converted;
            }

            await fillTask;
        }

        static async Task FillPipeAsync(PipeWriter writer, Func<Memory<byte>, CancellationToken, ValueTask<int>> dataReader, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentMemory = writer.GetMemory(128); //TODO: what min size buffer?
                var bytesRead = await dataReader(currentMemory, cancellationToken);
                if (bytesRead == 0) break;
                writer.Advance(bytesRead);
                var result = await writer.FlushAsync(cancellationToken);
                if (result.IsCompleted) break;
            }
            writer.Complete();
        }

    }
}
