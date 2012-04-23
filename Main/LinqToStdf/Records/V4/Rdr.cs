// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(ushort)),
    ArrayFieldLayout(FieldIndex = 1, FieldType = typeof(ushort), ArrayLengthFieldIndex = 0, RecordProperty = "RetestBins")]
    public class Rdr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 70); }
        }

        public ushort[] RetestBins { get; set; }
    }
}
