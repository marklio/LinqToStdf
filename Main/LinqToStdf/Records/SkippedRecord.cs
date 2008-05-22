using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.Records {
    /// <summary>
    /// This record represents a record that was skipped.
    /// Currently, records can be skipped as a result of
    /// compiled queries.
    /// </summary>
    public class SkippedRecord : StdfRecord {

        public SkippedRecord(Type skippedType) {
            SkippedType = skippedType;
        }

        public override RecordType RecordType {
            get { return new RecordType(); }
        }

        public Type SkippedType { get; private set; }
    }
}
