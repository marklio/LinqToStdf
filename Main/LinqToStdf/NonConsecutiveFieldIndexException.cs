// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {

	/// <summary>
	/// Indicates a record has non-consecutive <see cref="Attributes.FieldLayoutAttribute.FieldIndex"/>'s declared in its metadata.
	/// </summary>
	public class NonconsecutiveFieldIndexException : Exception {
		//
		// For guidelines regarding the creationg of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public NonconsecutiveFieldIndexException() : base() { }
		public NonconsecutiveFieldIndexException(Type type) : this(string.Format(Resources.NonConsecutiveFieldIndexDefault, type)) { }
		public NonconsecutiveFieldIndexException(string message) : base(message) { }
		public NonconsecutiveFieldIndexException(string message, Exception inner) : base(message, inner) { }
	}
}
