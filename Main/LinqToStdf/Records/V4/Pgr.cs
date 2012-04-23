// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(ushort), RecordProperty = "GroupIndex"),
    StringFieldLayout(FieldIndex = 1, RecordProperty = "GroupName", MissingValue = ""),
    FieldLayout(FieldIndex = 2, FieldType = typeof(ushort)),
    ArrayFieldLayout(FieldIndex = 3, FieldType = typeof(ushort), ArrayLengthFieldIndex = 2, RecordProperty = "PinIndexes")]
    public class Pgr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 62); }
        }

        public ushort GroupIndex { get; set; }
        public string GroupName { get; set; }
        public ushort[] PinIndexes { get; set; }

        [Obsolete("Pgr.Index has been renamed Pgr.GroupIndex to be consistent with Plr's GroupIndexes")]
        public ushort Index {
            get { return GroupIndex; }
            set { GroupIndex = value; }
        }
    }
}
