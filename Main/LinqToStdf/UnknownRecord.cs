// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LinqToStdf {

    /// <summary>
    /// <para>
    /// Encapsulates an "unknown" STDF record.
    /// </para>
    /// <para>
    /// Unknown records carry around the byte contents of the original record,
    /// plus the record type and endianess of the origin stream.
    /// </para>
    /// <para>
    /// If one is produced from the parser, it means an appropriate converter was not present in the <see cref="RecordConverterFactory"/>.
    /// If this is unexpected, you can add the <see cref="BuiltInFilters.ExpectOnlyKnownRecords"/>, which will treat unknown records
    /// as corrupt data.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Under the covers, a record reader reads the STDF file and produces <see cref="UnknownRecord">UnknownRecords</see>.
    /// These records are then converted to concrete record types via a <see cref="RecordConverterFactory"/>.
    /// </remarks>
    public class UnknownRecord : StdfRecord {

        /// <summary>
        /// Constructs an unknown record
        /// </summary>
        /// <param name="recordType">The <see cref="RecordType"/> for the record</param>
        /// <param name="content">The original byte content of the record</param>
        /// <param name="endian">The endian-ness of <paramref name="content"/></param>
        public UnknownRecord(RecordType recordType, byte[] content, Endian endian) {
            this._RecordType = recordType;
            this._Content = content;
            this._Endian = endian;
        }

        private readonly RecordType _RecordType;
        /// <summary>
        /// The records <see cref="RecordType"/>
        /// </summary>
        public override RecordType RecordType {
            get { return this._RecordType; }
        }

        private readonly Endian _Endian;
        /// <summary>
        /// The endian-ness of <see cref="Content"/>
        /// </summary>
        public Endian Endian {
            get { return _Endian; }
        }
        
        private readonly byte[] _Content;
        /// <summary>
        /// The original byte content of the record
        /// </summary>
        public byte[] Content {
            get { return _Content; }
        }

        /// <summary>
        /// <para>
        /// This is a helper function used by the LCG code
        /// to simplify the process of ensuring that the provided
        /// target record is suitable for conversion.
        /// </para>
        /// <para>
        /// Throws InvalidRecordConversionException if the conversion cannot succeed.
        /// </para>
        /// </summary>
        /// <param name="record">The record that is the target for conversion</param>
        /// <exception cref="InvalidRecordConversionException"/>
        public void EnsureConvertibleTo(StdfRecord record) {
            if (!RecordType.Equals(record.RecordType)) {
                throw new InvalidRecordConversionException();
            }
        }

        /// <summary>
        /// Helper method for the LCG code that returns a <see cref="BinaryReader"/>
        /// over the contents of the record. (be sure and dispose it).
        /// </summary>
        /// <returns></returns>
        public BinaryReader GetBinaryReaderForContent() {
            return new BinaryReader(new MemoryStream(_Content, writable: false), _Endian, ownsStream: true);
        }
    }
}
