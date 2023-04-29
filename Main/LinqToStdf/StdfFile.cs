// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LinqToStdf.Records;
using LinqToStdf.CompiledQuerySupport;
using System.Diagnostics;
using LinqToStdf.Indexing;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Buffers;

namespace LinqToStdf {

    /// <summary>
    /// The delegate type for methods that add filtering capability to an StdfRecord stream.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public delegate IEnumerable<StdfRecord> RecordFilter(IEnumerable<StdfRecord> input);

    /// <summary>
    /// The main instantiation point for using the library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is THE starting point for the library.
    /// You construct an StdfFile over a path, and then
    /// you get access to all the records via a basic
    /// IEnumerable pattern, or via some of the Linq-like
    /// extensions to IRecordContext.
    /// </para>
    /// <para>
    /// By default, StdfFile will be preloaded with converters
    /// suitable for parsing STDF V4 files.
    /// </para>
    /// </remarks>
    public sealed partial class StdfFile : IRecordContext {
        readonly IStdfStreamManager _StreamManager;
        readonly static internal RecordConverterFactory _V4ConverterFactory = new RecordConverterFactory();
        /// <summary>
        /// Exposes the ConverterFactory in use for parsing.
        /// This allows record un/converters to be registered.
        /// </summary>
        public RecordConverterFactory ConverterFactory { get; }

        Endian _Endian = Endian.Unknown;
        /// <summary>
        /// Exposes the Endianness of the file.  Will be "uknown" until
        /// after parsing has begun.
        /// </summary>
        public Endian Endian { get { return _Endian; } }

        long? _ExpectedLength = null;
        public long? ExpectedLength { get { return _ExpectedLength; } }

        RecordFilter _RecordFilter;
        bool _FiltersLocked;
        readonly object _ISLock = new object();
        private IIndexingStrategy? _IndexingStrategy = null;

        public IIndexingStrategy IndexingStrategy
        {
            get
            {
                //TODO: get this locking pattern right
                if (_IndexingStrategy == null)
                {
                    lock (_ISLock)
                    {
                        if (_IndexingStrategy == null)
                        {
                            _IndexingStrategy = new SimpleIndexingStrategy();
                        }
                    }
                }
                return _IndexingStrategy;
            }
            set
            {
                //TODO: prevent this from changing
                lock (_ISLock)
                {
                    EnsureFiltersUnlocked();
                    _IndexingStrategy = value;
                }
            }
        }

        private bool _ThrowOnFormatError = true;
        /// <summary>
        /// Indicates whether the library should throw in the case of
        /// a format error. (default=true)
        /// This can only be set before parsing has begun.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This allows both tolerant and strict applications
        /// to be written using the same library.
        /// </para>
        /// <para>
        /// If set to false, a <see cref="Records.FormatErrorRecord"/> will be
        /// pushed through the stream instead of throwing an exception,
        /// and if possible, parsing will continue.
        /// However, at this time, most format exceptions are unrecoverable.
        /// In the future, the parser might be more intelligent in the way it
        /// handles issues.
        /// </para>
        /// </remarks>
        public bool ThrowOnFormatError {
            get { return _ThrowOnFormatError; }
            set { EnsureFiltersUnlocked(); _ThrowOnFormatError = value; }
        }

        private void EnsureFiltersUnlocked() {
            if (_FiltersLocked) throw new InvalidOperationException(Resources.ErrorFiltersLocked);
        }

        /// <summary>
        /// Static constructor that will prepare a converter factory
        /// suitable for parsing STDF V4 records.
        /// </summary>
        static StdfFile() {
            StdfV4Specification.RegisterRecords(_V4ConverterFactory);
        }

        /// <summary>
        /// Constructs an StdfFile for the given path, suitable
        /// for parsing STDF V4 files.
        /// </summary>
        /// <param name="path">The path the the STDF file</param>
        public StdfFile(string path) : this(new DefaultFileStreamManager(path), false) { }

        /// <summary>
        /// Constructs an StdfFile for the given path, suitable
        /// for parsing STDF V4 files.
        /// </summary>
        /// <param name="path">The path the the STDF file</param>
        /// <param name="debug">True if you want the converter factory
        /// to emit to a dynamic assembly suitable for debugging IL generation.</param>
        public StdfFile(string path, bool debug) : this(new DefaultFileStreamManager(path), debug) { }

        /// <summary>
        /// Constructs an StdfFile for the given path, suitable
        /// for parsing STDF V4 files.
        /// </summary>
        /// <param name="streamManager">The STDF stream manager to use</param>
        public StdfFile(IStdfStreamManager streamManager) : this(streamManager, false) { }

