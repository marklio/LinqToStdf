// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(byte), AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "SiteNumber"),
    StdfStringLayout(FieldIndex = 2, Length = 1, MissingValue = " ", AssignTo = "TestType"),
    StdfFieldLayout(FieldIndex = 3, FieldType = typeof(uint), AssignTo = "TestNumber"),
    StdfFieldLayout(FieldIndex = 4, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "ExecutedCount"),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "FailedCount"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "AlarmCount"),
    StdfStringLayout(FieldIndex = 7, AssignTo = "TestName", MissingValue = ""),
    StdfStringLayout(FieldIndex = 8, AssignTo = "SequencerName", MissingValue = ""),
    StdfStringLayout(FieldIndex = 9, AssignTo = "TestLabel", MissingValue = ""),
    StdfFieldLayout(FieldIndex = 10, FieldType = typeof(byte)),
    StdfOptionalFieldLayout(FieldIndex = 11, FieldType = typeof(float), FlagIndex = 10, FlagMask = (byte)0x04, AssignTo = "TestTime"),
    StdfOptionalFieldLayout(FieldIndex = 12, FieldType = typeof(float), FlagIndex = 10, FlagMask = (byte)0x01, AssignTo = "TestMin"),
    StdfOptionalFieldLayout(FieldIndex = 13, FieldType = typeof(float), FlagIndex = 10, FlagMask = (byte)0x02, AssignTo = "TestMax"),
    StdfOptionalFieldLayout(FieldIndex = 14, FieldType = typeof(float), FlagIndex = 10, FlagMask = (byte)0x10, AssignTo = "TestSum"),
    StdfOptionalFieldLayout(FieldIndex = 15, FieldType = typeof(float), FlagIndex = 10, FlagMask = (byte)0x20, AssignTo = "TestSumOfSquares")]
    public class Tsr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(10, 30); }
        }

        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
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
