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
                    ushort[] groupIndexes = new ushort[groupCount];
                    for (int i = 0; i < groupCount; i++) {
                        groupIndexes[i] = reader.ReadUInt16();
                    }
                    plr.GroupIndexes = groupIndexes;
                    if (!reader.AtEndOfStream) {
                        ushort[] groupModes = new ushort[groupCount];
                        for (int i = 0; i < groupCount; i++) {
                            groupModes[i] = reader.ReadUInt16();
                        }
                        plr.GroupModes = groupModes;
                        if (!reader.AtEndOfStream) {
                            byte[] groupRadixes = new byte[groupCount];
                            for (int i = 0; i < groupCount; i++) {
                                groupRadixes[i] = reader.ReadByte();
                            }
                            plr.GroupRadixes = groupRadixes;
                            if (!reader.AtEndOfStream) {
                                string[] programStatesRight = new string[groupCount];
                                for (int i = 0; i < groupCount; i++) {
                                    programStatesRight[i] = reader.ReadString(1);
                                }
                                plr.ProgramStatesRight = programStatesRight;
                                if (!reader.AtEndOfStream) {
                                    string[] returnStatesRight = new string[groupCount];
                                    for (int i = 0; i < groupCount; i++) {
                                        returnStatesRight[i] = reader.ReadString(1);
                                    }
                                    plr.ReturnStatesRight = returnStatesRight;
                                    if (!reader.AtEndOfStream) {
                                        string[] programStatesLeft = new string[groupCount];
                                        for (int i = 0; i < groupCount; i++) {
                                            programStatesLeft[i] = reader.ReadString(1);
                                        }
                                        plr.ProgramStatesLeft = programStatesLeft;
                                        if (!reader.AtEndOfStream) {
                                            string[] returnStatesLeft = new string[groupCount];
                                            for (int i = 0; i < groupCount; i++) {
                                                returnStatesLeft[i] = reader.ReadString(1);
                                            }
                                            plr.ReturnStatesLeft = returnStatesLeft;
                                        }
                                    }
                                }
                            }
                        }
                    }
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
