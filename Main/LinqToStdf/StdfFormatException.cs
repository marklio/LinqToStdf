// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LinqToStdf.Records;

namespace LinqToStdf {

    /// <summary>
    /// Indicates there was a problem with the format of the file.
    /// </summary>
    /// <remarks>
    /// By default, this exception is thrown when format errors are encountered.
    /// Alternatively, <see cref="StdfFile.ThrowOnFormatError"/> can be set to false,
    /// which will cause a <see cref="FormatErrorRecord"/> to be pushed through the
    /// STDF stream instead.  This would allow an application to be more tolerant
    /// of corrupted data.
    /// </remarks>
    public class StdfFormatException : StdfException {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public StdfFormatException() { }
        public StdfFormatException(string? message) : base(message) { }
        public StdfFormatException(string? message, Exception inner) : base(message, inner) { }
    }
}
