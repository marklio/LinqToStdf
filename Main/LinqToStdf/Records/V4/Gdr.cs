// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;

namespace LinqToStdf.Records.V4 {

	public class Gdr : StdfRecord {

		public override RecordType RecordType {
			get { return new RecordType(50, 10); }
		}

		private object[] _GenericData;
		public object[] GenericData {
			get { return _GenericData; }
			set { _GenericData = value; }
		}

		internal static Gdr ConvertToGdr(UnknownRecord unknownRecord) {
			Gdr gdr = new Gdr();
			using (BinaryReader reader = new BinaryReader(new MemoryStream(unknownRecord.Content), unknownRecord.Endian, true)) {
				ushort fieldCount = reader.ReadUInt16();
				object[] data = new object[fieldCount];
				for (int i = 0; i < fieldCount; i++) {
					byte dataTypeCode = reader.ReadByte();
					switch (dataTypeCode) {
						case 0:
							break;
						case 1:
							data[i] = reader.ReadByte();
							break;
						case 2:
							data[i] = reader.ReadUInt16();
							break;
						case 3:
							data[i] = reader.ReadUInt32();
							break;
						case 4:
							data[i] = reader.ReadSByte();
							break;
						case 5:
							data[i] = reader.ReadInt16();
							break;
						case 6:
							data[i] = reader.ReadInt32();
							break;
						case 7:
							data[i] = reader.ReadSingle();
							break;
						case 8:
							data[i] = reader.ReadDouble();
							break;
						case 10:
							data[i] = reader.ReadString();
							break;
						case 11: {
							byte length = reader.ReadByte();
							byte[] bytes = new byte[length];
							for (int byteIndex = 0; byteIndex < length; byteIndex++) {
								bytes[byteIndex] = reader.ReadByte();
							}
							data[i] = bytes;
							break;
						}
						case 12: {
							ushort length = reader.ReadUInt16();
							length = (ushort)((length / 8) + (((length % 8) > 0)?1:0));
							byte[] bytes = new byte[length];
							for (int byteIndex = 0; byteIndex < length; byteIndex++) {
								bytes[byteIndex] = reader.ReadByte();
							}
							data[i] = bytes;
							break;
						}
						case 13: {
							byte nibble = reader.ReadByte();
							nibble = (byte)(nibble & 0x0F);
							break;
						}
						default:
							throw new InvalidOperationException(string.Format(Resources.InvalidGdrDataTypeCode, dataTypeCode));
					}
				}
                gdr.GenericData = data;
            }
			return gdr;
		}

        internal static UnknownRecord ConvertFromGdr(StdfRecord record, Endian endian) {
            Gdr gdr = (Gdr)record;
            using (MemoryStream stream = new MemoryStream()) {
                BinaryWriter writer = new BinaryWriter(stream, endian, false);
                writer.WriteUInt16((ushort)gdr._GenericData.Length);
                //TODO: support padding bytes? Most modern parser doesn't care.
				//TODO: faster if statements via RuntimeTypeHandle
                for (int i = 0; i < gdr._GenericData.Length; i++) {
                    object o = gdr._GenericData[i];
                    if (o is byte) {
                        writer.WriteByte((byte)1);
                        writer.WriteByte((byte)o);
                    }
                    else if (o is ushort) {
                        writer.WriteByte((byte)2);
                        writer.WriteUInt16((ushort)o);
                    }
                    else if (o is uint) {
                        writer.WriteByte((byte)3);
                        writer.WriteUInt32((uint)o);
                    }
                    else if (o is sbyte) {
                        writer.WriteByte((byte)4);
                        writer.WriteSByte((sbyte)o);
                    }
                    else if (o is short) {
                        writer.WriteByte((byte)5);
                        writer.WriteInt16((short)o);
                    }
                    else if (o is int) {
                        writer.WriteByte((byte)6);
                        writer.WriteInt32((int)o);
                    }
                    else if (o is float) {
                        writer.WriteByte((byte)7);
                        writer.WriteSingle((float)o);
                    }
                    else if (o is double) {
                        writer.WriteByte((byte)8);
                        writer.WriteDouble((double)o);
                    }
                    else if (o is string) {
                        writer.WriteByte((byte)10);
                        writer.WriteString((string)o);
                    }
                    else if (o is byte[]) {
                        writer.WriteByte((byte)11);
                        writer.WriteByteArray((byte[])o);
                    }
                    else if (o is BitArray) {
                        writer.WriteByte((byte)12);
                        writer.WriteBitArray((BitArray)o);
                    }
                    else {
                        throw new InvalidOperationException(string.Format(Resources.UnsupportedGdrDataType, o.GetType()));
                    }
                    //TODO: how to deal with nibble?
                }
                return new UnknownRecord(gdr.RecordType, stream.ToArray(), endian);
            }
        }
    }
}