        /// <summary>
        /// Constructs an StdfFile for the given path, suitable
        /// for parsing STDF V4 files.
        /// </summary>
        /// <param name="streamManager">The STDF stream manager to use</param>
        /// <param name="debug">True if you want the converter factory
        /// to emit to a dynamic assembly suitable for debugging IL generation.</param>
        public StdfFile(IStdfStreamManager streamManager, bool debug) : this(streamManager, debug, null) { }

        static RecordConverterFactory CreateConverterFactory(bool debug, RecordsAndFields? recordsAndFields)
        {
            if (debug || recordsAndFields != null)
            {
                var factory = new RecordConverterFactory(recordsAndFields) { Debug = debug };
                StdfV4Specification.RegisterRecords(factory);
                return factory;
            }
            else
            {
                return new RecordConverterFactory(_V4ConverterFactory);
            }
        }
        internal StdfFile(IStdfStreamManager streamManager, bool debug, RecordsAndFields? recordsAndFields)
            : this(streamManager, CreateConverterFactory(debug, recordsAndFields)) {
        }

        internal StdfFile(IStdfStreamManager streamManager, RecordConverterFactory recordConverterFactory)
        {
            _StreamManager = streamManager;
            _RecordFilter = BuiltInFilters.IdentityFilter;
            ConverterFactory = recordConverterFactory;
        }

        /// <summary>
        /// Adds a record filter to the stream.
        /// Note, if caching is enabled, filters will only be run on the
        /// first time through the file.  If filtering needs to occur
        /// all the time, turn off caching, or apply the filter to the results
        /// of the call to <see cref="GetRecords"/>.
        /// </summary>
        public void AddFilter(RecordFilter filter) {
            EnsureFiltersUnlocked();
            _RecordFilter = _RecordFilter.Chain(filter);
        }

        IEnumerable<StdfRecord> SetStdfFile(IEnumerable<StdfRecord> records) {
            foreach (var r in records) {
                r.StdfFile = this;
                yield return r;
            }
        }

        private RecordFilter GetBaseRecordFilter() {
            RecordFilter filter = SetStdfFile;
            return _ThrowOnFormatError ? filter.Chain(BuiltInFilters.ThrowOnFormatError) : filter;
        }

        private RecordFilter GetTopRecordFilter() {
            return IndexingStrategy.CacheRecords;
        }

        public IQueryable<StdfRecord> GetRecords()
        {
            return new Queryable<StdfRecord>(GetRecordsEnumerable(), IndexingStrategy.TransformQuery);
        }

        internal abstract class Queryable
        {

            internal static IQueryable Create(Type elementType, IEnumerable sequence, Func<Expression, Expression> transform)
            {
                return (IQueryable)(Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(new Type[] { elementType }), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { sequence, transform }, null) ?? throw new InvalidOperationException("Couldn't create Queryable instance."));
            }

            internal static IQueryable Create(Type elementType, Expression expression, Func<Expression, Expression> transform)
            {
                return (IQueryable)(Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(new Type[] { elementType }), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { expression, transform }, null) ?? throw new InvalidOperationException("Couldn't create Queryable instance."));
            }

