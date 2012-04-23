// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Attributes {

    /// <summary>
    /// This is the basic attribute used to specify the layout of an STDF record.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These attributes, along with descendant attributes, are placed on a record class
    /// to indicate what fields to expect in the stream and how to interpret them.
    /// When you register a class with a <see cref="RecordConverterFactory"/>,
    /// the factory will use this information to construct converters capable of reading
    /// the record from a stream.
    /// </para>
    /// <para>
    /// In addition to the attributes, you can write your own converters and unconverters for
    /// records that cannot be described by the attributes.
    /// </para>
    /// <para>
    /// FieldType is used to indicate the type of field being parsed.
    /// STDF field types have a very good mapping to CLR primitive types with a few exceptions.
    /// Character arrays map to strings, and have a special attribute (<see cref="StdfStringLayoutAttribute"/>)
    /// which optionally allows you to set a specific length for strings that are not self-length.
    /// </para>
    /// <para>
    /// Other kinds of arrays are treated differently.
    /// If the field is an array, use a <see cref="StdfArrayLayoutAttribute"/> and set
    /// the FieldType to the element type.  Exceptions to this are nibble arrays and
    /// bit arrays
    /// </para>
    /// <para>
    /// For nibble arrays, use <see cref="StdfNibbleArrayLayoutAttribute"/>.
    /// </para>
    /// <para>
    /// Bit arrays are special-cased similarly to strings since they are self-length.
    /// They map to <see cref="System.Collections.BitArray"/>, so you can
    /// use a regular <see cref="StdfFieldLayoutAttribute"/> and set the FieldType
    /// to <see cref="System.Collections.BitArray"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class StdfFieldLayoutAttribute : Attribute {

        private int _FieldIndex;
        /// <summary>
        /// This indicates the order of the fields in the record.
        /// </summary>
        /// <remarks>
        /// For a given record type using automatic parsing,
        /// there must be an attribute with FieldIndex 0, and there must be no gaps
        /// in FieldIndex for all provided attributes.
        /// </remarks>
        public int FieldIndex {
            get { return _FieldIndex; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value", Resources.NegativeFieldIndex);
                }
                _FieldIndex = value;
            }
        }

        /// <summary>
        /// Indicates the type of the field to be parsed.
        /// </summary>
        public virtual Type FieldType { get; set; }

        /// <summary>
        /// This should be the case-sensitive name of a property on the record that the value
        /// will be assigned to.  The property must match the type of FieldType (or be assignable from it),
        /// although there is an exception for making optional value types nullable.
        /// If this isn't set, the value will be thrown away after parsing, which is useful for array length
        /// fields.  However, if you want the record to be writeable, it must be recoverable at write time
        /// via an optional field layout or an array layout.
        /// </summary>
        public string AssignTo { get; set; }

        /// <summary>
        /// Defines the value used to inidicate that the field is mising.  If a parsed value
        /// equals the MissingValue, the AssignTo property will be assigned null
        /// (or a null nullable).  Similarly, if a null AssignTo property must be written, the
        /// null will be written as the MissingValue.
        /// There must me a relevant relationship between the value used and the FieldType.
        /// </summary>
        public object MissingValue { get; set; }

        /// <summary>
        /// This indicates that when a parsed missing value is recognized, the AssignTo property will
        /// be assigned the missing value, not null, if the parsed value is equal to it.  A truly
        /// missing value will result in the AssignTo property being assigned null.  Also, upon writing,
        /// a recognized missing value will be written when at the end of the record, instead of truncating
        /// the record.
        /// </summary>
        public bool PersistMissingValue { get; set; }
    }
}
