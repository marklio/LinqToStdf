using System.Buffers;
using System.IO.Pipelines;

namespace LinqToStdf;

/// <summary>
/// This wraps a pipe reader and adds the ability for it to "munch" data. Munched data will affect what is returned from
/// a read, but 
/// </summary>
class MunchingPipeReader : PipeReader
{
    readonly PipeReader _Reader;
    SequencePosition? _MunchedPosition;
    public MunchingPipeReader(PipeReader reader)
    {
        _Reader = reader;
    }
    public override void AdvanceTo(SequencePosition consumed) {
        _Reader.AdvanceTo(consumed);
        if (_MunchedPosition is null || _MunchedPosition.Value.GetInteger() < consumed.GetInteger())
        {
            _MunchedPosition = consumed;
        }
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _Reader.AdvanceTo(consumed, examined);
        if (_MunchedPosition is null || _MunchedPosition.Value.GetInteger() < consumed.GetInteger())
        {
            _MunchedPosition = consumed;
        }
    }

    public override void CancelPendingRead() => _Reader.CancelPendingRead();

    public override void Complete(Exception? exception = null) => _Reader.Complete(exception);

    public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        var result = await _Reader.ReadAsync(cancellationToken);
        if (_MunchedPosition is not null)
        {
            if (_MunchedPosition.Value.GetInteger() > result.Buffer.Start.GetInteger() && _MunchedPosition.Value.GetInteger() < result.Buffer.End.GetInteger())
            {
                return new ReadResult(result.Buffer.Slice(_MunchedPosition.Value), result.IsCanceled, result.IsCompleted);
            }
        }
        _MunchedPosition = result.Buffer.Start;
        return result;
    }

    public override bool TryRead(out ReadResult result)
    {
        if (!_Reader.TryRead(out result)) return false;
        if (_MunchedPosition is not null)
        {
            if (_MunchedPosition.Value.GetInteger() > result.Buffer.Start.GetInteger() && _MunchedPosition.Value.GetInteger() < result.Buffer.End.GetInteger())
            {
                //TODO: can we slice this in such a way that we need to change the result?
                result =new ReadResult(result.Buffer.Slice(_MunchedPosition.Value), result.IsCanceled, result.IsCompleted);
            }
        }      
        return true;
    }

    public ReadOnlySequence<byte> GetMunchedData()
    {
        if (_MunchedPosition is null) return ReadOnlySequence<byte>.Empty;
        if (!TryRead(out var currentResult))
        {
            throw new InvalidOperationException("Couldn't read current data");
        }
        return currentResult.Buffer.Slice(0, _MunchedPosition.Value);
    }

    public void MunchTo(SequencePosition munched)
    {
        _MunchedPosition = munched;
    }

    public void AdvanceMunched()
    {
        if (_MunchedPosition is not null)
        {
            AdvanceTo(_MunchedPosition.Value);
        }
    }

    public void Regurgitate()
    {
        _MunchedPosition = null;
    }
}
