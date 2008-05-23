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
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "SiteNumber")]
    public class Pir : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(5, 10); }
        }

        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
    }
}
