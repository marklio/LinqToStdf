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
    /// Indicates there was a format error in the STDF stream.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format errors are typically fatal to the parser,
    /// as some fundamental invariant of the STDF format has been broken.
    /// However, parsing can sometimes proceed based on some assumptions.
    /// It is dangerous to put faith in the contents of such a file,
    /// but often, proceeding can be useful to determine the cause of
    /// corruption.
    /// </para>
    /// </remarks>
    public class FormatErrorRecord : ErrorRecord {
        public bool Recoverable { get; set; }

        public override StdfException ToException() {
            return new StdfFormatException(Message ?? "Unknown Format Error") { ErrorRecord = this };
        }
    }
}
