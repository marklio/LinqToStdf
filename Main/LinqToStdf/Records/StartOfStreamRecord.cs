// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf.Records
{

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
    public class StartOfStreamRecord : StdfRecord
    {
        public StartOfStreamRecord(StdfFile stdfFile) : base(stdfFile)
        {

        }

        public override RecordType RecordType
        {
            get { throw new NotSupportedException(); }
        }

        public string FileName { get; set; }
        public Endian Endian { get; set; }
        public long? ExpectedLength { get; set; }

        public override bool IsWritable { get { return false; } }
    }
}
