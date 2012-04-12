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
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(ushort)),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(ushort)),
    StdfNibbleArrayLayout(FieldIndex = 7, ArrayLengthFieldIndex = 5, AssignTo = "PinStates"),
    StdfArrayLayout(FieldIndex = 8, FieldType = typeof(float), ArrayLengthFieldIndex = 6, AssignTo = "Results"),
    StdfStringLayout(FieldIndex = 9, AssignTo = "TestText"),
    StdfStringLayout(FieldIndex = 10, AssignTo = "AlarmId"),
    StdfFieldLayout(FieldIndex = 11, FieldType = typeof(byte), AssignTo = "OptionalFlags"),
    StdfOptionalFieldLayout(FieldIndex = 12, FieldType = typeof(sbyte), FlagIndex = 11, FlagMask = (byte)0x01, AssignTo = "ResultScalingExponent"),
    StdfOptionalFieldLayout(FieldIndex = 13, FieldType = typeof(sbyte), FlagIndex = 11, FlagMask = (byte)0x50, AssignTo = "LowLimitScalingExponent"),
    StdfOptionalFieldLayout(FieldIndex = 14, FieldType = typeof(sbyte), FlagIndex = 11, FlagMask = (byte)0xA0, AssignTo = "HighLimitScalingExponent"),
    StdfOptionalFieldLayout(FieldIndex = 15, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x50, AssignTo = "LowLimit"),
    StdfOptionalFieldLayout(FieldIndex = 16, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0xA0, AssignTo = "HighLimit"),
    StdfOptionalFieldLayout(FieldIndex = 17, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x02, AssignTo = "StartingCondition"),
    StdfOptionalFieldLayout(FieldIndex = 18, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x02, AssignTo = "ConditionIncrement"),
    StdfArrayLayout(FieldIndex = 19, FieldType = typeof(ushort), ArrayLengthFieldIndex = 5, AssignTo = "PinIndexes"),
    StdfStringLayout(FieldIndex = 20, AssignTo = "Units"),
    StdfStringLayout(FieldIndex = 21, AssignTo = "IncrementUnits"),
    StdfStringLayout(FieldIndex = 22, AssignTo = "ResultFormatString"),
    StdfStringLayout(FieldIndex = 23, AssignTo = "LowLimitFormatString"),
    StdfStringLayout(FieldIndex = 24, AssignTo = "HighLimitFormatString"),
    StdfOptionalFieldLayout(FieldIndex = 25, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x04, AssignTo = "LowSpecLimit"),
    StdfOptionalFieldLayout(FieldIndex = 26, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x08, AssignTo = "HighSpecLimit")]
    public class Mpr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(15, 15); }
        }

        public uint TestNumber { get; set; }
        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
        public byte TestFlags { get; set; }
        public byte ParametricFlags { get; set; }
        public byte[] PinStates { get; set; }
        public float[] Results { get; set; }
        public string TestText { get; set; }
        public string AlarmId { get; set; }
        public byte? OptionalFlags { get; set; }
        public sbyte? ResultScalingExponent { get; set; }
        public sbyte? LowLimitScalingExponent { get; set; }
        public sbyte? HighLimitScalingExponent { get; set; }
        public float? LowLimit { get; set; }
        public float? HighLimit { get; set; }
        public float? StartingCondition { get; set; }
        public float? ConditionIncrement { get; set; }
        public ushort[] PinIndexes { get; set; }
        public string Units { get; set; }
        public string IncrementUnits { get; set; }
        public string ResultFormatString { get; set; }
        public string LowLimitFormatString { get; set; }
        public string HighLimitFormatString { get; set; }
        public float? LowSpecLimit { get; set; }
        public float? HighSpecLimit { get; set; }
    }
}
