using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LinqToStdf.Records;
using LinqToStdf.CompiledQuerySupport;

#if SILVERLIGHT
using System.Windows.Controls;
#endif

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
    public sealed class StdfFile : IRecordContext {
#if SILVERLIGHT
        FileDialogFileInfo _FileInfo;
#endif
        string _Path;
        static internal RecordConverterFactory _V4ConverterFactory = new RecordConverterFactory();
        RecordConverterFactory _ConverterFactory;
        /// <summary>
        /// Exposes the ConverterFactory in use for parsing.
        /// This allows record un/converters to be registered.
        /// </summary>
        public RecordConverterFactory ConverterFactory {
            get { return _ConverterFactory; }
        }

        Endian _Endian = Endian.Unknown;
        /// <summary>
        /// Exposes the Endianness of the file.  Will be "uknown" until
        /// after parsing has begun.
        /// </summary>
        public Endian Endian { get { return _Endian; } }

        RecordFilter _RecordFilter = null;
        RecordFilter _CachingFilter = BuiltInFilters.CachingFilter;
        bool _FiltersLocked;

        private bool _EnableCaching = true;
        /// <summary>
        /// Indicates whether caching is enabled. (default=true)
        /// This can only be set before parsing has begun.
        /// </summary>
        /// <remarks>
        /// Caching enables multiple queries without reparsing the file.
        /// Naturally, there is memory overhead associated with this.
        /// The default is to enable caching, which will suit the primary scenarios
        /// better.  There will be scenarios where caching is not desirable.
        /// </remarks>
        public bool EnableCaching {
            get { return _EnableCaching; }
            set { EnsureFiltersUnlocked(); _EnableCaching = value; }
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
            if (_FiltersLocked) throw new InvalidOperationException("You cannot modify the filters after records have been read.");
        }

        /// <summary>
        /// Static constructor that will prepare a converter factory
        /// suitable for parsing STDF V4 records.
        /// </summary>
        static StdfFile() {
            StdfV4Specification.RegisterRecords(_V4ConverterFactory);
        }

#if SILVERLIGHT
        public StdfFile(FileDialogFileInfo fileInfo)
            : this(null, false, null) {
            _FileInfo = fileInfo;
        }
#endif

        /// <summary>
        /// Constructs an StdfFile for the given path, suitable
        /// for parsing STDF V4 files.
        /// </summary>
        /// <param name="path">The path the the STDF file</param>
        public StdfFile(string path) : this(path, false) { }

        /// <summary>
        /// Constructs an StdfFile for the given path, suitable
        /// for parsing STDF V4 files.
        /// </summary>
        /// <param name="path">The path the the STDF file</param>
        /// <param name="debug">True if you want the converter factory
        /// to emit to a dynamic assembly suitable for debugging IL generation.</param>
        public StdfFile(string path, bool debug) : this(path, debug, null) { }

        internal StdfFile(string path, bool debug, RecordsAndFields recordsAndFields)
            : this(path, PrivateImpl.None) {
            if (debug || recordsAndFields != null) {
                _ConverterFactory = new RecordConverterFactory(recordsAndFields) { Debug = debug };
                StdfV4Specification.RegisterRecords(_ConverterFactory);
            }
            else {
                _ConverterFactory = new RecordConverterFactory(_V4ConverterFactory);
            }
        }

        internal StdfFile(string path, RecordConverterFactory rcf)
            : this(path, PrivateImpl.None) {
            _ConverterFactory = rcf;
        }

        private StdfFile(string path, PrivateImpl pi) {
            _Path = path;
            _RecordFilter = BuiltInFilters.IdentityFilter;
        }

        enum PrivateImpl { None = 0 }

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
            return _EnableCaching ? _CachingFilter : BuiltInFilters.IdentityFilter;
        }

        /// <summary>
        /// Gets all the records in the file as a "stream" of StdfRecord object
        /// </summary>
        public IEnumerable<StdfRecord> GetRecords() {
            _FiltersLocked = true;
            foreach (var record in GetTopRecordFilter()(_RecordFilter(GetBaseRecordFilter()(InternalGetAllRecords())))) {
                if (record.GetType() == typeof(StartOfStreamRecord)) {
					var sosRecord = (StartOfStreamRecord)record;
                    sosRecord.FileName = Path.GetFileName(_Path);
                    if (_Endian == Endian.Unknown) {
                        _Endian = sosRecord.Endian;
                    }
                }
                yield return record;
            }
        }

        #region Rewind and Seek APIs

        void RewindToLastKnownOffset() {
            RewindToOffset(_LastKnownOffset);
        }

        void RewindToOffset(long offset) {
            //TODO: validate this arg
            //TODO: ensure we're in a call to InternalGetAllRecords
            _Stream.Seek(offset, SeekOrigin.Begin);
        }

        void EnterSeekMode() {
            _InSeekMode = true;
        }

        /// <summary>
        /// Rewinds the underlying stream to the just after the last record conversion
        /// that yielded a known record and enters "seek" mode, where the stream will 
        /// be assumed corrupt and searched for byte sequences that will identify the
        /// record boundarie. When the record stream is re-aquired, a CorruptDataRecord
        /// will be emitted containing the bytes between the last known good record and
        /// the newly-aquired record stream.
        /// Must be called within the context of live record reading.
        /// </summary>
        public void RewindAndSeek() {
            RewindToLastKnownOffset();
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
        //indicates a pending rewind command
        long _RewindTo = -1;
        //indicates the position in the stream where we stopped getting known records
        long _LastKnownOffset;

        Stream _Stream;

        IEnumerable<byte> ReadStreamAsByteSequence() {
            while (true) {
                var b = _Stream.ReadByte();
                if (b == -1) break;
                yield return (byte)b;
            }
        }

        Stream GetStream() {
#if SILVERLIGHT
            if (_FileInfo != null) {
                return _FileInfo.OpenRead();
            }
            else {
#else
            {
#endif
                return new FileStream(_Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
        }

        IEnumerable<StdfRecord> InternalGetAllRecords() {
            _LastKnownOffset = 0;
            using (_Stream = GetStream()) {
                //read the FAR to get endianness
                var endian = Endian.Little;
                var far = new byte[6];
                if (_Stream.Read(far, 0, 6) < 6) {
                    yield return new StartOfStreamRecord() { Endian = Endian.Unknown };
                    yield return new FormatErrorRecord() {
                                     Message = "The FAR could not be fully read.",
                                     Recoverable = false
                                 };
                    yield return new EndOfStreamRecord();
                    yield break;
                }
                switch (far[4]) {
                    case 0:
                    case 1:
                        endian = Endian.Big;
                        break;
                    default:
                        endian = Endian.Little;
                        break;
                }

                var stdfVersion = far[5];
                var length = (endian == Endian.Little ? far[0] : far[1]);
                if (length != 2) {
                    yield return new StartOfStreamRecord() { Endian = endian };
                    yield return new FormatErrorRecord() {
                                     Message = "The FAR record length was not reported as 2.",
                                     Recoverable = false
                                 };
                    yield return new EndOfStreamRecord();
                    yield break;
                }
                //validate record type
                if (far[2] != 0) {
                    yield return new StartOfStreamRecord() { Endian = endian };
                    yield return new FormatErrorRecord() {
                                     Message = "The FAR record type was not reported as 0.",
                                     Recoverable = false
                                 };
                    yield return new EndOfStreamRecord();
                    yield break;
                }
                //validate record type
                if (far[3] != 10) {
                    yield return new StartOfStreamRecord() { Endian = endian };
                    yield return new FormatErrorRecord() {
                                     Message = "The FAR record sub-type was not reported as 10.",
                                     Recoverable = false
                                 };
                    yield return new EndOfStreamRecord();
                    yield break;
                }
                //OK we're satisfied, let's go
                yield return new StartOfStreamRecord() { Endian = endian };
                yield return new LinqToStdf.Records.V4.Far() { CpuType = far[4], StdfVersion = far[5] };


                //create a reader
                BinaryReader reader = new BinaryReader(_Stream, endian, false);

                //let's keep track of the last "known" good position.
                //That is, the last time we got a record we recognized.
                //We'll use this to "rewind" and look for known records.
                _LastKnownOffset = _Stream.Position;

                //now we have the FAR out of the way, and we can blow through the rest.
                while (true) {
                    if (_RewindTo != -1) {
                        //TODO: validate we went where we asked?
                        _Stream.Seek(_RewindTo, SeekOrigin.Begin);
                        _RewindTo = -1;
                    }
                    if (_InSeekMode) {
                        int backup = -1;
                        BackUpCallback backupCallback = (bytes) => { backup = bytes; };
                        var corruptData = new MemoryStream();
                        var corruptOffset = _Stream.Position;
                        //set up the seek algorithms and consume the sequence.
                        var algorithm = _SeekAlgorithm(ReadStreamAsByteSequence(), endian, backupCallback);
                        foreach (var b in algorithm) {
                            corruptData.WriteByte(b);
                        }
                        //when we get here, one of the algorithms has found the record stream,
                        //or we went to the end of the stream

                        if (backup != -1) {
                            //someone found where we need to be, backup the number of bytes they suggest
                            _Stream.Seek(-backup, SeekOrigin.Current);
                            corruptData.SetLength(corruptData.Length - backup);
                            //spit out the corrupt data
                            yield return new CorruptDataRecord() {
                                             CorruptData = corruptData.ToArray(),
                                             Offset = corruptOffset,
                                             Recoverable = true
                                         };
                        }
                        else {
                            //we got to the end without finding anything
                            //spit out the corrupt data
                            yield return new CorruptDataRecord() {
                                             CorruptData = corruptData.ToArray(),
                                             Offset = corruptOffset,
                                             Recoverable = false
                                         };
                            //spit out a format error
                            yield return new FormatErrorRecord() {
                                             Message = "End of Stream encountered while in seek mode.",
                                             Recoverable = false,
                                             Offset = _Stream.Position
                                         };
                            yield return new EndOfStreamRecord() { Offset = _Stream.Position };
                            yield break;
                        }
                        _InSeekMode = false;
                    }
                    var position = _Stream.Position;
                    if (reader.AtEndOfStream) break;
                    RecordHeader? header = null;
                    try {
                        header = reader.ReadHeader();
                    }
                    //swallow EOS
                    catch (EndOfStreamException) { }
                    if (header == null) {
                        yield return new FormatErrorRecord() {
                                         Message = "End of Stream encountered while reading record header.",
                                         Recoverable = false,
                                         Offset = position
                                     };
                        break;
                    }
                    var contents = new byte[header.Value.Length];
                    int read = _Stream.Read(contents, 0, contents.Length);
                    if (read < contents.Length) {
                        yield return new FormatErrorRecord() {
                                         Message = "End of Stream encountered while reading record contents.",
                                         Recoverable = false,
                                         Offset = position
                                     };
                    }
                    else {
                        var ur = new UnknownRecord(header.Value.RecordType, contents, endian) { Offset = position };
                        StdfRecord r = _ConverterFactory.Convert(ur);
                        if (r.GetType() != typeof(UnknownRecord)) {
                            //it converted, so update our last known position
                            _LastKnownOffset = _Stream.Position;
                        }
                        r.Offset = position;
                        yield return r;
                    }
                }
                yield return new EndOfStreamRecord() { Offset = _Stream.Position };
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
