// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(ushort), RecordProperty = "Index"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(ushort), MissingValue = ushort.MinValue, RecordProperty = "ChannelType"),
    StringFieldLayout(FieldIndex = 2, RecordProperty = "ChannelName", MissingValue = ""),
    StringFieldLayout(FieldIndex = 3, RecordProperty = "PhysicalName", MissingValue = ""),
    StringFieldLayout(FieldIndex = 4, RecordProperty = "LogicalName", MissingValue = ""),
    FieldLayout(FieldIndex = 5, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber")]
    public class Pmr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 60); }
        }

        public ushort Index { get; set; }
        public ushort? ChannelType { get; set; }
        public string ChannelName { get; set; }
        public string PhysicalName { get; set; }
        public string LogicalName { get; set; }
        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
    }
}
