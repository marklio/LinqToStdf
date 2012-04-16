// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
	using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(byte), AssignTo = "HeadNumber"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(byte), AssignTo = "SiteNumber"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(byte), AssignTo = "PartFlag"),
    StdfFieldLayout(FieldIndex = 3, FieldType = typeof(ushort), AssignTo = "TestCount"),
    StdfFieldLayout(FieldIndex = 4, FieldType = typeof(ushort), AssignTo = "HardBin"),
    StdfFieldLayout(FieldIndex = 5, FieldType = typeof(ushort), MissingValue = ushort.MaxValue, AssignTo = "SoftBin"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(short), MissingValue = short.MinValue, AssignTo = "XCoordinate"),
    StdfFieldLayout(FieldIndex = 7, FieldType = typeof(short), MissingValue = short.MinValue, AssignTo = "YCoordinate"),
    StdfFieldLayout(FieldIndex = 8, FieldType = typeof(uint), MissingValue = uint.MinValue, AssignTo = "TestTime"),
    StdfStringLayout(FieldIndex = 9, AssignTo = "PartId", MissingValue = ""),
    StdfStringLayout(FieldIndex = 10, AssignTo = "PartText", MissingValue = ""),
    StdfFieldLayout(FieldIndex = 11, FieldType = typeof(byte)),
    StdfArrayLayout(FieldIndex = 12, FieldType = typeof(byte), ArrayLengthFieldIndex = 11, AssignTo = "PartFix"),
    StdfDependencyProperty(FieldIndex = 13, DependentOnIndex = 2, AssignTo = "SupersedesPartId"),
    StdfDependencyProperty(FieldIndex = 14, DependentOnIndex = 2, AssignTo = "SupersedesCoords"),
    StdfDependencyProperty(FieldIndex = 15, DependentOnIndex = 2, AssignTo = "AbnormalTest"),
    StdfDependencyProperty(FieldIndex = 16, DependentOnIndex = 2, AssignTo = "Failed")]
    public class Prr : StdfRecord, IHeadSiteIndexable {

        public override RecordType RecordType {
            get { return new RecordType(5, 20); }
        }

        public byte HeadNumber { get; set; }
        public byte SiteNumber { get; set; }
        public byte PartFlag { get; set; }
        public ushort? TestCount { get; set; }
        public ushort? HardBin { get; set; }
        public ushort? SoftBin { get; set; }
        public short? XCoordinate { get; set; }
        public short? YCoordinate { get; set; }
        public uint? TestTime { get; set; }
        public string PartId { get; set; }
        public string PartText { get; set; }
        public byte[] PartFix { get; set; }
        //dependency properties
        static readonly byte _SupersedesPartIdMask = 0x01;
        static readonly byte _SupersedesCoordsMask = 0x02;
        static readonly byte _AbnormalTestMask = 0x04;
        static readonly byte _FailedMask = 0x08;
        static readonly byte _FailFlagInvalidMask = 0x10;

        public bool SupersedesPartId {
            get { return (PartFlag & _SupersedesPartIdMask) != 0; }
            set {
                if (value) PartFlag |= _SupersedesPartIdMask;
                else PartFlag &= (byte)~_SupersedesPartIdMask;
            }
        }

        public bool SupersedesCoords {
            get { return (PartFlag & _SupersedesCoordsMask) != 0; }
            set {
                if (value) PartFlag |= _SupersedesCoordsMask;
                else PartFlag &= (byte)~_SupersedesCoordsMask;
            }
        }

        public bool AbnormalTest {
            get { return (PartFlag & _AbnormalTestMask) != 0; }
            set {
                if (value) PartFlag |= _AbnormalTestMask;
                else PartFlag &= (byte)~_AbnormalTestMask;
            }
        }

        public bool? Failed {
            get { return (PartFlag & _FailFlagInvalidMask) != 0 ? (bool?)null : (PartFlag & _FailedMask) != 0; }
            set {
                if (value == null) {
                    PartFlag &= (byte)~_FailedMask;
                    PartFlag |= _FailFlagInvalidMask;
                }
                else {
                    PartFlag &= (byte)~_FailFlagInvalidMask;
                    if ((bool)value) PartFlag |= _FailedMask;
                    else PartFlag &= (byte)~_FailedMask;
                }
            }
        }
    }
}
