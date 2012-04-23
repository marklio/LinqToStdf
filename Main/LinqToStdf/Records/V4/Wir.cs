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
    FieldLayout(FieldIndex = 2, FieldType = typeof(DateTime), RecordProperty = "StartTime"),
    StringFieldLayout(FieldIndex = 3, RecordProperty = "WaferId", MissingValue = "")]
    public class Wir : StdfRecord, IHeadIndexable {

        public override RecordType RecordType {
            get { return new RecordType(2, 10); }
        }

        public byte HeadNumber { get; set; }
        public byte? SiteGroup { get; set; }
        public DateTime? StartTime { get; set; }
        public string WaferId { get; set; }
    }
}
