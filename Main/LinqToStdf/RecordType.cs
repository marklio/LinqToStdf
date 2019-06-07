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
    /// Encapsulates the record type and subtype.  Implements all the bells and whistles
    /// for comparing, and equality and using in a hashtable, as well as all the
    /// expected operators
    /// </summary>
    public struct RecordType : IComparable, IComparable<RecordType>, IEquatable<RecordType>
    {

        /// <summary>
        /// Constructs a record type with an STDF type and subtype
        /// </summary>
        /// <param name="type">The STDF type</param>
        /// <param name="subtype">The STDF subtype</param>
        public RecordType(byte type, byte subtype)
        {
            this.Type = type;
            this.Subtype = subtype;
        }

        /// <summary>
        /// The STDF type
        /// </summary>
        public byte Type { get; }

        /// <summary>
        /// The STDF subtype
        /// </summary>
        public byte Subtype { get; }

        /// <summary>
        /// Overrides <see cref="Object.Equals(object)"/> appropriately
        /// </summary>
        /// <param name="obj">the object to compare to</param>
        /// <returns>true if the instance is equal to <paramref name="obj"/>, otherwise false</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is RecordType))
            {
                return false;
            }
            return Equals((RecordType)obj);
        }

        /// <summary>
        /// Gets an appropriate hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return (Type << 24) | Subtype;
        }

        /// <summary>
        /// Supplies an appropriate string representation of this instance.
        /// </summary>
        public override string ToString()
        {
            return string.Format("StdfRecord:{0}:{1}", this.Type, this.Subtype);
        }

        #region IEquatable<RecordType> Members

        /// <summary>
        /// Implements equality
        /// </summary>
        /// <param name="other">the RecordType to compare to</param>
        /// <returns>true if the instance is equal to <paramref name="other"/>, otherwise false</returns>
        public bool Equals(RecordType other)
        {
            return (this.Type == other.Type && this.Subtype == other.Subtype);
        }

        #endregion

        #region IComparable<RecordType> Members

        /// <summary>
        /// Implements comparability
        /// </summary>
        /// <param name="other">the RecordType to compare to</param>
        /// <returns>the standard comparison values</returns>
        /// <seealso cref="IComparable.CompareTo"/>
        public int CompareTo(RecordType other)
        {
            int value = this.Type.CompareTo(other.Type);
            if (value == 0)
            {
                value = this.Subtype.CompareTo(other.Subtype);
            }
            return value;
        }

        #endregion

        #region IComparable Members

        /// <summary>
        /// Implements comparability
        /// </summary>
        /// <param name="obj">the RecordType to compare to</param>
        /// <returns>the standard comparison values</returns>
        /// <seealso cref="IComparable.CompareTo"/>
        public int CompareTo(object obj)
        {
            return CompareTo((RecordType)obj);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Equality operator
        /// </summary>
        public static bool operator ==(RecordType first, RecordType second)
        {
            return first.Equals(second);
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        public static bool operator !=(RecordType first, RecordType second)
        {
            return !first.Equals(second);
        }

        /// <summary>
        /// Greater than operator
        /// </summary>
        public static bool operator >(RecordType first, RecordType second)
        {
            return first.CompareTo(second) > 0;
        }

        /// <summary>
        /// Less than operator
        /// </summary>
        public static bool operator <(RecordType first, RecordType second)
        {
            return first.CompareTo(second) < 0;
        }

        /// <summary>
        /// Greater than or equal to operator
        /// </summary>
        public static bool operator >=(RecordType first, RecordType second)
        {
            return first.CompareTo(second) >= 0;
        }

        /// <summary>
        /// Less than or equal to operator
        /// </summary>
        public static bool operator <=(RecordType first, RecordType second)
        {
            return first.CompareTo(second) <= 0;
        }

        #endregion
    }
}
