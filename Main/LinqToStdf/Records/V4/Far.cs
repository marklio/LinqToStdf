// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf.Records.V4
{
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), RecordProperty = "CpuType"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)4, RecordProperty = "StdfVersion")]
    public class Far : StdfRecord
    {
        public Far(StdfFile stdfFile) : base(stdfFile)
        {

        }

        public override RecordType RecordType
        {
            get { return new RecordType(0, 10); }
        }

        public byte CpuType { get; set; }
        public byte StdfVersion { get; set; }
    }
}
