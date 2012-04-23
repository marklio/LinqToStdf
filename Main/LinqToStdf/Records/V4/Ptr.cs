// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(uint), AssignTo = "TestNumber"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(byte), AssignTo = "SiteNumber"),
    StdfFieldLayout(FieldIndex = 3, FieldType = typeof(byte), AssignTo = "TestFlags"),
    StdfFieldLayout(FieldIndex = 4, FieldType = typeof(byte), AssignTo = "ParametricFlags"),
    StdfOptionalFieldLayout(FieldIndex = 5, FieldType = typeof(float), FlagIndex = 3, FlagMask = (byte)0x02, AssignTo = "Result"),
    StdfStringLayout(FieldIndex = 6, AssignTo = "TestText", MissingValue = ""),
    StdfStringLayout(FieldIndex = 7, AssignTo = "AlarmId", MissingValue = ""),
    StdfFieldLayout(FieldIndex = 8, FieldType = typeof(byte), AssignTo = "OptionalFlags"),
    StdfOptionalFieldLayout(FieldIndex = 9, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0x01, AssignTo = "ResultScalingExponent"),
    StdfOptionalFieldLayout(FieldIndex = 10, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0x50, AssignTo = "LowLimitScalingExponent"),
    StdfOptionalFieldLayout(FieldIndex = 11, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0xA0, AssignTo = "HighLimitScalingExponent"),
    StdfOptionalFieldLayout(FieldIndex = 12, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x50, AssignTo = "LowLimit"),
    StdfOptionalFieldLayout(FieldIndex = 13, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0xA0, AssignTo = "HighLimit"),
    StdfStringLayout(FieldIndex = 14, AssignTo = "Units", MissingValue = ""),
    StdfStringLayout(FieldIndex = 15, AssignTo = "ResultFormatString", MissingValue = ""),
    StdfStringLayout(FieldIndex = 16, AssignTo = "LowLimitFormatString", MissingValue = ""),
    StdfStringLayout(FieldIndex = 17, AssignTo = "HighLimitFormatString", MissingValue = ""),
    StdfOptionalFieldLayout(FieldIndex = 18, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x04, AssignTo = "LowSpecLimit"),
    StdfOptionalFieldLayout(FieldIndex = 19, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x08, AssignTo = "HighSpecLimit")]
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
