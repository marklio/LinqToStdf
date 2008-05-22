using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(DateTime), AssignTo = "FinishTime"),
    StdfStringLayout(FieldIndex = 1, Length = 1, MissingValue = " ", AssignTo = "DispositionCode"),
    StdfStringLayout(FieldIndex = 2, AssignTo = "UserDescription"),
    StdfStringLayout(FieldIndex = 3, AssignTo = "ExecDescription")]
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
