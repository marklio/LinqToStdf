// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(ushort)),
    StdfArrayLayout(FieldIndex = 1, FieldType = typeof(ushort), ArrayLengthFieldIndex = 0, AssignTo = "GroupIndexes"),
    StdfArrayLayout(FieldIndex = 2, FieldType = typeof(ushort), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = ushort.MinValue, AssignTo = "GroupModes"),
    StdfArrayLayout(FieldIndex = 3, FieldType = typeof(byte), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = byte.MinValue, AssignTo = "GroupRadixes"),
    StdfArrayLayout(FieldIndex = 4, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = String.Empty, AssignTo = "ProgramStatesRight"),
    StdfArrayLayout(FieldIndex = 5, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = String.Empty, AssignTo = "ReturnStatesRight"),
    StdfArrayLayout(FieldIndex = 6, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = String.Empty, AssignTo = "ProgramStatesLeft"),
    StdfArrayLayout(FieldIndex = 7, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = String.Empty, AssignTo = "ReturnStatesLeft")]
    public class Plr : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 63); }
        }

        public ushort[] GroupIndexes { get; set; }
        public ushort[] GroupModes { get; set; }
        public byte[] GroupRadixes { get; set; }
        public string[] ProgramStatesRight { get; set; }
        public string[] ReturnStatesRight { get; set; }
        public string[] ProgramStatesLeft { get; set; }
        public string[] ReturnStatesLeft { get; set; }

        internal static Plr ConvertToPlr(UnknownRecord unknownRecord) {
            Plr plr = new Plr();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(unknownRecord.Content), unknownRecord.Endian, true)) {
                ushort groupCount = reader.ReadUInt16();
                if (groupCount > 0) {
                    // TODO: Now that we have end-of-stream tolerant uint16, byte and variable-length string array readers, this maybe could be code-generated...
                    plr.GroupIndexes = reader.ReadUInt16Array(groupCount);
                    plr.GroupModes = reader.ReadUInt16Array(groupCount, false);
                    plr.GroupRadixes = reader.ReadByteArray(groupCount, false);
                    plr.ProgramStatesRight = reader.ReadStringArray(groupCount, false);
                    plr.ReturnStatesRight = reader.ReadStringArray(groupCount, false);
                    plr.ProgramStatesLeft = reader.ReadStringArray(groupCount, false);
                    plr.ReturnStatesLeft = reader.ReadStringArray(groupCount, false);
                }
            }
            return plr;
        }

        internal static UnknownRecord ConvertFromPlr(StdfRecord record, Endian endian) {
            Plr plr = (Plr)record;
            using (MemoryStream stream = new MemoryStream()) {
                BinaryWriter writer = new BinaryWriter(stream, endian, true);

                throw new NotImplementedException("Plr uncoversion is ugly.");

                // The last array field in the record is allowed to be truncated instead of padding the end with missing items
                // Array elements are written in reverse, because writer is in backwards mode
                // The not-last arrays can have larger lengths, but those lengths must match
                // The maximum array's length is written
                int fieldsWritten = 0;
                int groupCount = 0;


                if (plr.ReturnStatesLeft != null) {
                    writer.WriteStringArray(plr.ReturnStatesLeft);
                    fieldsWritten += 1;
                    groupCount = plr.ReturnStatesLeft.Length;
                }

                if (plr.ProgramStatesLeft != null) {
                    if (((fieldsWritten > 0) && (plr.ProgramStatesLeft.Length < groupCount)) || ((fieldsWritten > 1) && (plr.ProgramStatesLeft.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 6));
                    writer.WriteStringArray(plr.ProgramStatesLeft);
                    fieldsWritten += 1;
                    groupCount = plr.ProgramStatesLeft.Length;
                }
                else {
                    throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 6));
                }

                return new UnknownRecord(plr.RecordType, stream.ToArray(), endian);
            }
        }
    }
}
