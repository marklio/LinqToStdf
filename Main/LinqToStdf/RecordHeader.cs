// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf {

	/// <summary>
	/// Encapsulates an STDF record header
	/// </summary>
	public struct RecordHeader {

		/// <summary>
		/// Constructs a new record header
		/// </summary>
		public RecordHeader(ushort length, RecordType recordType) {
			this._Length = length;
			this._RecordType = recordType;
		}

		private ushort _Length;
		/// <summary>
		/// The length of the record
		/// </summary>
		public ushort Length {
			get { return _Length; }
		}

		private RecordType _RecordType;
		/// <summary>
		/// The <see cref="RecordType"/> of the record.
		/// </summary>
		public RecordType RecordType {
			get { return _RecordType; }
		}
	}
}
