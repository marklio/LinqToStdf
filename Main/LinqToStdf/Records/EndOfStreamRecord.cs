// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records {

    /// <summary>
    /// Indicates that the end of the stream of records has been reached.
    /// Can be used most notably to trigger the generation of a missing Mrr.
    /// </summary>
    public class EndOfStreamRecord : StdfRecord {

        public EndOfStreamRecord()
        {
            Synthesized = true;
        }

        public override RecordType RecordType {
            get { throw new NotSupportedException(); }
        }

        public override bool IsWritable { get { return false; } }
    }
}
