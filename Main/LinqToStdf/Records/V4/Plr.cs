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
    StdfArrayLayout(FieldIndex = 4, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = "", AssignTo = "ProgramStatesRight"),
    StdfArrayLayout(FieldIndex = 5, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = "", AssignTo = "ReturnStatesRight"),
    StdfArrayLayout(FieldIndex = 6, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = "", AssignTo = "ProgramStatesLeft"),
    StdfArrayLayout(FieldIndex = 7, FieldType = typeof(string), ArrayLengthFieldIndex = 0, AllowTruncation = true, MissingValue = "", AssignTo = "ReturnStatesLeft")]
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
                // Group count and list of group indexes are required
                ushort groupCount = reader.ReadUInt16();
                plr.GroupIndexes = reader.ReadUInt16Array(groupCount, true);

                // Latter arrays are optional, and may be truncated
                if (!reader.AtEndOfStream)
                    plr.GroupModes = reader.ReadUInt16Array(groupCount, false);
                if (!reader.AtEndOfStream)
                    plr.GroupRadixes = reader.ReadByteArray(groupCount, false);
                if (!reader.AtEndOfStream)
                    plr.ProgramStatesRight = reader.ReadStringArray(groupCount, false);
                if (!reader.AtEndOfStream)
                    plr.ReturnStatesRight = reader.ReadStringArray(groupCount, false);
                if (!reader.AtEndOfStream)
                    plr.ProgramStatesLeft = reader.ReadStringArray(groupCount, false);
                if (!reader.AtEndOfStream)
                    plr.ReturnStatesLeft = reader.ReadStringArray(groupCount, false);
            }
            return plr;
        }

        internal static UnknownRecord ConvertFromPlr(StdfRecord record, Endian endian) {
            Plr plr = (Plr)record;
            using (MemoryStream stream = new MemoryStream()) {
                // Writing the PLR backwards
                BinaryWriter writer = new BinaryWriter(stream, endian, true);

                // Get GroupIndexes length, which must be consistent with all other arrays, except for the last one present, which may be truncated
                if (plr.GroupIndexes == null)
                    throw new InvalidOperationException(String.Format(Resources.NonNullableField, 1, typeof(Plr)));

                int groupCount = plr.GroupIndexes.Length;
                if (groupCount > UInt16.MaxValue)
                    throw new InvalidOperationException(String.Format(Resources.ArrayTooLong, UInt16.MaxValue, 1, typeof(Plr)));

                bool fieldsWritten = false;

                // Field 7: ReturnStatesLeft
                if (plr.ReturnStatesLeft != null) {
                    // Check for larger group length (writing has definitely not occurred yet)
                    if (plr.ProgramStatesLeft.Length > groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 7));
                    // Write the field
                    writer.WriteStringArray(plr.ReturnStatesLeft);
                    fieldsWritten = true;
                }
                else if (fieldsWritten) {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 6: ProgramStatesLeft
                if (plr.ProgramStatesLeft != null) {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.ProgramStatesLeft.Length > groupCount) || (fieldsWritten && (plr.ProgramStatesLeft.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 6));
                    // Write the field
                    writer.WriteStringArray(plr.ProgramStatesLeft);
                    fieldsWritten = true;
                }
                else if (fieldsWritten) {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 5: ReturnStatesRight
                if (plr.ReturnStatesRight != null) {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.ReturnStatesRight.Length > groupCount) || (fieldsWritten && (plr.ReturnStatesRight.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 5));
                    // Write the field
                    writer.WriteStringArray(plr.ReturnStatesRight);
                    fieldsWritten = true;
                }
                else if (fieldsWritten) {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 4: ProgramStatesRight
                if (plr.ProgramStatesRight != null) {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.ProgramStatesRight.Length > groupCount) || (fieldsWritten && (plr.ProgramStatesRight.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 4));
                    // Write the field
                    writer.WriteStringArray(plr.ProgramStatesRight);
                    fieldsWritten = true;
                }
                else if (fieldsWritten) {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 3: GroupRadixes
                if (plr.GroupRadixes != null) {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.GroupRadixes.Length > groupCount) || (fieldsWritten && (plr.GroupRadixes.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 3));
                    // Write the field
                    writer.WriteByteArray(plr.GroupRadixes);
                    fieldsWritten = true;
                }
                else if (fieldsWritten) {
                    // Fill an array of missing values and write
                    byte[] arr = new byte[groupCount];
                    Array.ForEach<byte>(arr, delegate(byte a) { a = Byte.MinValue; });
                    writer.WriteByteArray(arr);
                }

                // Field 2: GroupModes
                if (plr.GroupModes != null) {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.GroupModes.Length > groupCount) || (fieldsWritten && (plr.GroupModes.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 2));
                    // Write the field
                    writer.WriteUInt16Array(plr.GroupModes);
                    fieldsWritten = true;
                }
                else if (fieldsWritten) {
                    // Fill an array of missing values and write
                    ushort[] arr = new ushort[groupCount];
                    Array.ForEach<ushort>(arr, delegate(ushort a) { a = UInt16.MinValue; });
                    writer.WriteUInt16Array(arr);
                }

                // Field 1: GroupIndexes
                // Check for larger, or not equal group length ifwriting has occurred
                if ((plr.GroupIndexes.Length > groupCount) || (fieldsWritten && (plr.GroupIndexes.Length != groupCount)))
                    throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 1));
                // Write the field
                writer.WriteUInt16Array(plr.GroupIndexes);
                fieldsWritten = true;

                // Field 0: Group Count
                writer.WriteUInt16((ushort)groupCount);

                // Reverse bytes in stream
                long length = stream.Length;
                if (length > UInt16.MaxValue)
                    throw new InvalidOperationException(Resources.RecordTooLong);
                byte[] sa = stream.ToArray();
                Array.Reverse(sa, 0, (int)length);

                return new UnknownRecord(plr.RecordType, sa, endian);
            }
        }
    }
}
