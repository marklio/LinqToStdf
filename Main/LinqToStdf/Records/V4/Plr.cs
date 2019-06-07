// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace LinqToStdf.Records.V4
{
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(ushort)),
    ArrayFieldLayout(FieldIndex = 1, FieldType = typeof(ushort), ArrayLengthFieldIndex = 0, RecordProperty = "GroupIndexes"),
    ArrayFieldLayout(FieldIndex = 2, FieldType = typeof(ushort), IsOptional = true, MissingValue = ushort.MinValue, ArrayLengthFieldIndex = 0, AllowTruncation = true, RecordProperty = "GroupModes"),
    ArrayFieldLayout(FieldIndex = 3, FieldType = typeof(byte), IsOptional = true, MissingValue = byte.MinValue, ArrayLengthFieldIndex = 0, AllowTruncation = true, RecordProperty = "GroupRadixes"),
    ArrayFieldLayout(FieldIndex = 4, FieldType = typeof(string), IsOptional = true, MissingValue = "", ArrayLengthFieldIndex = 0, AllowTruncation = true, RecordProperty = "ProgramStatesRight"),
    ArrayFieldLayout(FieldIndex = 5, FieldType = typeof(string), IsOptional = true, MissingValue = "", ArrayLengthFieldIndex = 0, AllowTruncation = true, RecordProperty = "ReturnStatesRight"),
    ArrayFieldLayout(FieldIndex = 6, FieldType = typeof(string), IsOptional = true, MissingValue = "", ArrayLengthFieldIndex = 0, AllowTruncation = true, RecordProperty = "ProgramStatesLeft"),
    ArrayFieldLayout(FieldIndex = 7, FieldType = typeof(string), IsOptional = true, MissingValue = "", ArrayLengthFieldIndex = 0, AllowTruncation = true, RecordProperty = "ReturnStatesLeft")]
    public class Plr : StdfRecord
    {
        public Plr(StdfFile stdfFile) : base(stdfFile)
        {

        }

        public override RecordType RecordType
        {
            get { return new RecordType(1, 63); }
        }

        public ushort[] GroupIndexes { get; set; }
        /// <summary>
        /// Known values are: 0, 10, 20, 21, 22, 23, 30, 31, 32, 33
        /// </summary>
        public ushort[] GroupModes { get; set; }
        /// <summary>
        /// Known values are: 0, 2, 8, 10, 16, 20
        /// </summary>
        public byte[] GroupRadixes { get; set; }
        public string[] ProgramStatesRight { get; set; }
        public string[] ReturnStatesRight { get; set; }
        public string[] ProgramStatesLeft { get; set; }
        public string[] ReturnStatesLeft { get; set; }

        internal static Plr ConvertToPlr(UnknownRecord unknownRecord, StdfFile stdfFile)
        {
            Plr plr = new Plr(stdfFile);
            using (BinaryReader reader = new BinaryReader(new MemoryStream(unknownRecord.Content), unknownRecord.Endian, true))
            {
                // Group count and list of group indexes are required
                ushort groupCount = reader.ReadUInt16();
                plr.GroupIndexes = reader.ReadUInt16Array(groupCount, true);

                // Latter arrays are optional, and may be truncated
                if (!reader.AtEndOfStream)
                {
                    ushort[] groupModes = reader.ReadUInt16Array(groupCount, false);
                    // Expand a truncated array, filling with missing value
                    if ((groupModes != null) && (groupModes.Length < groupCount))
                    {
                        int i = groupModes.Length;
                        Array.Resize<ushort>(ref groupModes, groupCount);
                        for (; i < groupModes.Length; i++)
                            groupModes[i] = ushort.MinValue;
                    }
                    plr.GroupModes = groupModes;
                }
                if (!reader.AtEndOfStream)
                {
                    byte[] groupRadixes = reader.ReadByteArray(groupCount, false);
                    // Expand a truncated array, filling with missing value
                    if ((groupRadixes != null) && (groupRadixes.Length < groupCount))
                    {
                        int i = groupRadixes.Length;
                        Array.Resize<byte>(ref groupRadixes, groupCount);
                        for (; i < groupRadixes.Length; i++)
                            groupRadixes[i] = byte.MinValue;
                    }
                    plr.GroupRadixes = groupRadixes;
                }
                if (!reader.AtEndOfStream)
                {
                    string[] programStatesRight = reader.ReadStringArray(groupCount, false);
                    // Expand a truncated array, filling with missing value
                    if ((programStatesRight != null) && (programStatesRight.Length < groupCount))
                    {
                        int i = programStatesRight.Length;
                        Array.Resize<string>(ref programStatesRight, groupCount);
                        for (; i < programStatesRight.Length; i++)
                            programStatesRight[i] = "";
                    }
                    plr.ProgramStatesRight = programStatesRight;
                }
                if (!reader.AtEndOfStream)
                {
                    string[] returnStatesRight = reader.ReadStringArray(groupCount, false);
                    // Expand a truncated array, filling with missing value
                    if ((returnStatesRight != null) && (returnStatesRight.Length < groupCount))
                    {
                        int i = returnStatesRight.Length;
                        Array.Resize<string>(ref returnStatesRight, groupCount);
                        for (; i < returnStatesRight.Length; i++)
                            returnStatesRight[i] = "";
                    }
                    plr.ReturnStatesRight = returnStatesRight;
                }
                if (!reader.AtEndOfStream)
                {
                    string[] programStatesLeft = reader.ReadStringArray(groupCount, false);
                    // Expand a truncated array, filling with missing value
                    if ((programStatesLeft != null) && (programStatesLeft.Length < groupCount))
                    {
                        int i = programStatesLeft.Length;
                        Array.Resize<string>(ref programStatesLeft, groupCount);
                        for (; i < programStatesLeft.Length; i++)
                            programStatesLeft[i] = "";
                    }
                    plr.ProgramStatesLeft = programStatesLeft;
                }
                if (!reader.AtEndOfStream)
                {
                    string[] returnStatesLeft = reader.ReadStringArray(groupCount, false);
                    // Expand a truncated array, filling with missing value
                    if ((returnStatesLeft != null) && (returnStatesLeft.Length < groupCount))
                    {
                        int i = returnStatesLeft.Length;
                        Array.Resize<string>(ref returnStatesLeft, groupCount);
                        for (; i < returnStatesLeft.Length; i++)
                            returnStatesLeft[i] = "";
                    }
                    plr.ReturnStatesLeft = returnStatesLeft;
                }
            }
            return plr;
        }

        internal static UnknownRecord ConvertFromPlr(StdfRecord record, Endian endian)
        {
            Plr plr = (Plr)record;
            using (MemoryStream stream = new MemoryStream())
            {
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
                if (plr.ReturnStatesLeft != null)
                {
                    // Check for larger group length (writing has definitely not occurred yet)
                    if (plr.ProgramStatesLeft.Length > groupCount)
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 7));
                    // Write the field
                    writer.WriteStringArray(plr.ReturnStatesLeft);
                    fieldsWritten = true;
                }
                else if (fieldsWritten)
                {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate (string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 6: ProgramStatesLeft
                if (plr.ProgramStatesLeft != null)
                {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.ProgramStatesLeft.Length > groupCount) || (fieldsWritten && (plr.ProgramStatesLeft.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 6));
                    // Write the field
                    writer.WriteStringArray(plr.ProgramStatesLeft);
                    fieldsWritten = true;
                }
                else if (fieldsWritten)
                {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate (string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 5: ReturnStatesRight
                if (plr.ReturnStatesRight != null)
                {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.ReturnStatesRight.Length > groupCount) || (fieldsWritten && (plr.ReturnStatesRight.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 5));
                    // Write the field
                    writer.WriteStringArray(plr.ReturnStatesRight);
                    fieldsWritten = true;
                }
                else if (fieldsWritten)
                {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate (string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 4: ProgramStatesRight
                if (plr.ProgramStatesRight != null)
                {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.ProgramStatesRight.Length > groupCount) || (fieldsWritten && (plr.ProgramStatesRight.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 4));
                    // Write the field
                    writer.WriteStringArray(plr.ProgramStatesRight);
                    fieldsWritten = true;
                }
                else if (fieldsWritten)
                {
                    // Fill an array of missing values and write
                    string[] arr = new string[groupCount];
                    Array.ForEach<string>(arr, delegate (string a) { a = ""; });
                    writer.WriteStringArray(arr);
                }

                // Field 3: GroupRadixes
                if (plr.GroupRadixes != null)
                {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.GroupRadixes.Length > groupCount) || (fieldsWritten && (plr.GroupRadixes.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 3));
                    // Write the field
                    writer.WriteByteArray(plr.GroupRadixes);
                    fieldsWritten = true;
                }
                else if (fieldsWritten)
                {
                    // Fill an array of missing values and write
                    byte[] arr = new byte[groupCount];
                    Array.ForEach<byte>(arr, delegate (byte a) { a = Byte.MinValue; });
                    writer.WriteByteArray(arr);
                }

                // Field 2: GroupModes
                if (plr.GroupModes != null)
                {
                    // Check for larger, or not equal group length ifwriting has occurred
                    if ((plr.GroupModes.Length > groupCount) || (fieldsWritten && (plr.GroupModes.Length != groupCount)))
                        throw new InvalidOperationException(String.Format(Resources.SharedLengthViolation, 2));
                    // Write the field
                    writer.WriteUInt16Array(plr.GroupModes);
                    fieldsWritten = true;
                }
                else if (fieldsWritten)
                {
                    // Fill an array of missing values and write
                    ushort[] arr = new ushort[groupCount];
                    Array.ForEach<ushort>(arr, delegate (ushort a) { a = UInt16.MinValue; });
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
