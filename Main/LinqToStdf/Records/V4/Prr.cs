// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace LinqToStdf.Records.V4
{
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "HeadNumber"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(byte), MissingValue = (byte)1, PersistMissingValue = true, RecordProperty = "SiteNumber"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), RecordProperty = "PartFlag"),
    FieldLayout(FieldIndex = 3, FieldType = typeof(ushort), RecordProperty = "TestCount"),
    FieldLayout(FieldIndex = 4, FieldType = typeof(ushort), RecordProperty = "HardBin"),
    FieldLayout(FieldIndex = 5, FieldType = typeof(ushort), IsOptional = true, MissingValue = ushort.MaxValue, RecordProperty = "SoftBin"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(short), IsOptional = true, MissingValue = short.MinValue, RecordProperty = "XCoordinate"),
    FieldLayout(FieldIndex = 7, FieldType = typeof(short), IsOptional = true, MissingValue = short.MinValue, RecordProperty = "YCoordinate"),
    FieldLayout(FieldIndex = 8, FieldType = typeof(uint), IsOptional = true, MissingValue = uint.MinValue, RecordProperty = "TestTime"),
    StringFieldLayout(FieldIndex = 9, IsOptional = true, RecordProperty = "PartId"),
    StringFieldLayout(FieldIndex = 10, IsOptional = true, RecordProperty = "PartText"),
    FieldLayout(FieldIndex = 11, FieldType = typeof(byte)),
    ArrayFieldLayout(FieldIndex = 12, FieldType = typeof(byte), ArrayLengthFieldIndex = 11, RecordProperty = "PartFix"),
    DependencyProperty(FieldIndex = 13, DependentOnIndex = 2, RecordProperty = "SupersedesPartId"),
    DependencyProperty(FieldIndex = 14, DependentOnIndex = 2, RecordProperty = "SupersedesCoords"),
    DependencyProperty(FieldIndex = 15, DependentOnIndex = 2, RecordProperty = "AbnormalTest"),
    DependencyProperty(FieldIndex = 16, DependentOnIndex = 2, RecordProperty = "Failed")]
    public class Prr : StdfRecord, IHeadSiteIndexable
    {
        public Prr(StdfFile stdfFile) : base(stdfFile)
        {

        }

        public override RecordType RecordType
        {
            get { return new RecordType(5, 20); }
        }

        public byte? HeadNumber { get; set; }
        public byte? SiteNumber { get; set; }
        public byte PartFlag { get; set; }
        public ushort TestCount { get; set; }
        /// <summary>
        /// While ushort, valid bins must be 0 - 32,767
        /// </summary>
        public ushort HardBin { get; set; }
        /// <summary>
        /// While ushort, valid bins must be 0 - 32,767
        /// </summary>
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

        public bool SupersedesPartId
        {
            get { return (PartFlag & _SupersedesPartIdMask) != 0; }
            set
            {
                if (value) PartFlag |= _SupersedesPartIdMask;
                else PartFlag &= (byte)~_SupersedesPartIdMask;
            }
        }

        public bool SupersedesCoords
        {
            get { return (PartFlag & _SupersedesCoordsMask) != 0; }
            set
            {
                if (value) PartFlag |= _SupersedesCoordsMask;
                else PartFlag &= (byte)~_SupersedesCoordsMask;
            }
        }

        public bool AbnormalTest
        {
            get { return (PartFlag & _AbnormalTestMask) != 0; }
            set
            {
                if (value) PartFlag |= _AbnormalTestMask;
                else PartFlag &= (byte)~_AbnormalTestMask;
            }
        }

        public bool? Failed
        {
            get { return (PartFlag & _FailFlagInvalidMask) != 0 ? (bool?)null : (PartFlag & _FailedMask) != 0; }
            set
            {
                if (value == null)
                {
                    PartFlag &= (byte)~_FailedMask;
                    PartFlag |= _FailFlagInvalidMask;
                }
                else
                {
                    PartFlag &= (byte)~_FailFlagInvalidMask;
                    if ((bool)value) PartFlag |= _FailedMask;
                    else PartFlag &= (byte)~_FailedMask;
                }
            }
        }
    }
}
