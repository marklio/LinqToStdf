// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [FieldLayout(FieldIndex = 0, FieldType = typeof(DateTime), RecordProperty = "SetupTime"),
    FieldLayout(FieldIndex = 1, FieldType = typeof(DateTime), RecordProperty = "StartTime"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), RecordProperty = "StationNumber"),
    StringFieldLayout(FieldIndex = 3, Length = 1, RecordProperty = "ModeCode"),
    StringFieldLayout(FieldIndex = 4, Length = 1, RecordProperty = "RetestCode"),
    StringFieldLayout(FieldIndex = 5, Length = 1, RecordProperty = "ProtectionCode"),
    FieldLayout(FieldIndex = 6, FieldType = typeof(ushort), MissingValue = ushort.MaxValue, RecordProperty = "BurnInTime"),
    StringFieldLayout(FieldIndex = 7, Length = 1, MissingValue = " ", RecordProperty = "CommandModeCode"),
    StringFieldLayout(FieldIndex = 8, RecordProperty = "LotId"),
    StringFieldLayout(FieldIndex = 9, RecordProperty = "PartType"),
    StringFieldLayout(FieldIndex = 10, RecordProperty = "NodeName"),
    StringFieldLayout(FieldIndex = 11, RecordProperty = "TesterType"),
    StringFieldLayout(FieldIndex = 12, RecordProperty = "JobName"),
    StringFieldLayout(FieldIndex = 13, RecordProperty = "JobRevision"),
    StringFieldLayout(FieldIndex = 14, RecordProperty = "SublotId"),
    StringFieldLayout(FieldIndex = 15, RecordProperty = "OperatorName"),
    StringFieldLayout(FieldIndex = 16, RecordProperty = "ExecType"),
    StringFieldLayout(FieldIndex = 17, RecordProperty = "ExecVersion"),
    StringFieldLayout(FieldIndex = 18, RecordProperty = "TestCode"),
    StringFieldLayout(FieldIndex = 19, RecordProperty = "TestTemperature"),
    StringFieldLayout(FieldIndex = 20, RecordProperty = "UserText"),
    StringFieldLayout(FieldIndex = 21, RecordProperty = "AuxiliaryFile"),
    StringFieldLayout(FieldIndex = 22, RecordProperty = "PackageType"),
    StringFieldLayout(FieldIndex = 23, RecordProperty = "FamilyId"),
    StringFieldLayout(FieldIndex = 24, RecordProperty = "DateCode"),
    StringFieldLayout(FieldIndex = 25, RecordProperty = "FacilityId"),
    StringFieldLayout(FieldIndex = 26, RecordProperty = "FloorId"),
    StringFieldLayout(FieldIndex = 27, RecordProperty = "ProcessId"),
    StringFieldLayout(FieldIndex = 28, RecordProperty = "OperationFrequency"),
    StringFieldLayout(FieldIndex = 29, RecordProperty = "SpecificationName"),
    StringFieldLayout(FieldIndex = 30, RecordProperty = "SpecificationVersion"),
    StringFieldLayout(FieldIndex = 31, RecordProperty = "FlowId"),
    StringFieldLayout(FieldIndex = 32, RecordProperty = "SetupId"),
    StringFieldLayout(FieldIndex = 33, RecordProperty = "DesignRevision"),
    StringFieldLayout(FieldIndex = 34, RecordProperty = "EngineeringId"),
    StringFieldLayout(FieldIndex = 35, RecordProperty = "RomCode"),
    StringFieldLayout(FieldIndex = 36, RecordProperty = "SerialNumber"),
    StringFieldLayout(FieldIndex = 37, RecordProperty = "SupervisorName")]
    public class Mir : StdfRecord {

        public override RecordType RecordType {
            get { return new RecordType(1, 10); }
        }

        public DateTime? SetupTime { get; set; }
        public DateTime? StartTime { get; set; }
        public byte? StationNumber { get; set; }
        public string ModeCode { get; set; }
        public string RetestCode { get; set; }
        public string ProtectionCode { get; set; }
        public ushort? BurnInTime { get; set; }
        public string CommandModeCode { get; set; }
        public string LotId { get; set; }
        public string PartType { get; set; }
        public string NodeName { get; set; }
        public string TesterType { get; set; }
        public string JobName { get; set; }
        public string JobRevision { get; set; }
        public string SublotId { get; set; }
        public string OperatorName { get; set; }
        public string ExecType { get; set; }
        public string ExecVersion { get; set; }
        public string TestCode { get; set; }
        public string TestTemperature { get; set; }
        public string UserText { get; set; }
        public string AuxiliaryFile { get; set; }
        public string PackageType { get; set; }
        public string FamilyId { get; set; }
        public string DateCode { get; set; }
        public string FacilityId { get; set; }
        public string FloorId { get; set; }
        public string ProcessId { get; set; }
        public string OperationFrequency { get; set; }
        public string SpecificationName { get; set; }
        public string SpecificationVersion { get; set; }
        public string FlowId { get; set; }
        public string SetupId { get; set; }
        public string DesignRevision { get; set; }
        public string EngineeringId { get; set; }
        public string RomCode { get; set; }
        public string SerialNumber { get; set; }
        public string SupervisorName { get; set; }
    }
}
