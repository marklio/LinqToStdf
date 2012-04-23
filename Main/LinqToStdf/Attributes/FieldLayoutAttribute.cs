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
    /// Character arrays map to strings, and have a special attribute (<see cref="StringFieldLayoutAttribute"/>)
    /// which optionally allows you to set a specific length for strings that are not self-length.
    /// </para>
    /// <para>
    /// Other kinds of arrays are treated differently.
    /// If the field is an array, use a <see cref="ArrayFieldLayoutAttribute"/> and set
    /// the FieldType to the element type.  Exceptions to this are nibble arrays and
    /// bit arrays
    /// </para>
    /// <para>
    /// For nibble arrays, use <see cref="NibbleArrayFieldLayoutAttribute"/>.
    /// </para>
    /// <para>
    /// Bit arrays are special-cased similarly to strings since they are self-length.
    /// They map to <see cref="System.Collections.BitArray"/>, so you can
    /// use a regular <see cref="FieldLayoutAttribute"/> and set the FieldType
    /// to <see cref="System.Collections.BitArray"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class FieldLayoutAttribute : Attribute {

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
        /// The property of the record that corresponds to this field.
        /// </summary>
        /// <remarks>
        /// The name given is case sensitive and must match the property that belongs to the record.
        /// The property definition must match the type of FieldType (or be assignable from it),
        /// although there is an exception for making optional value types nullable.
        /// If this isn't set, the value will be thrown away after parsing, which is useful for array length
        /// fields.  However, if you want the record to be writeable, it must be recoverable at write time
        /// via an optional field layout or an array layout.
        /// </remarks>
        public string RecordProperty { get; set; }

        /// <summary>
        /// Indicates whether the field’s presence in the STDF file is optional.
        /// </summary>
        /// <remarks>
        /// <para>The default value is false.  Setting this to true requires that a
        /// <see cref="MissingValue"/> be defined, but the presence of a <see cref="MissingValue"/> does not
        /// require this to be set to true.</para>
        /// <para>The STDF specification defines the concept of optional field and missing or invalid
        /// data.  Optional fields are those that are not required for a minimally valid STDF file.  The
        /// LinqToStdf implementation diverges slightly from this concept in that missing or invalid fields
        /// are represented as null when the record is in object form.  That divergence leads to a separation
        /// of the concepts of optional and simply having a missing/invalid placeholder value.</para>
        /// <para>Non-optional fields must be present in the STDF file.  When a non-optional field is unable to
        /// be read, conversion of the record will fail, as the record does not meet the requirements of a
        /// "minimally valid" record, per the STDF specification.  When a non-optional field’s property is null and a
        /// write is attempted, either the <see cref="MissingValue"/> will be used, or the writing of the record will fail.</para>
        /// <para>Optional fields may be present in the STDF file, but are not required when found at the end
        /// of the record.  When an optional field is unable to be read, null is assigned to the corresponding
        /// property.  When an optional field’s property is null and a write is attempted, either the <see cref="MissingValue"/>
        /// will be used, or the field will not be written if at the end of the record.</para>
        /// </remarks>
        public bool IsOptional { get; set; }

        /// <summary>
        /// Defines the value used to inidicate that the field is mising or invalid.
        /// </summary>
        /// <remarks>
        /// <para>There must me a relevant relationship between the value used and the FieldType.</para>
        /// <para>The STDF specification clearly defines the concept of a value to use to
        /// represent missing or invalid data when a set of bytes must be written to an STDF file.
        /// This defines that value.  On reading an STDF file, when an optional field's value is
        /// found equal to that of MissingValue, the corresponding property (<see cref="RecordProperty"/>) is
        /// set to null, unless the value should be persisted (<see cref="PersistMissingValue"/>).
        /// On writing an STDF file, when an optional field's property (<see cref="RecordProperty"/>)
        /// is null, the value written for the field will either be that defined here, or the field
        /// will be left off of the end of the record if possible.</para>
        /// </remarks>
        public object MissingValue { get; set; }

        /// <summary>
        /// Indicates the value that indicates a missing or invalid field is also potentially valid, and should
        /// thus be persisted across conversion from file to object, or object to file.
        /// </summary>
        /// <remarks>
        /// Under default circumstances, a value recognized as the one representing a missing or invalid field
        /// will be converted into a null upon read, or possibly truncated on write.  Setting this property
        /// will allow a recognized missing value to persist, resulting in a property set to the value (instead
        /// of null) when it is read from the file, or will force the value to be written to the file when it
        /// it is assigned to the property (as opposed to potentially being truncated).
        /// </remarks>
        public bool PersistMissingValue { get; set; }
    }
}
