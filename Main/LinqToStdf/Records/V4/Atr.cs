using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;
	
	[StdfFieldLayout(FieldIndex = 0, FieldType  = typeof(DateTime), AssignTo = "ModifiedTime"),
	StdfStringLayout(FieldIndex = 1, AssignTo = "CommandLine")]
	public class Atr : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(0, 20); }
		}

        public DateTime? ModifiedTime { get; set; }
        public string CommandLine { get; set; }
	}
}
