// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(ushort), AssignTo = "Index"),
    StdfStringLayout(FieldIndex = 1, AssignTo = "GroupName"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(ushort)),
    StdfArrayLayout(FieldIndex = 3, FieldType = typeof(ushort), ArrayLengthFieldIndex = 2, AssignTo = "PinIndexes")]
    public class Pgr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 62); }
        }

        public ushort Index { get; set; }
        public string GroupName { get; set; }
        public ushort[] PinIndexes { get; set; }
    }
}
