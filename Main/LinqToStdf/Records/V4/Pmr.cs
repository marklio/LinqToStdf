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
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(ushort), MissingValue = ushort.MinValue, AssignTo = "ChannelType"),
    StdfStringLayout(FieldIndex = 2, AssignTo = "ChannelName", MissingValue = ""),
    StdfStringLayout(FieldIndex = 3, AssignTo = "PhysicalName", MissingValue = ""),
    StdfStringLayout(FieldIndex = 4, AssignTo = "LogicalName", MissingValue = ""),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(byte), MissingValue = (byte)1, AllowMissingValue = true, AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(byte), MissingValue = (byte)1, AllowMissingValue = true, AssignTo = "SiteNumber")]
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
