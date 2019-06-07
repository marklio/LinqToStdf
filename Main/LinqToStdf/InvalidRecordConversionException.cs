// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf
{

    /// <summary>
    /// Indicates that a conversion is of the wrong type.  This means a conversion was passed
    /// a record whose <see cref="RecordType"/> is different than the record type the conversion
    /// understands.
    /// </summary>
    public class InvalidRecordConversionException : InvalidOperationException
    {
        //
        // For guidelines regarding the creationg of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public InvalidRecordConversionException() : base(Resources.InvalidRecordConverstionDefault) { }
        public InvalidRecordConversionException(string message) : base(message) { }
        public InvalidRecordConversionException(string message, Exception inner) : base(message, inner) { }
    }
}
