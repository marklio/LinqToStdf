// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LinqToStdf.Records;

namespace LinqToStdf
{

    /// <summary>
    /// Base exception type for exceptions resulting from the parsing of an STDF file.
    /// </summary>
    public class StdfException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public StdfException() { }
        public StdfException(string message) : base(message) { }
        public StdfException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// If applicable, the <see cref="ErrorRecord"/> associated with the record.
        /// </summary>
        public ErrorRecord ErrorRecord { get; set; }
    }
}
