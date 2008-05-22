using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

	[StdfFieldLayout(FieldIndex = 0, FieldType  = typeof(byte), AssignTo = "HeadNumber"),
	StdfFieldLayout(FieldIndex = 1, FieldType  = typeof(byte), AssignTo = "SiteNumber"),
	StdfFieldLayout(FieldIndex = 2, FieldType  = typeof(ushort), AssignTo = "BinNumber"),
	StdfFieldLayout(FieldIndex = 3, FieldType  = typeof(uint), AssignTo = "BinCount"),
	StdfStringLayout(FieldIndex = 4, Length = 1, MissingValue=" ", AssignTo = "BinPassFail"),
	StdfStringLayout(FieldIndex = 5, AssignTo = "BinName")]
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
