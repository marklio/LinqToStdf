// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

	[FieldLayout(FieldIndex = 0, FieldType  = typeof(byte), RecordProperty = "HeadNumber"),
	FieldLayout(FieldIndex = 1, FieldType  = typeof(byte), RecordProperty = "SiteNumber"),
	FieldLayout(FieldIndex = 2, FieldType  = typeof(ushort), RecordProperty = "BinNumber"),
	FieldLayout(FieldIndex = 3, FieldType  = typeof(uint), RecordProperty = "BinCount"),
	StringFieldLayout(FieldIndex = 4, Length = 1, MissingValue=" ", RecordProperty = "BinPassFail"),
	StringFieldLayout(FieldIndex = 5, RecordProperty = "BinName", MissingValue="")]
	public abstract class BinSummaryRecord : StdfRecord, IHeadSiteIndexable {
		public abstract BinType BinType { get;}
        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
        public ushort BinNumber { get; set; }
        public uint BinCount { get; set; }
        public string BinPassFail { get; set; }
        public string BinName { get; set; }
	}
}
