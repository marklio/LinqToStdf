// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Attributes {

    /// <summary>
    /// This indicates that the field to be parsed is a nibble array.
    /// The result will be a byte[] that has been expanded so that each nibble
    /// is represented by a separate byte.
    /// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class StdfNibbleArrayLayoutAttribute : StdfArrayLayoutAttribute {

        public override Type FieldType {
            get { return typeof(byte); }
            set { throw new NotSupportedException(); }
        }
	}
}