            public abstract IEnumerable Enumerable { get; }
            public abstract Expression Expression { get; }
        }

        internal class Queryable<T> : Queryable, IOrderedQueryable<T>, IQueryProvider
        {
            EnumerableQuery<T>? _EnumerableQuery;
            readonly Func<Expression, Expression> _ExpressionTransform;
            readonly Expression _Expression;

            Queryable(Func<Expression, Expression> expressionTransform)
            {
                _ExpressionTransform = expressionTransform;
                //we need _Expression to be "assigned" before exiting this ctor to make nullability analysis happy, but we can't assign it
                //because we need to access "this". So, assign a dummy, non-null value and overwrite it.
                _Expression = Expression.Constant(null);
            }

            public Queryable(IEnumerable<T> enumerable, Func<Expression, Expression> expressionTransform)
                : this(expressionTransform)
            {
                _EnumerableQuery = new EnumerableQuery<T>(enumerable);
                _Expression = Expression.Constant(this);
            }

            public Queryable(Expression expression, Func<Expression, Expression> expressionTransform)
                : this(expressionTransform)
            {
                _Expression = expression;
            }

            [MemberNotNull(nameof(_EnumerableQuery))]
            void EnsureEnumerableQuery()
            {
                _EnumerableQuery ??= new EnumerableQuery<T>(new QueryableRewriter().Visit(_ExpressionTransform(_Expression)));
            }

            private IEnumerator<T> GetEnumeratorInternal()
            {
                EnsureEnumerableQuery();
                return ((IEnumerable<T>)this._EnumerableQuery).GetEnumerator();
            }

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return GetEnumeratorInternal();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumeratorInternal();
            }

            #endregion

            #region IQueryable Members

            public Type ElementType
            {
                get { return typeof(T); }
            }

            public override Expression Expression
            {
                get { return _Expression; }
            }

            public override IEnumerable Enumerable
            {
                get
                {
                    EnsureEnumerableQuery();
                    return _EnumerableQuery;
                }
            }

            public IQueryProvider Provider
            {
                get { return this; }
            }

            #endregion

            #region IQueryProvider Members

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new Queryable<TElement>(expression, _ExpressionTransform);
            }

            public IQueryable CreateQuery(Expression expression)
            {
                var type = TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type);
                if (type is null)
                {
                    throw new ArgumentException("Could not find type", "expression");
                }
                return Queryable.Create(type.GetGenericArguments()[0], expression, _ExpressionTransform);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                var queryable = _EnumerableQuery ?? new EnumerableQuery<T>(_Expression);
                return ((IQueryProvider)queryable).Execute<TResult>(new QueryableRewriter().Visit(_ExpressionTransform(expression)));
            }

            public object Execute(Expression expression)
            {
                var queryable = _EnumerableQuery ?? new EnumerableQuery<T>(_Expression);
                return ((IQueryProvider)queryable).Execute(new QueryableRewriter().Visit(_ExpressionTransform(expression))) ?? throw new InvalidOperationException("Execute returned null");
            }

            #endregion

            class QueryableRewriter : ExpressionVisitor
            {
                protected override Expression VisitConstant(ConstantExpression node)
                {
                    if (!(node.Value is Queryable queryable))
                    {
                        return base.VisitConstant(node);
                    }
                    if (queryable.Enumerable != null)
                    {
                        //TODO: get public type?
                        return base.VisitConstant(Expression.Constant(queryable.Enumerable));
                    }
                    return Visit(queryable.Expression);
                }
            }
        }

        /// <summary>
        /// Gets all the records in the file as a "stream" of StdfRecord object
        /// </summary>
        public IEnumerable<StdfRecord> GetRecordsEnumerable() {
            _FiltersLocked = true;
            foreach (var record in GetTopRecordFilter()(_RecordFilter(GetBaseRecordFilter()(InternalGetAllRecords())))) {
                if (record.GetType() == typeof(StartOfStreamRecord)) {
                    var sosRecord = (StartOfStreamRecord)record;
                    sosRecord.FileName = _StreamManager.Name;
                    if (_Endian == Endian.Unknown) {
                        _Endian = sosRecord.Endian;
                    }
                    _ExpectedLength = sosRecord.ExpectedLength;
                }
                yield return record;
            }
        }

        /// <summary>
        /// Consumes all the records in the StdfFile
        /// </summary>
        /// <remarks>
        /// This is useful if you have a set of filters set up, but no need for iterating
        /// through the records themselves at the end of the chain.
        /// </remarks>
        /// <returns>The count of records consumed at the end of the chain.</returns>
        public int Consume() {
            return this.GetRecords().Count();
        }

        #region Rewind and Seek APIs

        void EnterSeekMode() {
            _InSeekMode = true;
        }

        /// <summary>
        /// Rewinds the underlying stream to the just after the last record conversion
        /// that yielded a known record and enters "seek" mode, where the stream will 
        /// be assumed corrupt and searched for byte sequences that will identify the
        /// record boundaries. When the record stream is re-acquired, a CorruptDataRecord
        /// will be emitted containing the bytes between the last known good record and
        /// the newly-acquired record stream.
        /// Must be called within the context of live record reading.
        /// </summary>
        public void RewindAndSeek() {
            EnterSeekMode();
        }

        SeekAlgorithm _SeekAlgorithm = SeekAlgorithms.Identity;

        /// <summary>
        /// Adds a <see cref="SeekAlgorithm"/> to be used if <see cref="RewindAndSeek()"/> is called.
        /// </summary>
        /// <param name="algorithm"></param>
        public void AddSeekAlgorithm(SeekAlgorithm algorithm) {
            _SeekAlgorithm = _SeekAlgorithm.Chain(algorithm);
        }

        #endregion

        //indicates we are in seek mode
        bool _InSeekMode;

        async IAsyncEnumerable<StdfRecord> InternalGetAllRecords(CancellationToken cancellationToken)
        {
            //set this in case the last time we ended in seek mode
            _InSeekMode = false;
            using IStdfStreamScope streamScope = _StreamManager.GetScope();

            //TODO: should scopes give us pipe readers?
            var pipeReader = new MunchingPipeReader(PipeReader.Create(streamScope.Stream));
            var expectedLength = streamScope.Stream.Length;
            //read the FAR to get endianness
            var endian = Endian.Little;
            var farResult = await pipeReader.ReadAtLeastAsync(6, cancellationToken);
            var far = farResult.Buffer;
            //TODO: need to figure out 
            if (far.Length < 6)
            {
                yield return new StartOfStreamRecord { Endian = Endian.Unknown, ExpectedLength = expectedLength };
                yield return new FormatErrorRecord
                {
                    Message = Resources.FarReadError,
                    Recoverable = false
                };
                yield return new EndOfStreamRecord();
                yield break;
            }
            endian = far.GetByteAtIndex(4) < 2 ? Endian.Big : Endian.Little;
            var stdfVersion = far.GetByteAtIndex(5);
            var length = (endian == Endian.Little ? far.GetByteAtIndex(0) : far.GetByteAtIndex(1));
            if (length != 2)
            {
                yield return new StartOfStreamRecord { Endian = endian, ExpectedLength = expectedLength };
                yield return new FormatErrorRecord
                {
                    Message = Resources.FarLengthError,
                    Recoverable = false
                };
                yield return new EndOfStreamRecord { Offset = 2 };
                yield break;
            }
            //validate record type
            if (far.GetByteAtIndex(2) != 0)
            {
                yield return new StartOfStreamRecord { Endian = endian, ExpectedLength = expectedLength };
                yield return new FormatErrorRecord
                {
                    Offset = 2,
                    Message = Resources.FarRecordTypeError,
                    Recoverable = false
                };
                yield return new EndOfStreamRecord { Offset = 6 };
                yield break;
            }
            //validate record type
            if (far.GetByteAtIndex(3) != 10)
            {
                yield return new StartOfStreamRecord { Endian = endian, ExpectedLength = expectedLength };
                yield return new FormatErrorRecord
                {
                    Offset = 3,
                    Message = Resources.FarRecordSubTypeError,
                    Recoverable = false
                };
                yield return new EndOfStreamRecord { Offset = 3 };
                yield break;
            }
            //OK we're satisfied, let's go
            yield return new StartOfStreamRecord() { Endian = endian, ExpectedLength = expectedLength };
            yield return new LinqToStdf.Records.V4.Far() { CpuType = far.GetByteAtIndex(4), StdfVersion = far.GetByteAtIndex(5) };

            //flush the memory
            pipeReader.AdvanceTo(far.GetPosition(6));

            //now we have the FAR out of the way, and we can blow through the rest.
            while (true)
            {
                if (_InSeekMode)
                {
                    pipeReader.Regurgitate();
                    SequencePosition? foundPosition = null;
                    //create the callback algorithms use to indicate the found something
                    void BackupCallback(SequencePosition recordPosition) { foundPosition = recordPosition; }
                    var corruptOffset = pipeReader.Offset;
                    //set up the seek algorithms and consume the sequence.
                    var algorithm = _SeekAlgorithm(pipeReader.ReadAsByteSequence(), endian, BackupCallback);
                    algorithm.Count();
                    //when we get here, one of the algorithms has found the record stream,
                    //or we went to the end of the stream

                    var recoverable = false;
                    if (foundPosition is not null)
                    {
                        //someone found where we need to be, backup the number of bytes they suggest
                        pipeReader.MunchTo(foundPosition.Value);
                        recoverable = true;
                    }
                    //spit out the corrupt data
                    yield return new CorruptDataRecord()
                    {
                        CorruptData = pipeReader.GetMunchedData().ToArray(),
                        Offset = corruptOffset,
                        Recoverable = recoverable
                    };
                    //the data's gone out the door, so flush it
                    pipeReader.AdvanceToMunched();
                    if (!recoverable)
                    {
                        //we got to the end without finding anything
                        //spit out a format error
                        yield return new FormatErrorRecord()
                        {
                            Message = Resources.EOFInSeekMode,
                            Recoverable = false,
                            Offset = pipeReader.Offset
                        };
                        yield return new EndOfStreamRecord() { Offset = pipeReader.Offset };
                        yield break;
                    }
                    _InSeekMode = false;
                }
                //consume any munched data
                //TODO: should this be a configurable policy?
                //TODO: should we do this at all? it would mean if we get lost and don't seek,
                //we'll keep the rest of the file in memory until the end
                pipeReader.AdvanceToMunched();
                var position = pipeReader.Offset;
                //read a record header
                RecordHeader? header = await pipeReader.ReadHeaderAsync(endian, cancellationToken);

                //null means we couldn't read the full header
                //TODO: make sure this can only occur at EOS
                if (header is null)
                {
                    //if we were at the end of the stream, we should be able to read
                    //any available data with this
                    if (!pipeReader.TryRead(out var eofTestRead))
                    {
                        //TODO: can we hit this?
                        throw new InvalidOperationException("Encountered failure to read remaining data.");
                    }

                    //There are several situations:
                    // * the file ends before the header begins. Expect:
                    //   * IsComplete = true
                    //   * Length=0
                    // * The file ends somewhere in the header. Expect:
                    //   * IsComplete = true
                    //   * Length > 0
                    // * No other situation is expected here

                    //all our expected situations have IsCompleted == true
                    if (eofTestRead.IsCompleted)
                    {
                        //if there was any data left, dump a corrupt data record
                        if (eofTestRead.Buffer.Length > 0)
                        {
                            yield return new CorruptDataRecord()
                            {
                                Offset = position,
                                //TODO: allocation strategy?
                                CorruptData = eofTestRead.Buffer.ToArray(),
                                Recoverable = false
                            };
                            pipeReader.AdvanceTo(eofTestRead.Buffer.End);
                            yield return new FormatErrorRecord()
                            {
                                Message = Resources.EOFInHeader,
                                Recoverable = false,
                                Offset = position
                            };
                        }
                        //TODO: do this outside
                        await pipeReader.CompleteAsync();
                        yield return new EndOfStreamRecord() { Offset = position };
                        yield break;
                    }
                    else
                    {
                        //some other issue occurred in the mechanics of reading
                        //TODO: what conditions might get here?
                        throw new InvalidOperationException("Header could not be read, but we are not at end of stream.");
                    }
                }
                //at this point, we've read the header and are going to do a normal record read
                var contentsResult = await pipeReader.ReadAtLeastAsync(header.Value.Length);

                if (contentsResult.Buffer.Length < header.Value.Length)
                {
                    if (!contentsResult.IsCompleted) throw new InvalidOperationException("Record content could not be read, but we are not at end of stream.");
                    //we did not read the full record length, dump

                    yield return new CorruptDataRecord()
                    {
                        Offset = position,
                        CorruptData = contentsResult.Buffer.ToArray(),
                        Recoverable = false
                    };
                    pipeReader.AdvanceTo(contentsResult.Buffer.End);
                    yield return new FormatErrorRecord()
                    {
                        Message = Resources.EOFInRecordContent,
                        Recoverable = false,
                        Offset = position
                    };
                    //allow execution to continue, where we will find end of stream and produce EOS record
                }
                else
                {
                    var content = contentsResult.Buffer.Slice(0, header.Value.Length);
                    //TODO: we'd like to be able to run converters against the PipeReader-owned bytes, but we also
                    //need to be able to release UnknownRecords that don't convert. Ideally, we'd like a way to make that pattern clear,
                    //particularly since callers can participate in conversion.
                    var ur = new UnknownRecord(header.Value.RecordType, contentsResult.Buffer, endian) { Offset = position };
                    StdfRecord r = ConverterFactory.Convert(ur);
                    if (r is UnknownRecord unconvertedRecord)
                    {
                        r = ur.NormalizeForPublish();
                        //only munch UnknownRecords to allow for rewinding
                        pipeReader.MunchTo(contentsResult.Buffer.End);

                    }
                    else
                    {
                        //it converted, so update our last known position
                        //TODO: We should think about:
                        //* how to indicate corruption within the record boundaries
                        //* enabling filters to set the last known offset (to allow valid unknown records to pass through)
                        //  * This could possible be done by allowing filters access to Flush or the dump functionality.
                        pipeReader.AdvanceTo(contentsResult.Buffer.End);
                    }
                    r.Offset = position;
                    yield return r;
                }
            }
        }

        #region IRecordContext Members

        /// <summary>
        /// Implementation of <see cref="IRecordContext.StdfFile"/> to enable
        /// the extension methods. (returns this)
        /// </summary>
        StdfFile IRecordContext.StdfFile {
            get { return this; }
        }

        #endregion
    }
}
