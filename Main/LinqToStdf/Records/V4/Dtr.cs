using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

	[StdfStringLayout(FieldIndex = 0, AssignTo = "Text")]
	public class Dtr : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(50, 30); }
		}

        public string Text { get; set; }
	}
}
