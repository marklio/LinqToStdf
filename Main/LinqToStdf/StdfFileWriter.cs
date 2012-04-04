// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LinqToStdf.Records;
using LinqToStdf.Records.V4;

namespace LinqToStdf {

    /// <summary>
    /// StdfFileWriter provides a "what you expect" API for writing STDF files.
    /// You provide a path and then call <see cref="WriteRecord"/> or <see cref="WriteRecords"/>
    /// to write to the file.
    /// You can provide an endianness, or have the endianness inferred from the first record
    /// (which must be either <see cref="StartOfStreamRecord"/> or <see cref="Far"/>.
    /// </summary>
    /// <seealso cref="StdfOutputDirectory"/>
    public sealed class StdfFileWriter : IDisposable {

        RecordConverterFactory _Factory;
        Stream _Stream;
        Endian _Endian;
        public StdfFileWriter(string path, Endian endian, bool debug = false)
        {
            _Stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            _Endian = endian;
            if (debug)
            {
                _Factory = new RecordConverterFactory() { Debug = debug };
                StdfV4Specification.RegisterRecords(_Factory);
            }
            else
            {
                _Factory = new RecordConverterFactory(StdfFile._V4ConverterFactory);
            }
        }

        public StdfFileWriter(string path, bool debug = false) : this(path, Endian.Unknown, debug) { }

        /// <summary>
        /// Writes a single record to the file
        /// </summary>
        /// <param name="record"></param>
        public void WriteRecord(StdfRecord record) {
            if (_Endian == Endian.Unknown) {
                //we must be able to infer the endianness based on the first record
                if (record.GetType() == typeof(StartOfStreamRecord)) {
					var sos = (StartOfStreamRecord)record;
                    _Endian = sos.Endian;
                    return;
                }
                else if (record.GetType() == typeof(Far)) {
                    InferEndianFromFar((Far)record);
                }
                if (_Endian == Endian.Unknown) throw new InvalidOperationException(Resources.CannotInferEndianness);
            }
            if (record.IsWritable) {
                var writer = new LinqToStdf.BinaryWriter(_Stream, _Endian, false);
                var ur = _Factory.Unconvert(record, _Endian);
                writer.WriteHeader(new RecordHeader((ushort)ur.Content.Length, ur.RecordType));
                _Stream.Write(ur.Content, 0, ur.Content.Length);
            }
        }

        private void InferEndianFromFar(Far far) {
            switch (far.CpuType) {
                case 0:
                case 1:
                    _Endian = Endian.Big;
                    break;
                default:
                    _Endian = Endian.Little;
                    break;
            }
        }

        /// <summary>
        /// Writes a stream of records to the file.
        /// </summary>
        /// <param name="records"></param>
        public void WriteRecords(IEnumerable<StdfRecord> records) {
            foreach (var r in records) {
                WriteRecord(r);
            }
        }

        #region IDisposable Members

        public void Dispose() {
            if (_Stream != null) _Stream.Dispose();
        }

        #endregion
    }
}
