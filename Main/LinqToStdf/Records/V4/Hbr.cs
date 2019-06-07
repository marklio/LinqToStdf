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
    public class Hbr : BinSummaryRecord
    {
        public Hbr(StdfFile stdfFile) : base(stdfFile)
        {

        }
        public override RecordType RecordType
        {
            get { return new RecordType(1, 40); }
        }

        public override BinType BinType
        {
            get { return BinType.Hard; }
        }
    }
}
