// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = byte.MaxValue, RecordProperty = "SiteGroup"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(DateTime), RecordProperty = "FinishTime"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(uint), RecordProperty = "PartCount"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(uint), MissingValue = uint.MaxValue, RecordProperty = "RetestCount"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(uint), MissingValue = uint.MaxValue, RecordProperty = "AbortCount"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(uint), MissingValue = uint.MaxValue, RecordProperty = "GoodCount"),
    FieldLayout(FieldIndex = 7, FieldType = typeof(uint), MissingValue = uint.MaxValue, RecordProperty = "FunctionalCount"),
    StringFieldLayout(FieldIndex = 8, RecordProperty = "WaferId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 9, RecordProperty = "FabWaferId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 10, RecordProperty = "FrameId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 11, RecordProperty = "MaskId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 12, RecordProperty = "UserDescription", MissingValue = ""),
    StringFieldLayout(FieldIndex = 13, RecordProperty = "ExecDescription", MissingValue = "")]
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
