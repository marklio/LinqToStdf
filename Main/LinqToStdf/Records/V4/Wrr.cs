// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(byte), AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = byte.MaxValue, AssignTo = "SiteGroup"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(DateTime), AssignTo = "FinishTime"),
    StdfFieldLayout(FieldIndex = 3, FieldType = typeof(uint), AssignTo = "PartCount"),
    StdfFieldLayout(FieldIndex = 4, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "RetestCount"),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "AbortCount"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "GoodCount"),
    StdfFieldLayout(FieldIndex = 7, FieldType = typeof(uint), MissingValue = uint.MaxValue, AssignTo = "FunctionalCount"),
    StdfStringLayout(FieldIndex = 8, AssignTo = "WaferId"),
    StdfStringLayout(FieldIndex = 9, AssignTo = "FabWaferId"),
    StdfStringLayout(FieldIndex = 10, AssignTo = "FrameId"),
    StdfStringLayout(FieldIndex = 11, AssignTo = "MaskId"),
    StdfStringLayout(FieldIndex = 12, AssignTo = "UserDescription"),
    StdfStringLayout(FieldIndex = 13, AssignTo = "ExecDescription")]
    public class Wrr : StdfRecord, IHeadIndexable {

        public override RecordType RecordType {
            get { return new RecordType(2, 20); }
        }

        public byte HeadNumber { get; set; }
        public byte? SiteGroup { get; set; }
        public DateTime? FinishTime { get; set; }
        public uint PartCount { get; set; }
        public uint? RetestCount { get; set; }
        public uint? AbortCount { get; set; }
        public uint? GoodCount { get; set; }
        public uint? FunctionalCount { get; set; }
        public string WaferId { get; set; }
        public string FabWaferId { get; set; }
        public string FrameId { get; set; }
        public string MaskId { get; set; }
        public string UserDescription { get; set; }
        public string ExecDescription { get; set; }
    }
}
