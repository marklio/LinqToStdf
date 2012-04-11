// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4 {
    using Attributes;

    [StdfFieldLayout(FieldIndex = 0, FieldType = typeof(DateTime), AssignTo = "SetupTime"),
    StdfFieldLayout(FieldIndex = 1, FieldType = typeof(DateTime), AssignTo = "StartTime"),
    StdfFieldLayout(FieldIndex = 2, FieldType = typeof(byte), AssignTo = "StationNumber"),
    StdfStringLayout(FieldIndex = 3, Length = 1, MissingValue = " ", AssignTo = "ModeCode"),
    StdfStringLayout(FieldIndex = 4, Length = 1, MissingValue = " ", AssignTo = "RetestCode"),
    StdfStringLayout(FieldIndex = 5, Length = 1, MissingValue = " ", AssignTo = "ProtectionCode"),
    StdfFieldLayout(FieldIndex = 6, FieldType = typeof(ushort), MissingValue = ushort.MaxValue, AssignTo = "BurnInTime"),
    StdfStringLayout(FieldIndex = 7, Length = 1, MissingValue = " ", AssignTo = "CommandModeCode"),
    StdfStringLayout(FieldIndex = 8, AssignTo = "LotId"),
    StdfStringLayout(FieldIndex = 9, AssignTo = "PartType"),
    StdfStringLayout(FieldIndex = 10, AssignTo = "NodeName"),
    StdfStringLayout(FieldIndex = 11, AssignTo = "TesterType"),
    StdfStringLayout(FieldIndex = 12, AssignTo = "JobName"),
    StdfStringLayout(FieldIndex = 13, AssignTo = "JobRevision"),
    StdfStringLayout(FieldIndex = 14, AssignTo = "SublotId"),
    StdfStringLayout(FieldIndex = 15, AssignTo = "OperatorName"),
    StdfStringLayout(FieldIndex = 16, AssignTo = "ExecType"),
    StdfStringLayout(FieldIndex = 17, AssignTo = "ExecVersion"),
    StdfStringLayout(FieldIndex = 18, AssignTo = "TestCode"),
    StdfStringLayout(FieldIndex = 19, AssignTo = "TestTemperature"),
    StdfStringLayout(FieldIndex = 20, AssignTo = "UserText"),
    StdfStringLayout(FieldIndex = 21, AssignTo = "AuxiliaryFile"),
    StdfStringLayout(FieldIndex = 22, AssignTo = "PackageType"),
    StdfStringLayout(FieldIndex = 23, AssignTo = "FamilyId"),
    StdfStringLayout(FieldIndex = 24, AssignTo = "DateCode"),
    StdfStringLayout(FieldIndex = 25, AssignTo = "FacilityId"),
    StdfStringLayout(FieldIndex = 26, AssignTo = "FloorId"),
    StdfStringLayout(FieldIndex = 27, AssignTo = "ProcessId"),
    StdfStringLayout(FieldIndex = 28, AssignTo = "OperationFrequency"),
    StdfStringLayout(FieldIndex = 29, AssignTo = "SpecificationName"),
    StdfStringLayout(FieldIndex = 30, AssignTo = "SpecificationVersion"),
    StdfStringLayout(FieldIndex = 31, AssignTo = "FlowId"),
    StdfStringLayout(FieldIndex = 32, AssignTo = "SetupId"),
    StdfStringLayout(FieldIndex = 33, AssignTo = "DesignRevision"),
    StdfStringLayout(FieldIndex = 34, AssignTo = "EngineeringId"),
    StdfStringLayout(FieldIndex = 35, AssignTo = "RomCode"),
    StdfStringLayout(FieldIndex = 36, AssignTo = "SerialNumber"),
    StdfStringLayout(FieldIndex = 37, AssignTo = "SupervisorName")]
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
