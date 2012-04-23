// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Attributes {

    /// <summary>
    /// Indicates that the field is a timestamp.  The result will be a
    /// <see cref="DateTime"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class TimeFieldLayoutAttribute : FieldLayoutAttribute {

        public TimeFieldLayoutAttribute() {
            base.FieldType = typeof(DateTime);
            base.MissingValue = TimeFieldLayoutAttribute.Epoch;
        }

        /// <summary>
        /// The epoch used for STDF dates
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Overriden to be locked to string. setting is an invalid operation.
        /// </summary>
        public override Type FieldType {
            get { return base.FieldType; }
            set {
                if (value != typeof(DateTime)) {
                    throw new InvalidOperationException(Resources.TimeFieldLayoutNonDateTime);
                }
            }
        }
    }
}
