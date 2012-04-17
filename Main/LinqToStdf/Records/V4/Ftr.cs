// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;
    using System.Collections;

	[StdfFieldLayout(FieldIndex = 0, FieldType = typeof(uint), AssignTo = "TestNumber"),
	StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "HeadNumber"),
	StdfFieldLayout(FieldIndex = 2, FieldType = typeof(byte), AssignTo = "SiteNumber"),
	StdfFieldLayout(FieldIndex = 3, FieldType = typeof(byte), AssignTo = "TestFlags"),
	StdfFieldLayout(FieldIndex = 4, FieldType = typeof(byte)),
	StdfOptionalFieldLayout(FieldIndex = 5, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x01, AssignTo = "CycleCount"),
	StdfOptionalFieldLayout(FieldIndex = 6, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x02, AssignTo = "RelativeVectorAddress"),
	StdfOptionalFieldLayout(FieldIndex = 7, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x04, AssignTo = "RepeatCount"),
	StdfOptionalFieldLayout(FieldIndex = 8, FieldType = typeof(uint), FlagIndex = 4, FlagMask = (byte)0x08, AssignTo = "FailingPinCount"),
	StdfOptionalFieldLayout(FieldIndex = 9, FieldType = typeof(int), FlagIndex = 4, FlagMask = (byte)0x10, AssignTo = "XFailureAddress"),
	StdfOptionalFieldLayout(FieldIndex = 10, FieldType = typeof(int), FlagIndex = 4, FlagMask = (byte)0x10, AssignTo = "YFailureAddress"),
	StdfOptionalFieldLayout(FieldIndex = 11, FieldType = typeof(short), FlagIndex = 4, FlagMask = (byte)0x20, AssignTo = "VectorOffset"),
	StdfFieldLayout(FieldIndex = 12, FieldType = typeof(ushort)),
	StdfFieldLayout(FieldIndex = 13, FieldType = typeof(ushort)),
	StdfArrayLayout(FieldIndex = 14, FieldType = typeof(ushort), ArrayLengthFieldIndex = 12, AssignTo = "ReturnIndexes"),
	StdfNibbleArrayLayout(FieldIndex = 15, ArrayLengthFieldIndex = 12, AssignTo = "ReturnStates"),
	StdfArrayLayout(FieldIndex = 16, FieldType = typeof(ushort), ArrayLengthFieldIndex = 13, AssignTo = "ProgrammedIndexes"),
	StdfNibbleArrayLayout(FieldIndex = 17, ArrayLengthFieldIndex = 13, AssignTo = "ProgrammedStates"),
	StdfFieldLayout(FieldIndex = 18, FieldType = typeof(BitArray), AssignTo = "FailingPinBitfield"),
    StdfStringLayout(FieldIndex = 19, AssignTo = "VectorName", MissingValue = ""),
    StdfStringLayout(FieldIndex = 20, AssignTo = "TimeSet", MissingValue = ""),
    StdfStringLayout(FieldIndex = 21, AssignTo = "OpCode", MissingValue = ""),
    StdfStringLayout(FieldIndex = 22, AssignTo = "TestText", MissingValue = ""),
    StdfStringLayout(FieldIndex = 23, AssignTo = "AlarmId", MissingValue = ""),
    StdfStringLayout(FieldIndex = 24, AssignTo = "ProgrammedText", MissingValue = ""),
    StdfStringLayout(FieldIndex = 25, AssignTo = "ResultText", MissingValue = ""),
    StdfFieldLayout(FieldIndex = 26, FieldType = typeof(byte), AssignTo = "PatternGeneratorNumber"),
    StdfFieldLayout(FieldIndex = 27, FieldType = typeof(BitArray), AssignTo = "SpinMap")]
	public class Ftr : StdfRecord, IHeadSiteIndexable  {

		public override RecordType RecordType {
			get { return new RecordType(15, 20); }
		}
        public uint TestNumber { get; set; }
        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
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
