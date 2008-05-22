using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(byte), AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "SiteNumber"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(uint), AssignTo = "PartCount"),
    StdfFieldLayout(FieldIndex = 3, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "RetestCount"),
    StdfFieldLayout(FieldIndex = 4, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "AbortCount"),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "GoodCount"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "FunctionalCount")]
    public class Pcr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(1, 30); }
        }

        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
        public uint PartCount { get; set; }
        public uint? RetestCount { get; set; }
        public uint? AbortCount { get; set; }
        public uint? GoodCount { get; set; }
        public uint? FunctionalCount { get; set; }
    }
}
