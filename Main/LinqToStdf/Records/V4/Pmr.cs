// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(ushort), RecordProperty = "PinIndex"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(ushort), MissingValue = ushort.MinValue, RecordProperty = "ChannelType"),
    StringFieldLayout(FieldIndex = 2, IsOptional = true, RecordProperty = "ChannelName"),
    StringFieldLayout(FieldIndex = 3, IsOptional = true, RecordProperty = "PhysicalName"),
    StringFieldLayout(FieldIndex = 4, IsOptional = true, RecordProperty = "LogicalName"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(byte), IsOptional = true, MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(byte), IsOptional = true, MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber")]
    public class Pmr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 60); }
        }

        /// <summary>
        /// While ushort, valid PMR PinIndexes must be 1 - 32,767
        /// </summary>
        public ushort PinIndex { get; set; }
        public ushort? ChannelType { get; set; }
        public string? ChannelName { get; set; }
        public string? PhysicalName { get; set; }
        public string? LogicalName { get; set; }
        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }

        [Obsolete("Pmr.Index has been renamed Pmr.PinIndex to be consistent with Pgr.PinIndexes")]
        public ushort Index {
            get { return PinIndex; }
            set { PinIndex = value; }
        }
    }
}
