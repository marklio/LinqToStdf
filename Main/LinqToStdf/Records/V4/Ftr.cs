// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4
{
    using Attributes;
    using System.Collections;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(uint), RecordProperty = "TestNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(byte), RecordProperty = "TestFlags"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(byte), IsOptional = true, MissingValue = (byte)0),
    FlaggedFieldLayout(FieldIndex = 5, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x01, RecordProperty = "CycleCount"),
    FlaggedFieldLayout(FieldIndex = 6, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x02, RecordProperty = "RelativeVectorAddress"),
    FlaggedFieldLayout(FieldIndex = 7, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x04, RecordProperty = "RepeatCount"),
    FlaggedFieldLayout(FieldIndex = 8, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x08, RecordProperty = "FailingPinCount"),
    FlaggedFieldLayout(FieldIndex = 9, FieldType = typeof(int), FlagIndex = 4, FlagMask = (byte)0x10, RecordProperty = "XFailureAddress"),
    FlaggedFieldLayout(FieldIndex = 10, FieldType = typeof(int), FlagIndex = 4, FlagMask = (byte)0x10, RecordProperty = "YFailureAddress"),
    FlaggedFieldLayout(FieldIndex = 11, FieldType = typeof(short), FlagIndex = 4, FlagMask = (byte)0x20, RecordProperty = "VectorOffset"),
    FieldLayout(FieldIndex = 12, FieldType = typeof(ushort), IsOptional = true, MissingValue = (ushort)0),
    FieldLayout(FieldIndex = 13, FieldType = typeof(ushort), IsOptional = true, MissingValue = (ushort)0),
    ArrayFieldLayout(FieldIndex = 14, FieldType = typeof(ushort), ArrayLengthFieldIndex = 12, RecordProperty = "ReturnIndexes"),
    NibbleArrayFieldLayout(FieldIndex = 15, ArrayLengthFieldIndex = 12, RecordProperty = "ReturnStates"),
    ArrayFieldLayout(FieldIndex = 16, FieldType = typeof(ushort), ArrayLengthFieldIndex = 13, RecordProperty = "ProgrammedIndexes"),
    NibbleArrayFieldLayout(FieldIndex = 17, ArrayLengthFieldIndex = 13, RecordProperty = "ProgrammedStates"),
    FieldLayout(FieldIndex = 18, FieldType = typeof(BitArray), IsOptional = true, RecordProperty = "FailingPinBitfield"),
    StringFieldLayout(FieldIndex = 19, IsOptional = true, RecordProperty = "VectorName"),
    StringFieldLayout(FieldIndex = 20, IsOptional = true, RecordProperty = "TimeSet"),
    StringFieldLayout(FieldIndex = 21, IsOptional = true, RecordProperty = "OpCode"),
    StringFieldLayout(FieldIndex = 22, IsOptional = true, RecordProperty = "TestText"),
    StringFieldLayout(FieldIndex = 23, IsOptional = true, RecordProperty = "AlarmId"),
    StringFieldLayout(FieldIndex = 24, IsOptional = true, RecordProperty = "ProgrammedText"),
    StringFieldLayout(FieldIndex = 25, IsOptional = true, RecordProperty = "ResultText"),
    FieldLayout(FieldIndex = 26, FieldType = typeof(byte), IsOptional = true, MissingValue = Byte.MaxValue, RecordProperty = "PatternGeneratorNumber"),
    FieldLayout(FieldIndex = 27, FieldType = typeof(BitArray), IsOptional = true, RecordProperty = "SpinMap")]
    public class Ftr : StdfRecord, IHeadSiteIndexable
    {

        public override RecordType RecordType
        {
            get { return new RecordType(15, 20); }
        }
        public uint TestNumber { get; set; }
        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        public byte TestFlags { get; set; }
        public uint? CycleCount { get; set; }
        public uint? RelativeVectorAddress { get; set; }
        public uint? RepeatCount { get; set; }
        public uint? FailingPinCount { get; set; }
        public int? XFailureAddress { get; set; }
        public int? YFailureAddress { get; set; }
        public short? VectorOffset { get; set; }
        public ushort[] ReturnIndexes { get; set; }
        public byte[] ReturnStates { get; set; }
        public ushort[] ProgrammedIndexes { get; set; }
        public byte[] ProgrammedStates { get; set; }
        public BitArray FailingPinBitfield { get; set; }
        public string VectorName { get; set; }
        public string OpCode { get; set; }
        public string TestText { get; set; }
        public string AlarmId { get; set; }
        public string ProgrammedText { get; set; }
        public string ResultText { get; set; }
        public byte? PatternGeneratorNumber { get; set; }
        public BitArray SpinMap { get; set; }
    }
}
