// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), RecordProperty = "SiteGroup"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte)),
    ArrayFieldLayout(FieldIndex = 3, FieldType = typeof(byte), ArrayLengthFieldIndex = 2, RecordProperty = "Sites"),
    StringFieldLayout(FieldIndex = 4, RecordProperty = "HandlerType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 5, RecordProperty = "HandlerId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 6, RecordProperty = "CardType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 7, RecordProperty = "CardId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 8, RecordProperty = "LoadboardType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 9, RecordProperty = "LoadboardId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 10, RecordProperty = "DibType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 11, RecordProperty = "DibId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 12, RecordProperty = "CableType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 13, RecordProperty = "CableId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 14, RecordProperty = "ContactorType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 15, RecordProperty = "ContactorId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 16, RecordProperty = "LaserType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 17, RecordProperty = "LaserId", MissingValue = ""),
    StringFieldLayout(FieldIndex = 18, RecordProperty = "ExtraType", MissingValue = ""),
    StringFieldLayout(FieldIndex = 19, RecordProperty = "ExtraId", MissingValue = "")]
    public class Sdr : StdfRecord, IHeadIndexable {

        public override RecordType RecordType {
            get { return new RecordType(1, 80); }
        }

        public byte HeadNumber { get; set; }
        public byte SiteGroup { get; set; }
        public byte[] Sites { get; set; }
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
    }
}
