using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    public class Sbr : BinSummaryRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 50); }
        }

        public override BinType BinType {
            get { return BinType.Soft; }
        }
    }
}
