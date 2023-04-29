using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;

namespace LinqToStdf;

/// <summary>
/// This wraps a pipe reader and adds the ability for it to "munch" data. Munched data will affect what is returned from
/// a read (reads will start past munched data), but we can dip back into that data if needed.
/// Think of it as a cow and we can swallow data into a first stomach, but then regurgitate it and chew on it again if needed,
/// Or send it down into a second stomach where it can truly go away.
/// This replaces the rewinding stream concept we had in the old IO system.
/// </summary>
class MunchingPipeReader : PipeReader
{
    readonly PipeReader _Reader;
    /// <summary>
    /// The munched position is the position we've "munched" to.
    /// If not null, this is the starting position of any sequence returned by reads,
    /// But we can regurgitate back to the "real" advanced position if we want.
    /// </summary>
    SequencePosition? _MunchedPosition;
    /// <summary>
    /// This is the last read sequence. We need this in order to reason about
    /// positions since positions can't do anything on their own.
    /// </summary>
    ReadOnlySequence<byte>? _LastRead;
    /// <summary>
    /// The "file offset" of the beginning of the sequences currently returned by reads.
    /// </summary>
    public long Offset { get; private set; } = 0;

    public MunchingPipeReader(PipeReader reader)
    {
        _Reader = reader;
    }
    public override void AdvanceTo(SequencePosition consumed)
    {
        _Reader.AdvanceTo(consumed);
        //we need to see if we've advanced past the munched position
        UpdateStateForAdvanceOperation(consumed);
    }

    private void UpdateStateForAdvanceOperation(SequencePosition consumed)
    {
        Debug.Assert(_LastRead is not null);
        var consumedOffset = _LastRead.Value.GetOffset(consumed);
        if (_MunchedPosition is not null)
        {
            //munched position has to be within
            var munchedOffset = _LastRead.Value.GetOffset(_MunchedPosition.Value);
            if (consumedOffset >= munchedOffset)
            {
                //we've consumed up to or beyond what we've munched
                //clear our munch status
                _MunchedPosition = null;
                //Update our offset with the difference
                Offset += consumedOffset - munchedOffset;
            }
            //if we didn't pass the munched offset, our global offset didn't change.
        }
        else
        {
            //if we hadn't munched anything, just increase our offset
            Offset += consumedOffset;
        }
        //we need to update last read, because we may be holding a sequence where part of it has
        //been reclaimed.
        _LastRead = _LastRead.Value.Slice(consumed);

    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        _Reader.AdvanceTo(consumed, examined);
        //we need to see if we've advanced past the munched position
        UpdateStateForAdvanceOperation(consumed);
    }

    public override void CancelPendingRead() => _Reader.CancelPendingRead();

    public override void Complete(Exception? exception = null) => _Reader.Complete(exception);

    public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        var result = await _Reader.ReadAsync(cancellationToken);
        return MunchResultIfNecessary(result);
    }

    ReadResult MunchResultIfNecessary(ReadResult result)
    {
        //store last read
        _LastRead = result.Buffer;
        //if we've munched, we need to slice this to start at the munched position
        if (_MunchedPosition is not null)
        {
            //we try to ensure that munched position is only not null when it exists within the bounds of what is currently read
            //This is because:
            // * It can only be set from a SequencePosition, and that can only be gotten legitimately from a ROS
            // * We clear it when we have advanced up to or beyond it (up to is an optimization, I believe)
            // We can copy over the bools because they are perfectly relevant for current status.
            return new ReadResult(result.Buffer.Slice(_MunchedPosition.Value), result.IsCanceled, result.IsCompleted);
        }
        return result;
    }

    public override bool TryRead(out ReadResult result)
    {
        if (!_Reader.TryRead(out result)) return false;
        result = MunchResultIfNecessary(result);
        return true;
    }

    /// <summary>
    /// Returns the data that has been munched
    /// </summary>
    public ReadOnlySequence<byte> GetMunchedData()
    {
        if (_MunchedPosition is null) return ReadOnlySequence<byte>.Empty;
        if (!TryRead(out var currentResult))
        {
            throw new InvalidOperationException("Couldn't read current data");
        }
        return currentResult.Buffer.Slice(0, _MunchedPosition.Value);
    }

    /// <summary>
    /// "munches" to the position specified.
    /// This will cause "reads" to return starting at this location as
    /// if it was advanced, but we can "regurgitate" back into it if needed.
    /// </summary>
    /// <param name="munched"></param>
    public void MunchTo(SequencePosition munched)
    {
        Debug.Assert(_LastRead is not null);
        //attempt to calculate a new offset
        //NOTE: this could go positive or negative
        //if we haven't munched, the offset offset will be zero
        var originalOffsetOffset = 0L;
        if (_MunchedPosition is not null)
        {
            originalOffsetOffset = _LastRead.Value.GetOffset(_MunchedPosition.Value);
        }
        var newOffsetOffset = _LastRead.Value.GetOffset(munched);
        Offset += newOffsetOffset - originalOffsetOffset;
        //Do we need to validate this?
        _MunchedPosition = munched;

    }

    /// <summary>
    /// This advances the reader to the munched point, if set.
    /// Think of this as "flushing the munch"
    /// </summary>
    public void AdvanceToMunched()
    {
        if (_MunchedPosition is not null)
        {
            AdvanceTo(_MunchedPosition.Value);
        }
    }

    /// <summary>
    /// This will regurgitate all munched data
    /// </summary>
    public void Regurgitate()
    {
        Debug.Assert(_LastRead is not null);
        if (_MunchedPosition is not null)
        {
            var munchedOffset = _LastRead.Value.GetOffset(_MunchedPosition.Value);
            Offset -= munchedOffset;
            _MunchedPosition = null;
        }
    }
}
