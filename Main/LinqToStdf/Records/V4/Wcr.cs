using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(float), MissingValue = (float)0, AssignTo = "WaferSize"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(float), MissingValue = (float)0, AssignTo = "DieHeight"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(float), MissingValue = (float)0, AssignTo = "DieWidth"),
    StdfFieldLayout(FieldIndex = 3, FieldType = typeof(byte), MissingValue = byte.MinValue, AssignTo = "Units"),
    StdfStringLayout(FieldIndex = 4, Length = 1, MissingValue = " ", AssignTo = "Flat"),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(short), MissingValue = short.MinValue, AssignTo = "CenterX"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(short), MissingValue = short.MinValue, AssignTo = "CenterY"),
    StdfStringLayout(FieldIndex = 7, Length = 1, MissingValue = " ", AssignTo = "PositiveX"),
    StdfStringLayout(FieldIndex = 8, Length = 1, MissingValue = " ", AssignTo = "PositiveY")]
    public class Wcr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(2, 30); }
        }

        public float? WaferSize { get; set; }
        public float? DieHeight { get; set; }
        public float? DieWidth { get; set; }
        public byte? Units { get; set; }
        public string Flat { get; set; }
        public short? CenterX { get; set; }
        public short? CenterY { get; set; }
        public string PositiveX { get; set; }
        public string PositiveY { get; set; }
    }
}
