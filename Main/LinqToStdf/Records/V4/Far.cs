using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

	[StdfFieldLayout(FieldIndex = 0, FieldType = typeof(byte), AssignTo = "CpuType"),
	StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo ="StdfVersion")]
	public class Far : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(0, 10); }
		}

        public byte CpuType { get; set; }
        public byte StdfVersion { get; set; }
	}
}
