// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(DateTime), RecordProperty = "FinishTime"),
    StringFieldLayout(FieldIndex = 1, Length = 1, MissingValue = " ", RecordProperty = "DispositionCode"),
    StringFieldLayout(FieldIndex = 2, RecordProperty = "UserDescription", MissingValue = ""),
    StringFieldLayout(FieldIndex = 3, RecordProperty = "ExecDescription", MissingValue = "")]
    public class Mrr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 20); }
        }

        public DateTime? FinishTime { get; set; }
        public string DispositionCode { get; set; }
        public string UserDescription { get; set; }
        public string ExecDescription { get; set; }
    }
}
