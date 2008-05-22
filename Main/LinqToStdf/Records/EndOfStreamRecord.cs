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

        public override RecordType RecordType {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        public override bool IsWritable { get { return false; } }
    }
}
