// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [TimeFieldLayout(FieldIndex = 0, RecordProperty = "FinishTime"),
    StringFieldLayout(FieldIndex = 1, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "DispositionCode"),
    StringFieldLayout(FieldIndex = 2, IsOptional = true, RecordProperty = "UserDescription"),
    StringFieldLayout(FieldIndex = 3, IsOptional = true, RecordProperty = "ExecDescription")]
    public class Mrr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 20); }
        }

        public DateTime? FinishTime { get; set; }
        public string? DispositionCode { get; set; }
        public string? UserDescription { get; set; }
        public string? ExecDescription { get; set; }
    }
}
