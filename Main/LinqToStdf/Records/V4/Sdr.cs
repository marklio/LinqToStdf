// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(byte), AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "SiteGroup"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(byte)),
    StdfArrayLayout(FieldIndex = 3, FieldType = typeof(byte), ArrayLengthFieldIndex = 2, AssignTo = "Sites"),
    StdfStringLayout(FieldIndex = 4, AssignTo = "HandlerType"),
    StdfStringLayout(FieldIndex = 5, AssignTo = "HandlerId"),
    StdfStringLayout(FieldIndex = 6, AssignTo = "CardType"),
    StdfStringLayout(FieldIndex = 7, AssignTo = "CardId"),
    StdfStringLayout(FieldIndex = 8, AssignTo = "LoadboardType"),
    StdfStringLayout(FieldIndex = 9, AssignTo = "LoadboardId"),
    StdfStringLayout(FieldIndex = 10, AssignTo = "DibType"),
    StdfStringLayout(FieldIndex = 11, AssignTo = "DibId"),
    StdfStringLayout(FieldIndex = 12, AssignTo = "CableType"),
    StdfStringLayout(FieldIndex = 13, AssignTo = "CableId"),
    StdfStringLayout(FieldIndex = 14, AssignTo = "ContactorType"),
    StdfStringLayout(FieldIndex = 15, AssignTo = "ContactorId"),
    StdfStringLayout(FieldIndex = 16, AssignTo = "LaserType"),
    StdfStringLayout(FieldIndex = 17, AssignTo = "LaserId"),
    StdfStringLayout(FieldIndex = 18, AssignTo = "ExtraType"),
    StdfStringLayout(FieldIndex = 19, AssignTo = "ExtraId")]
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
