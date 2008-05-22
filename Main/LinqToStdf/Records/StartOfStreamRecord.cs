using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records {

    /// <summary>
    /// Indicates the beginning of a stream of records.
    /// Carries information that may be useful to a RecordFilter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// StartOfStreamRecord, along with <see cref="EndOfStreamRecord"/>,
    /// is mainly an implementation detail,
    /// but it is exposed because it might be useful to implementations
    /// leveraging this library.
    /// </para>
    /// <para>
    /// For instance, a record consumer might use the presence of SOS and EOS
    /// to enable processing of multiple files, while being able to tell
    /// which files a record came from.
    /// </para>
    /// </remarks>
    public class StartOfStreamRecord : StdfRecord {

        public override RecordType RecordType {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        public string FileName { get; set; }
        public Endian Endian { get; set; }

        public override bool IsWritable { get { return false; } }
    }
}
