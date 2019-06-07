// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(float), IsOptional = true, MissingValue = (float)0, RecordProperty = "WaferSize"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(float), IsOptional = true, MissingValue = (float)0, RecordProperty = "DieHeight"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(float), IsOptional = true, MissingValue = (float)0, RecordProperty = "DieWidth"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(byte), IsOptional = true, MissingValue = byte.MinValue, RecordProperty = "Units"),
    StringFieldLayout(FieldIndex = 4, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "Flat"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(short), IsOptional = true, MissingValue = short.MinValue, RecordProperty = "CenterX"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(short), IsOptional = true, MissingValue = short.MinValue, RecordProperty = "CenterY"),
    StringFieldLayout(FieldIndex = 7, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "PositiveX"),
    StringFieldLayout(FieldIndex = 8, IsOptional = true, Length = 1, MissingValue= " ", RecordProperty = "PositiveY")]
    public class Wcr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(2, 30); }
        }

        public float? WaferSize { get; set; }
        public float? DieHeight { get; set; }
        public float? DieWidth { get; set; }
        /// <summary>
        /// Known values are: 0 (unknown), 1 (in), 2 (cm), 3 (mm), 4 (mils)
        /// </summary>
        public byte? Units { get; set; }
        /// <summary>
        /// Known values are: U, D, L, R
        /// </summary>
        public string Flat { get; set; }
        public short? CenterX { get; set; }
        public short? CenterY { get; set; }
        /// <summary>
        /// Known values are: L, R
        /// </summary>
        public string PositiveX { get; set; }
        /// <summary>
        /// Known values are: U, D
        /// </summary>
        public string PositiveY { get; set; }
    }
}
