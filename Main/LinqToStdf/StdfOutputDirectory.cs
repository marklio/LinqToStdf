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

namespace LinqToStdf {

    /// <summary>
    /// Provides a "larger scale" STDF writing mechanism.
    /// Rather than providing a "per file" API, StdfOutputDirectory
    /// represents a directory that STDF files will be written to.
    /// <see cref="StartOfStreamRecord"/> and <see cref="EndOfStreamRecord"/>
    /// are used to indicate the start and end a file.
    /// </summary>
    public class StdfOutputDirectory {
        readonly string _Path;

        /// <summary>
        /// Creates an StdfOutputDirectory using the given path as a root directory.
        /// </summary>
        /// <param name="path">The directory to use as the root.  It must exist.</param>
        public StdfOutputDirectory(string path) {
            if (!Directory.Exists(path)) {
                throw new DirectoryNotFoundException(string.Format(Resources.DirectoryNotFound, path));
            }
            _Path = path;
        }

        /// <summary>
        /// Consumes a stream of records to write to the output directory.
        /// Files cannot span calls to this method.
        /// </summary>
        /// <param name="records">The records to write.</param>
        public void WriteRecords(IEnumerable<StdfRecord> records) {
            StdfFileWriter writer = null;
            foreach (var r in records) {
                if (r.GetType() == typeof(StartOfStreamRecord)) {
					var sos = (StartOfStreamRecord)r;
                    if (writer != null) {
                        throw new InvalidOperationException(Resources.SOFBeforeEOF);
                    }
                    writer = new StdfFileWriter(Path.Combine(_Path, sos.FileName), sos.Endian);
                }
                else if (r.GetType() == typeof(EndOfStreamException)) {
                    EnsureWriter(writer);
                    writer.Dispose();
                    writer = null;
                }
                else {
                    EnsureWriter(writer);
                    writer.WriteRecord(r);
                }
            }
            if (writer != null) {
                throw new InvalidOperationException(Resources.EndWithoutEOS);
            }
        }

        static void EnsureWriter(StdfFileWriter writer) {
            if (writer == null) throw new InvalidOperationException(Resources.WriteOutsideSOSEOS);
        }
    }
}
