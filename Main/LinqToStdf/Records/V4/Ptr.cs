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
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(byte), RecordProperty = "TestFlags"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(byte), RecordProperty = "ParametricFlags"),
    FlaggedFieldLayout(FieldIndex = 5, FieldType = typeof(float), FlagIndex = 3, FlagMask = (byte)0x02, MissingValue = (float)0, RecordProperty = "Result"),
    StringFieldLayout(FieldIndex = 6, IsOptional = true, RecordProperty = "TestText"),
    StringFieldLayout(FieldIndex = 7, IsOptional = true, RecordProperty = "AlarmId"),
    FieldLayout(FieldIndex = 8, FieldType = typeof(byte), IsOptional = true, MissingValue = (byte)0, RecordProperty = "OptionalFlags"),
    FlaggedFieldLayout(FieldIndex = 9, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0x01, MissingValue = (sbyte)0, RecordProperty = "ResultScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 10, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0x50, MissingValue = (sbyte)0, RecordProperty = "LowLimitScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 11, FieldType = typeof(sbyte), FlagIndex = 8, FlagMask = (byte)0xA0, MissingValue = (sbyte)0, RecordProperty = "HighLimitScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 12, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x50, MissingValue = (float)0, RecordProperty = "LowLimit"),
    FlaggedFieldLayout(FieldIndex = 13, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0xA0, MissingValue = (float)0, RecordProperty = "HighLimit"),
    StringFieldLayout(FieldIndex = 14, IsOptional = true, RecordProperty = "Units"),
    StringFieldLayout(FieldIndex = 15, IsOptional = true, RecordProperty = "ResultFormatString"),
    StringFieldLayout(FieldIndex = 16, IsOptional = true, RecordProperty = "LowLimitFormatString"),
    StringFieldLayout(FieldIndex = 17, IsOptional = true, RecordProperty = "HighLimitFormatString"),
    FlaggedFieldLayout(FieldIndex = 18, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x04, MissingValue = (float)0, RecordProperty = "LowSpecLimit"),
    FlaggedFieldLayout(FieldIndex = 19, FieldType = typeof(float), FlagIndex = 8, FlagMask = (byte)0x08, MissingValue = (float)0, RecordProperty = "HighSpecLimit")]
    public class Ptr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(15, 10); }
        }

        public uint TestNumber { get; set; }
        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        public byte TestFlags { get; set; }
        public byte ParametricFlags { get; set; }
        public float? Result { get; set; }
        public string? TestText { get; set; }
        public string? AlarmId { get; set; }
        public byte? OptionalFlags { get; set; }
        /// <summary>
        /// Known values are: 15, 12, 9, 6, 3, 2, 0, -3, -6, -9, -12
        /// </summary>
        public sbyte? ResultScalingExponent { get; set; }
        /// <summary>
        /// Known values are: 15, 12, 9, 6, 3, 2, 0, -3, -6, -9, -12
        /// </summary>
        public sbyte? LowLimitScalingExponent { get; set; }
        /// <summary>
        /// Known values are: 15, 12, 9, 6, 3, 2, 0, -3, -6, -9, -12
        /// </summary>
        public sbyte? HighLimitScalingExponent { get; set; }
        public float? LowLimit { get; set; }
        public float? HighLimit { get; set; }
        public string? Units { get; set; }
        public string? ResultFormatString { get; set; }
        public string? LowLimitFormatString { get; set; }
        public string? HighLimitFormatString { get; set; }
        public float? LowSpecLimit { get; set; }
        public float? HighSpecLimit { get; set; }
    }
}
