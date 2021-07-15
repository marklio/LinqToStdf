﻿// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LinqToStdf.Records.V4;
using LinqToStdf.Records;

namespace LinqToStdf {

    /// <summary>
    /// Provides a number of built-in record filters.
    /// These can be added via <see cref="StdfFile.AddFilter"/>.
    /// </summary>
    public static class BuiltInFilters {

        /// <summary>
        /// This filter does nothing. (and has no n-based overhead)
        /// </summary>
        /// <remarks>
        /// This is useful if you need to return a <see cref="RecordFilter"/>,
        /// from a method, but it doesn't always need to do anything.
        /// </remarks>
        public static RecordFilter IdentityFilter { get { return (input) => input; } }

        #region CachingFilter implementation

        /// <summary>
        /// Provides the implementation for the caching filter
        /// </summary>
        class CachingFilterImpl {
            /// <summary>
            /// The cached records
            /// </summary>
            List<StdfRecord>? _Records;
            bool _Caching;

            /// <summary>
            /// Caches the records provided by input and passes them through.
            /// Subsequent calls return the contents of the cache
            /// </summary>
            public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input) {
                if (_Caching) {
                    throw new InvalidOperationException(Resources.CachingReEntrancy);
                }
                //cache the records
                if (_Records == null) {
                    _Caching = true;
                    _Records = new List<StdfRecord>();
                    foreach (var r in input) {
                        _Records.Add(r);
                    }
                    _Caching = false;
                }
                //provide the cached records
                foreach (var r in _Records) {
                    yield return r;
                }
            }
        }

        #endregion

        /// <summary>
        /// This filter implements caching for the StdfFile.
        /// It is internal because it is controlled via an option on StdfFile.
        /// </summary>
        internal static RecordFilter CachingFilter {
            get { return new CachingFilterImpl().Filter; }
        }

        //generics are great, BTW
        #region MissingBinSummaryFilter implementation

        /// <summary>
        /// Provides the implementation for synthesizing summary records from
        /// the site-specific records.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="BinSummaryRecord"/> to provide.</typeparam>
        class MissingBinSummaryFilterImpl<T> where T : BinSummaryRecord, new() {
            /// <summary>
            /// Indicates whether summary records are already in place
            /// </summary>
            bool _FoundSummary = false;

            /// <summary>
            /// The list of bin records
            /// </summary>
            readonly List<T> _Brs = new List<T>();
            /// <summary>
            /// Passes through the records provided by input,
            /// taking note of the bin records.  If no summary records
            /// are found, they are synthesized and passed through before the mrr.
            /// </summary>
            public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input) {
                foreach (var r in input) {
                    if (r.GetType() == typeof(T)) {
                        T br = (T)r;
                        _Brs.Add(br);
                        if (br.HeadNumber == 255) _FoundSummary = true;
                    }
                    else if (r.GetType() == typeof(Mrr) && !_FoundSummary) {
                        foreach (var gen in GenerateSummaries(r.Offset)) {
                            yield return gen;
                        }
                    }
                    yield return r;
                }
            }

