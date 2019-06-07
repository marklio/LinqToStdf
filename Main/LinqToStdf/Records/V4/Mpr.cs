// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf.Records.V4
{
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(uint), RecordProperty = "TestNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(byte), RecordProperty = "TestFlags"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(byte), RecordProperty = "ParametricFlags"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(ushort)),
    FieldLayout(FieldIndex = 6, FieldType = typeof(ushort)),
    NibbleArrayFieldLayout(FieldIndex = 7, ArrayLengthFieldIndex = 5, RecordProperty = "PinStates"),
    ArrayFieldLayout(FieldIndex = 8, FieldType = typeof(float), ArrayLengthFieldIndex = 6, RecordProperty = "Results"),
    StringFieldLayout(FieldIndex = 9, IsOptional = true, RecordProperty = "TestText"),
    StringFieldLayout(FieldIndex = 10, IsOptional = true, RecordProperty = "AlarmId"),
    FieldLayout(FieldIndex = 11, FieldType = typeof(byte), IsOptional = true, MissingValue = (byte)0xFF, RecordProperty = "OptionalFlags"),
    FlaggedFieldLayout(FieldIndex = 12, FieldType = typeof(sbyte), FlagIndex = 11, FlagMask = (byte)0x01, MissingValue = (sbyte)0, RecordProperty = "ResultScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 13, FieldType = typeof(sbyte), FlagIndex = 11, FlagMask = (byte)0x50, MissingValue = (sbyte)0, RecordProperty = "LowLimitScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 14, FieldType = typeof(sbyte), FlagIndex = 11, FlagMask = (byte)0xA0, MissingValue = (sbyte)0, RecordProperty = "HighLimitScalingExponent"),
    FlaggedFieldLayout(FieldIndex = 15, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x50, MissingValue = Single.NegativeInfinity, RecordProperty = "LowLimit"),
    FlaggedFieldLayout(FieldIndex = 16, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0xA0, MissingValue = Single.PositiveInfinity, RecordProperty = "HighLimit"),
    FlaggedFieldLayout(FieldIndex = 17, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x02, MissingValue = (float)0, RecordProperty = "StartingCondition"),
    FlaggedFieldLayout(FieldIndex = 18, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x02, MissingValue = (float)0, RecordProperty = "ConditionIncrement"),
    ArrayFieldLayout(FieldIndex = 19, FieldType = typeof(ushort), IsOptional = true, MissingValue = (ushort)0, ArrayLengthFieldIndex = 5, RecordProperty = "PinIndexes"),
    StringFieldLayout(FieldIndex = 20, IsOptional = true, RecordProperty = "Units"),
    StringFieldLayout(FieldIndex = 21, IsOptional = true, RecordProperty = "IncrementUnits"),
    StringFieldLayout(FieldIndex = 22, IsOptional = true, RecordProperty = "ResultFormatString"),
    StringFieldLayout(FieldIndex = 23, IsOptional = true, RecordProperty = "LowLimitFormatString"),
    StringFieldLayout(FieldIndex = 24, IsOptional = true, RecordProperty = "HighLimitFormatString"),
    FlaggedFieldLayout(FieldIndex = 25, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x04, MissingValue = Single.NegativeInfinity, RecordProperty = "LowSpecLimit"),
    FlaggedFieldLayout(FieldIndex = 26, FieldType = typeof(float), FlagIndex = 11, FlagMask = (byte)0x08, MissingValue = Single.PositiveInfinity, RecordProperty = "HighSpecLimit")]
    public class Mpr : StdfRecord, IHeadSiteIndexable
    {
        public Mpr(StdfFile stdfFile) : base(stdfFile)
        {

        }
        public override RecordType RecordType
        {
            get { return new RecordType(15, 15); }
        }

        public uint TestNumber { get; set; }
        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        public byte TestFlags { get; set; }
        public byte ParametricFlags { get; set; }
        public byte[] PinStates { get; set; }
        public float[] Results { get; set; }
        public string TestText { get; set; }
        public string AlarmId { get; set; }
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
