// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = byte.MaxValue, PersistMissingValue = true, RecordProperty = "SiteGroup"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte)),
    ArrayFieldLayout(FieldIndex = 3, FieldType = typeof(byte), ArrayLengthFieldIndex = 2, RecordProperty = "SiteNumbers"),
    StringFieldLayout(FieldIndex = 4, IsOptional = true, RecordProperty = "HandlerType"),
    StringFieldLayout(FieldIndex = 5, IsOptional = true, RecordProperty = "HandlerId"),
    StringFieldLayout(FieldIndex = 6, IsOptional = true, RecordProperty = "CardType"),
    StringFieldLayout(FieldIndex = 7, IsOptional = true, RecordProperty = "CardId"),
    StringFieldLayout(FieldIndex = 8, IsOptional = true, RecordProperty = "LoadboardType"),
    StringFieldLayout(FieldIndex = 9, IsOptional = true, RecordProperty = "LoadboardId"),
    StringFieldLayout(FieldIndex = 10, IsOptional = true, RecordProperty = "DibType"),
    StringFieldLayout(FieldIndex = 11, IsOptional = true, RecordProperty = "DibId"),
    StringFieldLayout(FieldIndex = 12, IsOptional = true, RecordProperty = "CableType"),
    StringFieldLayout(FieldIndex = 13, IsOptional = true, RecordProperty = "CableId"),
    StringFieldLayout(FieldIndex = 14, IsOptional = true, RecordProperty = "ContactorType"),
    StringFieldLayout(FieldIndex = 15, IsOptional = true, RecordProperty = "ContactorId"),
    StringFieldLayout(FieldIndex = 16, IsOptional = true, RecordProperty = "LaserType"),
    StringFieldLayout(FieldIndex = 17, IsOptional = true, RecordProperty = "LaserId"),
    StringFieldLayout(FieldIndex = 18, IsOptional = true, RecordProperty = "ExtraType"),
    StringFieldLayout(FieldIndex = 19, IsOptional = true, RecordProperty = "ExtraId")]
    public class Sdr : StdfRecord, IHeadIndexable {

        public override RecordType RecordType {
            get { return new RecordType(1, 80); }
        }

        public byte? HeadNumber { get; set; }
        public byte? SiteGroup { get; set; }
        public byte[] SiteNumbers { get; set; }
        public string HandlerType { get; set; }
        public string HandlerId { get; set; }
        public string CardType { get; set; }
        public string CardId { get; set; }
        public string LoadboardType { get; set; }
        public string LoadboardId { get; set; }
        public string DibType { get; set; }
        public string DibId { get; set; }
        public string CableType { get; set; }
        public string CableId { get; set; }
        public string ContactorType { get; set; }
        public string ContactorId { get; set; }
        public string LaserType { get; set; }
        public string LaserId { get; set; }
        public string ExtraType { get; set; }
        public string ExtraId { get; set; }

        [Obsolete("Sdr.Sites has been renamed Sdr.SiteNumbers to be consistent other records")]
        public byte[] Sites {
            get { return SiteNumbers; }
            set { SiteNumbers = value; }
        }
    }
}
