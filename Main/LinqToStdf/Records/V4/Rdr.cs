using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(ushort)),
    StdfArrayLayout(FieldIndex = 1, FieldType = typeof(ushort), ArrayLengthFieldIndex = 0, AssignTo = "RetestBins")]
    public class Rdr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 70); }
        }

        public ushort[] RetestBins { get; set; }
    }
}