            /// <summary>
            /// Generates the summary records
            /// </summary>
            private IEnumerable<StdfRecord> GenerateSummaries(long offset) {
                var q = from b in _Brs
                        group b by b.BinNumber into g
                        select new T()
                        {
                            Synthesized = true,
                            Offset = offset,
                            HeadNumber = 255,
                            SiteNumber = 0,
                            BinNumber = g.Key,
                            BinName = g.First().BinName,
                            BinPassFail = g.First().BinPassFail,
                            BinCount = (uint)g.Sum((b) => b.BinCount),
                        };

                foreach (var b in q) {
                    yield return b;
                }
            }
        }

        #endregion

        /// <summary>
        /// Reconstructs any missing "head 255" summary hbrs from site-specific hbrs.
        /// </summary>
        public static RecordFilter MissingHbrSummaryFilter {
            get { return new MissingBinSummaryFilterImpl<Hbr>().Filter; }
        }

        /// <summary>
        /// Reconstructs any missing "head 255" summary sbrs from site-specific sbrs.
        /// </summary>
        public static RecordFilter MissingSbrSummaryFilter {
            get { return new MissingBinSummaryFilterImpl<Sbr>().Filter; }
        }

        #region MissingPcrSummaryFilter implementation

        class MissingPcrSummaryFilterImpl {
            Pcr? _Summary = new Pcr()
            {
                Synthesized = true,
                HeadNumber = 255,
                SiteNumber = 0
            };
            public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input) {
                foreach (var r in input) {
                    if (r.GetType() == typeof(Pcr) && _Summary != null) {
                        Pcr p = (Pcr)r;
                        if (p.HeadNumber == 255) _Summary = null;
                        else {
                            _Summary.AbortCount = _Summary.AbortCount.Combine(p.AbortCount);
                            _Summary.FunctionalCount = _Summary.FunctionalCount.Combine(p.FunctionalCount);
                            _Summary.GoodCount = _Summary.GoodCount.Combine(p.GoodCount);
                            _Summary.RetestCount = _Summary.RetestCount.Combine(p.RetestCount);
                            _Summary.PartCount += p.PartCount;
                        }
                    }
                    else if (r.GetType() == typeof(Mrr) && _Summary != null) {
                        _Summary.Offset = r.Offset;
                        yield return _Summary;
                    }
                    yield return r;
                }
            }
        }

        #endregion

        /// <summary>
        /// Reconstructs a missing "head 255" summary pcrs from site-specific pcrs.
        /// </summary>
        public static RecordFilter MissingPcrSummaryFilter {
            get { return new MissingPcrSummaryFilterImpl().Filter; }
        }

        #region MissingTsrSummaryFilter implementation

        class MissingTsrSummaryFilterImpl {
            bool _FoundSummary;
            readonly List<Tsr> _Tsrs = new List<Tsr>();
            public IEnumerable<StdfRecord> Filter(IEnumerable<StdfRecord> input) {
                foreach (var r in input) {
                    if (r.GetType() == typeof(Tsr)) {
                        var tsr = (Tsr)r;
                        _Tsrs.Add(tsr);
                        if (tsr.HeadNumber == 255) _FoundSummary = true;
                    }
                    else if (r.GetType() == typeof(Mrr) && !_FoundSummary) {
                        foreach (var gen in GenerateSummaries(r.Offset)) {
                            yield return gen;
                        }
                    }
                    yield return r;
                }
            }

            private IEnumerable<StdfRecord> GenerateSummaries(long offset) {
                var q = from t in _Tsrs
                        group t by t.TestNumber into g
                        select new Tsr()
                        {
                            Synthesized = true,
                            Offset = offset,
                            HeadNumber = 255,
                            SiteNumber = 0,
                            TestNumber = g.Key,
                            TestName = g.First().TestName,
                            TestLabel = g.First().TestLabel,
                            AlarmCount = (uint?)g.Sum((t) => t.AlarmCount),
                            ExecutedCount = (uint?)g.Sum((t) => t.ExecutedCount),
                            FailedCount = (uint?)g.Sum((t) => t.FailedCount),
                            SequencerName = g.First().SequencerName,
                            TestMax = g.Max((t) => t.TestMax),
                            TestMin = g.Min((t) => t.TestMin),
                            TestSum = g.Sum((t) => t.TestSum),
                            TestSumOfSquares = g.Sum((t) => t.TestSumOfSquares),
                            TestTime = g.Sum((t) => t.TestTime),
                            TestType = g.First().TestType,
                        };

                foreach (var b in q) {
                    yield return b;
                }
            }
        }

        #endregion

        /// <summary>
        /// Reconstructs any missing "head 255" summary tsrs from site-specific tsrs.
        /// </summary>
        public static RecordFilter MissingTsrSummaryFilter {
            get { return new MissingTsrSummaryFilterImpl().Filter; }
        }

        /// <summary>
        /// Reconstructs any missing "head 255" bin summaries (hbr/sbr) from the site-specific records.
        /// </summary>
        public static RecordFilter MissingBinSummaryFilter {
            get { return MissingHbrSummaryFilter.Chain(MissingSbrSummaryFilter); }
        }

        /// <summary>
        /// Reconstructs any missing "head 255" summaries (hbr/sbr/pcr/tsr) from the site-specific records.
        /// </summary>
        public static RecordFilter MissingSummaryFilter {
            get { return MissingPcrSummaryFilter.Chain(MissingBinSummaryFilter.Chain(MissingTsrSummaryFilter)); }
        }

        static IEnumerable<StdfRecord> ThrowOnFormatErrorFilter(IEnumerable<StdfRecord> input) {
            foreach (var r in input) {
                if (r is FormatErrorRecord err) { throw err.ToException(); }
                yield return r;
            }
        }

        /// <summary>
        /// If any format errors are encountered, this will throw
        /// </summary>
        internal static RecordFilter ThrowOnFormatError { get { return ThrowOnFormatErrorFilter; } }

        #region V4ContentSpec implementation

        /// <summary>
        /// node in the state machine representation
        /// </summary>
        class RecordState {
            public string Message = Resources.V4ContentState_Unknown;
            public Func<StdfRecord, bool> ShouldTransition = r => throw new InvalidOperationException("Should never call this.");
            public readonly List<RecordState> Routes = new();
        }

        /// <summary>
        /// These records are not allowed after the initial sequence, or before the Mrr
        /// </summary>
        static readonly HashSet<RuntimeTypeHandle> _InitialSequenceSet = new HashSet<RuntimeTypeHandle>() { typeof(Far).TypeHandle, typeof(Atr).TypeHandle, typeof(Mir).TypeHandle, typeof(Rdr).TypeHandle, typeof(Sdr).TypeHandle, typeof(EndOfStreamRecord).TypeHandle };

        /// <summary>
        /// Uses a state machine to enforce the V4 content spec (initial sequence and mrr at the end)
        /// </summary>
        static IEnumerable<StdfRecord> V4ContentSpecFilter(IEnumerable<StdfRecord> input) {
            #region States
            // Build up the various states that describe the V4 content spec.

            var eofState = new RecordState() {
                               Message = Resources.V4ContentState_AtEOF,
                               ShouldTransition = (r) => r.GetType() == typeof(EndOfStreamRecord),
                           };
            var mrrState = new RecordState() {
                               Message = Resources.V4ContentState_AfterMrr,
                               ShouldTransition = (r) => r.GetType() == typeof(Mrr),
                               Routes = { eofState } //we only expect EOF from here
                           };
            var bodyState = new RecordState() {
                                Message = Resources.V4ContentState_StdfBody,
                                //anything that's not in the initial sequence (or EOS)
                                ShouldTransition = (r) => !_InitialSequenceSet.Contains(r.GetType().TypeHandle),
                                Routes = {mrrState},
                            };
            bodyState.Routes.Add(bodyState);

            var sdrState = new RecordState() {
                               Message = Resources.V4ContentState_AfterSdr,
                               ShouldTransition = (r) => r.GetType() == typeof(Sdr)
                           };
            sdrState.Routes.Add(sdrState);
            sdrState.Routes.Add(bodyState);
            var rdrState = new RecordState() {
                               Message = Resources.V4ContentState_AfterRdr,
                               ShouldTransition = (r) => r.GetType() == typeof(Rdr),
                               Routes = { sdrState, bodyState }
                           };
            var mirState = new RecordState() {
                               Message = Resources.V4ContentState_AfterMir,
                               ShouldTransition = (r) => r.GetType() == typeof(Mir),
                               Routes = { rdrState, sdrState, bodyState }
                           };
            var atrState = new RecordState() {
                               Message = Resources.V4ContentState_AfterAtr,
                               ShouldTransition = (r) => r.GetType() == typeof(Atr),
                           };
            atrState.Routes.Add(atrState);
            atrState.Routes.Add(mirState);
            var farState = new RecordState() {
                               Message = Resources.V4ContentState_AfterFar,
                               ShouldTransition = (r) => r.GetType() == typeof(Far),
                               Routes = { atrState, mirState }
                           };
            var sofState = new RecordState() {
                               Message = Resources.V4ContentState_AtSOF,
                               ShouldTransition = (r) => r.GetType() == typeof(StartOfStreamRecord),
                               Routes = { farState }
                           };

            #endregion

            //we'll start in a pre-far state
            var currentState = new RecordState() {
                                   Message = Resources.V4ContentState_BeforeSOF,
                                   Routes = { sofState }
                               };
            foreach (var r in input) {
                bool transitioned = false;
                foreach (var state in currentState.Routes ?? throw new InvalidOperationException("No routes available in state machine")) {
                    if (state.ShouldTransition(r)) {
                        transitioned = true;
                        currentState = state;
                        break;
                    }
                }
                //TODO: does IsWritable prevent informational and error records from violating the content spec (we want that)?
                if (!transitioned && r.IsWritable) {
                    yield return new V4ContentErrorRecord() { Offset = r.Offset, Message = string.Format(Resources.InitialSequenceError, r.GetType().Name, currentState.Message) };
                }
                yield return r;
            }
        }

        #endregion

        /// <summary>
        /// Enforces the record ordering rules of the V4 spec.  It will push V4ContentErrorRecord's through the stream
        /// for any violations.
        /// </summary>
        public static RecordFilter V4ContentSpec { get { return V4ContentSpecFilter; } }

        static IEnumerable<StdfRecord> ThrowOnV4ContentErrorFilter(IEnumerable<StdfRecord> input) {
            foreach (var r in input) {
                if (r is V4ContentErrorRecord err) { throw err.ToException(); }
                yield return r;
            }
        }

        /// <summary>
        /// Will throw if any V4ContentErrorRecords are encountered.
        /// </summary>
        public static RecordFilter ThrowOnV4ContentError { get { return ThrowOnV4ContentErrorFilter; } }

        #region RepairMissingMrr implementation

        //TODO: decide whether this should react to a special ErrorRecord, or EndOfStream
        // This boils down to whether spec violations should be repaired before or after validation.
        // Up to this point, repairs have not been the result of violations, so this is the first case.
        static IEnumerable<StdfRecord> RepairMissingMrrImpl(IEnumerable<StdfRecord> input)
        {
            var foundMrr = false;
            foreach (var r in input)
            {
                if (r.GetType() == typeof(Mrr)) foundMrr = true;
                else if (r.GetType() == typeof(EndOfStreamRecord) && !foundMrr)
                {
                    yield return new Mrr() { Synthesized = true, Offset = r.Offset };
                }
                yield return r;
            }
        }

        #endregion

        /// <summary>
        /// Will inject an mrr at the end of the stream if there wasn't one.
        /// </summary>
        /// <remarks>
        /// This is useful for making sure that other synthesized records
        /// that trigger off of MRR actually occur.
        /// </remarks>
        public static RecordFilter RepairMissingMrr { get { return RepairMissingMrrImpl; } }

        #region ExpectNoUnknownRecords implementation

        static IEnumerable<StdfRecord> ExpectOnlyKnownRecordsImpl(IEnumerable<StdfRecord> records) {
            foreach (var r in records) {
                if (r.GetType() == typeof(UnknownRecord)) {
                    if (r.StdfFile is null)
                    {
                        throw new InvalidOperationException("UnknownRecord encountered, but no StdfFile is available to attempt recovery.");
                    }
                    r.StdfFile.RewindAndSeek();
                }
                else {
                    yield return r;
                }
            }
        }

        #endregion

        /// <summary>
        /// Populates the "optional" PTR fields with the defaults provided by the first PTR record for the test.
        /// </summary>
        /// <remarks>
        /// See the V4 STDF spec section on PTRs, "Notes on Specific Fields", "Default Data"
        /// </remarks>
        public static RecordFilter PopulatePtrFieldsWithDefaults { get { return PopulatePtrFieldsWithDefaultsImpl; } }

        #region PopulatePtrFieldsWithDefaults implementation

        static IEnumerable<StdfRecord> PopulatePtrFieldsWithDefaultsImpl(IEnumerable<StdfRecord> records) {
            var firstPtrs = new Dictionary<uint, Ptr>();
            foreach (var r in records) {
                if (r.GetType() == typeof(Ptr)) {
                    var ptr = (Ptr)r;
                    if (firstPtrs.TryGetValue(ptr.TestNumber, out var first)) {
                        if (ptr.ResultScalingExponent == null)
                            ptr.ResultScalingExponent = first.ResultScalingExponent;
                        if (ptr.LowLimitScalingExponent == null)
                            ptr.LowLimitScalingExponent = first.LowLimitScalingExponent;
                        if (ptr.HighLimitScalingExponent == null)
                            ptr.HighLimitScalingExponent = first.HighLimitScalingExponent;
                        if (ptr.LowLimit == null)
                            ptr.LowLimit = first.LowLimit;
                        if (ptr.HighLimit == null)
                            ptr.HighLimit = first.HighLimit;
                        if (ptr.Units == null)
                            ptr.Units = first.Units;
                        if (ptr.ResultFormatString == null)
                            ptr.ResultFormatString = first.ResultFormatString;
                        if (ptr.LowLimitFormatString == null)
                            ptr.LowLimitFormatString = first.LowLimitFormatString;
                        if (ptr.HighLimitFormatString == null)
                            ptr.HighLimitFormatString = first.HighLimitFormatString;
                        if (ptr.LowSpecLimit == null)
                            ptr.LowSpecLimit = first.LowSpecLimit;
                        if (ptr.HighSpecLimit == null)
                            ptr.HighSpecLimit = first.HighSpecLimit;
                    }
                    else {
                        firstPtrs[ptr.TestNumber] = ptr;
                    }
                }
                yield return r;
            }
        }

        #endregion

        /// <summary>
        /// This filter will invoke the <see cref="StdfFile.RewindAndSeek">"rewind and seek"</see> functionality
        /// if any unknown records are encountered.
        /// </summary>
        public static RecordFilter ExpectOnlyKnownRecords { get { return ExpectOnlyKnownRecordsImpl; } }
    }
}
