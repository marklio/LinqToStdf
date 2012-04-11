// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LinqToStdf.Records.V4 {
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

                // Temporary throw 
                throw new NotImplementedException(string.Format(Resources.NoRegisteredUnconverter, plr.GetType()));

                // The last array field in the record is allowed to be truncated instead of padding the end with missing items

                // Array elements are written in reverse, because writer is in backwards mode

                // The not-last arrays can have larger lengths, but those lengths must match

                // The maximum array's length is written

                return new UnknownRecord(plr.RecordType, stream.ToArray(), endian);
            }
        }
    }
}
