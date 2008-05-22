using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records {

    /// <summary>
    /// Identifies a chunk of "corrupted" data.
    /// </summary>
    public class CorruptDataRecord : FormatErrorRecord {

        public override string Message {
            get {
                return base.Message ?? string.Format("{0} bytes of corrupt data found at offset {1}", (CorruptData ?? new byte[0]).Length, Offset);
            }
        }

        public byte[] CorruptData { get; set; }
    }
}
