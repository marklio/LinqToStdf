// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
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
