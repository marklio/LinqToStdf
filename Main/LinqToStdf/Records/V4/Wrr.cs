// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = byte.MaxValue, RecordProperty = "SiteGroup"),
    TimeFieldLayout(FieldIndex = 2, RecordProperty = "FinishTime"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(uint), RecordProperty = "PartCount"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "RetestCount"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "AbortCount"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "GoodCount"),
    FieldLayout(FieldIndex = 7, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "FunctionalCount"),
    StringFieldLayout(FieldIndex = 8, IsOptional = true, RecordProperty = "WaferId"),
    StringFieldLayout(FieldIndex = 9, IsOptional = true, RecordProperty = "FabWaferId"),
    StringFieldLayout(FieldIndex = 10, IsOptional = true, RecordProperty = "FrameId"),
    StringFieldLayout(FieldIndex = 11, IsOptional = true, RecordProperty = "MaskId"),
    StringFieldLayout(FieldIndex = 12, IsOptional = true, RecordProperty = "UserDescription"),
    StringFieldLayout(FieldIndex = 13, IsOptional = true, RecordProperty = "ExecDescription")]
    public class Wrr : StdfRecord, IHeadIndexable {

        public override RecordType RecordType {
            get { return new RecordType(2, 20); }
        }

        public byte? HeadNumber { get; set; }
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
