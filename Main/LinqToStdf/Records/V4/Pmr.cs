using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(ushort), AssignTo = "Index"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(ushort), MissingValue = ushort.MinValue, AssignTo = "ChannelType"),
    StdfStringLayout(FieldIndex = 2, AssignTo = "ChannelName"),
    StdfStringLayout(FieldIndex = 3, AssignTo = "PhysicalName"),
    StdfStringLayout(FieldIndex = 4, AssignTo = "LogicalName"),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(byte), MissingValue = (byte)1, AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(byte), MissingValue = (byte)1, AssignTo = "SiteNumber")]
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
