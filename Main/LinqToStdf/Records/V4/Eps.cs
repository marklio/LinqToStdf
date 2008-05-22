using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;
	
	public class Eps : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(20, 20); }
		}
	}
}
