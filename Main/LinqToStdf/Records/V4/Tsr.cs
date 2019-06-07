// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"),
    StringFieldLayout(FieldIndex = 2, Length = 1, MissingValue = " ", RecordProperty = "TestType"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(uint), RecordProperty = "TestNumber"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "ExecutedCount"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "FailedCount"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "AlarmCount"),
    StringFieldLayout(FieldIndex = 7, IsOptional = true, RecordProperty = "TestName"),
    StringFieldLayout(FieldIndex = 8, IsOptional = true, RecordProperty = "SequencerName"),
    StringFieldLayout(FieldIndex = 9, IsOptional = true, RecordProperty = "TestLabel"),
    FieldLayout(FieldIndex = 10, IsOptional = true, FieldType = typeof(byte)),
    FlaggedFieldLayout(FieldIndex = 11, FieldType = typeof(float), IsOptional = true, FlagIndex = 10, FlagMask = (byte)0x04, RecordProperty = "TestTime"),
    FlaggedFieldLayout(FieldIndex = 12, FieldType = typeof(float), IsOptional = true, FlagIndex = 10, FlagMask = (byte)0x01, RecordProperty = "TestMin"),
    FlaggedFieldLayout(FieldIndex = 13, FieldType = typeof(float), IsOptional = true, FlagIndex = 10, FlagMask = (byte)0x02, RecordProperty = "TestMax"),
    FlaggedFieldLayout(FieldIndex = 14, FieldType = typeof(float), IsOptional = true, FlagIndex = 10, FlagMask = (byte)0x10, RecordProperty = "TestSum"),
    FlaggedFieldLayout(FieldIndex = 15, FieldType = typeof(float), IsOptional = true, FlagIndex = 10, FlagMask = (byte)0x20, RecordProperty = "TestSumOfSquares")]
    public class Tsr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(10, 30); }
        }

        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        /// <summary>
        /// Known values are: P, F, M
        /// </summary>
        public string TestType { get; set; }
        public uint TestNumber { get; set; }
        public uint? ExecutedCount { get; set; }
        public uint? FailedCount { get; set; }
        public uint? AlarmCount { get; set; }
        public string TestName { get; set; }
        public string SequencerName { get; set; }
        public string TestLabel { get; set; }
        public float? TestTime { get; set; }
        public float? TestMin { get; set; }
        public float? TestMax { get; set; }
        public float? TestSum { get; set; }
        public float? TestSumOfSquares { get; set; }
    }
}
