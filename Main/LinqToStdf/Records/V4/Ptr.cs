// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(uint), RecordProperty = "TestNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), RecordProperty = "SiteNumber"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(byte), RecordProperty = "TestFlags"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(byte), RecordProperty = "ParametricFlags"),
    FlaggedFieldLayout(FieldIndex = 5, FieldType = typeof(float), FlagIndex = 3, FlagMask = (byte)0x02, RecordProperty = "Result"),
    StringFieldLayout(FieldIndex = 6, RecordProperty = "TestText", MissingValue = ""),
    StringFieldLayout(FieldIndex = 7, RecordProperty = "AlarmId", MissingValue = ""),
    FieldLayout(FieldIndex = 8, FieldType = typeof(byte), RecordProperty = "OptionalFlags"),
    FlaggedFieldLayout(FieldIndex = 9, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0x01, RecordProperty = "ResultScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 10, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0x50, RecordProperty = "LowLimitScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 11, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0xA0, RecordProperty = "HighLimitScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 12, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x50, RecordProperty = "LowLimit"),
    FlaggedFieldLayout(FieldIndex = 13, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0xA0, RecordProperty = "HighLimit"),
    StringFieldLayout(FieldIndex = 14, RecordProperty = "Units", MissingValue = ""),
    StringFieldLayout(FieldIndex = 15, RecordProperty = "ResultFormatString", MissingValue = ""),
    StringFieldLayout(FieldIndex = 16, RecordProperty = "LowLimitFormatString", MissingValue = ""),
    StringFieldLayout(FieldIndex = 17, RecordProperty = "HighLimitFormatString", MissingValue = ""),
    FlaggedFieldLayout(FieldIndex = 18, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x04, RecordProperty = "LowSpecLimit"),
    FlaggedFieldLayout(FieldIndex = 19, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x08, RecordProperty = "HighSpecLimit")]
    public class Ptr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(15, 10); }
        }

        public uint TestNumber { get; set; }
        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
        public byte TestFlags { get; set; }
        public byte ParametricFlags { get; set; }
        public float? Result { get; set; }
        public string TestText { get; set; }
        public string AlarmId { get; set; }
        public byte? OptionalFlags { get; set; }
        public sbyte? ResultScalingExponent { get; set; }
        public sbyte? LowLimitScalingExponent { get; set; }
        public sbyte? HighLimitScalingExponent { get; set; }
        public float? LowLimit { get; set; }
        public float? HighLimit { get; set; }
        public string Units { get; set; }
        public string ResultFormatString { get; set; }
        public string LowLimitFormatString { get; set; }
        public string HighLimitFormatString { get; set; }
        public float? LowSpecLimit { get; set; }
        public float? HighSpecLimit { get; set; }
    }
}
