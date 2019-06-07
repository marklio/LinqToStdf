// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4
{
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(uint), RecordProperty = "PartCount"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "RetestCount"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "AbortCount"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "GoodCount"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MaxValue, RecordProperty = "FunctionalCount")]
    public class Pcr : StdfRecord, IHeadSiteIndexable
    {

        public override RecordType RecordType
        {
            get { return new RecordType(1, 30); }
        }

        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        public uint PartCount { get; set; }
        public uint? RetestCount { get; set; }
        public uint? AbortCount { get; set; }
        public uint? GoodCount { get; set; }
        public uint? FunctionalCount { get; set; }
    }
}
