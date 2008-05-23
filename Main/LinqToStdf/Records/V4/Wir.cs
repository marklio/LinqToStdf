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
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(DateTime), AssignTo = "StartTime"),
    StdfStringLayout(FieldIndex = 3, AssignTo = "WaferId")]
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
