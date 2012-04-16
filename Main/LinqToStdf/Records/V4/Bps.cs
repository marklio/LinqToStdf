// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;
	
	[StdfStringLayout(FieldIndex = 0, AssignTo = "Name", MissingValue="")]
	public class Bps : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(20, 10); }
		}
        public string Name { get; set; }
	}
}
