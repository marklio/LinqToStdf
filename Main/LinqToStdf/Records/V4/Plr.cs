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
                ushort groupCount = reader.ReadUInt16();
                if (groupCount > 0) {
                    // TODO: Now that we have end-of-stream tolerant uint16, byte and variable-length string array readers, this maybe could be code-generated...
                    plr.GroupIndexes = reader.ReadUInt16Array(groupCount, true);
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
            }
            return plr;
        }

        internal static UnknownRecord ConvertFromPlr(StdfRecord record, Endian endian) {
            Plr plr = (Plr)record;
            using (MemoryStream stream = new MemoryStream()) {
                BinaryWriter writer = new BinaryWriter(stream, endian, true);

                // The last array field in the record is allowed to be truncated instead of padding the end with missing items
                // Array elements are written in reverse, because writer is in backwards mode
                // The not-last arrays can have larger lengths, but those lengths must match
                // The GroupIndexes array's length is always the one written
                int groupCount = 0;
                int fieldIndex = 7;
                int fieldsWritten = 0;

                // TODO: Make this CodeGen'ed (I'm following a pattern here)
                if (plr.ReturnStatesLeft != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.ReturnStatesLeft.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.ReturnStatesLeft.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteStringArray(plr.ReturnStatesLeft);
                    // Keep track of the shared array length
                    groupCount = plr.ReturnStatesLeft.Length;
                    fieldsWritten += 1;
                }
                else if (fieldsWritten > 0) {
                    // Here's where the transition to CodeGen breaks down, if a field has already been written and we
                    //  need to fill with a missing values array, we don't know for certain how long it should be, so
                    //  we look "ahead" to plr.GroupIndexes.Length, which must be the correct length
                    if (fieldsWritten == 1)
                        groupCount = plr.GroupIndexes.Length;
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }
                fieldIndex -= 1;

                if (plr.ProgramStatesLeft != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.ProgramStatesLeft.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.ProgramStatesLeft.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteStringArray(plr.ProgramStatesLeft);
                    // Keep track of the shared array length
                    groupCount = plr.ProgramStatesLeft.Length;
                    fieldsWritten += 1;
                }
                else if (fieldsWritten > 0) {
                    // Here's where the transition to CodeGen breaks down, if a field has already been written and we
                    //  need to fill with a missing values array, we don't know for certain how long it should be, so
                    //  we look "ahead" to plr.GroupIndexes.Length, which must be the correct length
                    if (fieldsWritten == 1)
                        groupCount = plr.GroupIndexes.Length;
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }
                fieldIndex -= 1;

                if (plr.ReturnStatesRight != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.ReturnStatesRight.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.ReturnStatesRight.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteStringArray(plr.ReturnStatesRight);
                    // Keep track of the shared array length
                    groupCount = plr.ReturnStatesRight.Length;
                    fieldsWritten += 1;
                }
                else if (fieldsWritten > 0) {
                    // Here's where the transition to CodeGen breaks down, if a field has already been written and we
                    //  need to fill with a missing values array, we don't know for certain how long it should be, so
                    //  we look "ahead" to plr.GroupIndexes.Length, which must be the correct length
                    if (fieldsWritten == 1)
                        groupCount = plr.GroupIndexes.Length;
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }
                fieldIndex -= 1;

                if (plr.ProgramStatesRight != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.ProgramStatesRight.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.ProgramStatesRight.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteStringArray(plr.ProgramStatesRight);
                    // Keep track of the shared array length
                    groupCount = plr.ProgramStatesRight.Length;
                    fieldsWritten += 1;
                }
                else if (fieldsWritten > 0) {
                    // Here's where the transition to CodeGen breaks down, if a field has already been written and we
                    //  need to fill with a missing values array, we don't know for certain how long it should be, so
                    //  we look "ahead" to plr.GroupIndexes.Length, which must be the correct length
                    if (fieldsWritten == 1)
                        groupCount = plr.GroupIndexes.Length;
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate(string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }
                fieldIndex -= 1;

                if (plr.GroupRadixes != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.GroupRadixes.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.GroupRadixes.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteByteArray(plr.GroupRadixes);
                    // Keep track of the shared array length
                    groupCount = plr.GroupRadixes.Length;
                    fieldsWritten += 1;
                }
                else if (fieldsWritten > 0) {
                    // Here's where the transition to CodeGen breaks down, if a field has already been written and we
                    //  need to fill with a missing values array, we don't know for certain how long it should be, so
                    //  we look "ahead" to plr.GroupIndexes.Length, which must be the correct length
                    if (fieldsWritten == 1)
                        groupCount = plr.GroupIndexes.Length;
                    // Fill an array of missing values and write
                    byte[] arr = new byte[groupCount];
                    Array.ForEach<byte>(arr, delegate(byte a) { a = byte.MinValue; });
                    writer.WriteByteArray(arr);
                }
                fieldIndex -= 1;

                if (plr.GroupModes != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.GroupModes.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.GroupModes.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteUInt16Array(plr.GroupModes);
                    // Keep track of the shared array length
                    groupCount = plr.GroupModes.Length;
                    fieldsWritten += 1;
                }
                else if (fieldsWritten > 0) {
                    // Here's where the transition to CodeGen breaks down, if a field has already been written and we
                    //  need to fill with a missing values array, we don't know for certain how long it should be, so
                    //  we look "ahead" to plr.GroupIndexes.Length, which must be the correct length
                    if (fieldsWritten == 1)
                        groupCount = plr.GroupIndexes.Length;
                    // Fill an array of missing values and write
                    ushort[] arr = new ushort[groupCount];
                    Array.ForEach<ushort>(arr, delegate(ushort a) { a = ushort.MinValue; });
                    writer.WriteUInt16Array(arr);
                }
                fieldIndex -= 1;

                if (plr.GroupIndexes != null) {
                    // No field can ever have a smaller length than the one that follows it
                    if (plr.GroupIndexes.Length < groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // The last field can have different (smaller) groupCount, but no others can
                    if ((fieldsWritten > 1) && (plr.GroupIndexes.Length != groupCount))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, fieldIndex));
                    // Write the field
                    writer.WriteUInt16Array(plr.GroupIndexes);
                    // Keep track of the shared array length
                    groupCount = plr.GroupIndexes.Length;
                    fieldsWritten += 1;
                }
                else {
                    throw new InvalidOperationException(String.Format(Resources.NonNullableField, fieldIndex, 7));
                }
                fieldIndex -= 1;

                writer.WriteUInt16((ushort)groupCount);

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
