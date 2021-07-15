// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

	[StringFieldLayout(FieldIndex = 0, RecordProperty = "Text")]
	public class Dtr : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(50, 30); }
		}

        public string? Text { get; set; }
	}
}
