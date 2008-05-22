using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Attributes {

    /// <summary>
    /// Indicates that the field is an array of the <see cref="StdfFieldLayoutAttribute.FieldType"/>.
    /// The length is provided by another field whose index is indicated by <see cref="ArrayLengthFieldIndex"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class StdfArrayLayoutAttribute : StdfFieldLayoutAttribute {

        /// <summary>
        /// Indicates the field that provides the length information for the array.
        /// </summary>
        public int ArrayLengthFieldIndex { get; set; }
    }
}
