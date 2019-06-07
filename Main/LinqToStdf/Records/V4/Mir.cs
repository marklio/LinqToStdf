// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToStdf.Records.V4
{
    using Attributes;

    [TimeFieldLayout(FieldIndex = 0, RecordProperty = "SetupTime"),
    TimeFieldLayout(FieldIndex = 1, RecordProperty = "StartTime"),
    FieldLayout(FieldIndex = 2, FieldType = typeof(byte), RecordProperty = "StationNumber"),
    StringFieldLayout(FieldIndex = 3, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "ModeCode"),
    StringFieldLayout(FieldIndex = 4, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "RetestCode"),
    StringFieldLayout(FieldIndex = 5, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "ProtectionCode"),
    FieldLayout(FieldIndex = 6, IsOptional = true, FieldType = typeof(ushort), MissingValue = ushort.MaxValue, RecordProperty = "BurnInTime"),
    StringFieldLayout(FieldIndex = 7, IsOptional = true, Length = 1, MissingValue = " ", RecordProperty = "CommandModeCode"),
    StringFieldLayout(FieldIndex = 8, IsOptional = true, RecordProperty = "LotId"),
    StringFieldLayout(FieldIndex = 9, IsOptional = true, RecordProperty = "PartType"),
    StringFieldLayout(FieldIndex = 10, IsOptional = true, RecordProperty = "NodeName"),
    StringFieldLayout(FieldIndex = 11, IsOptional = true, RecordProperty = "TesterType"),
    StringFieldLayout(FieldIndex = 12, IsOptional = true, RecordProperty = "JobName"),
    StringFieldLayout(FieldIndex = 13, IsOptional = true, RecordProperty = "JobRevision"),
    StringFieldLayout(FieldIndex = 14, IsOptional = true, RecordProperty = "SublotId"),
    StringFieldLayout(FieldIndex = 15, IsOptional = true, RecordProperty = "OperatorName"),
    StringFieldLayout(FieldIndex = 16, IsOptional = true, RecordProperty = "ExecType"),
    StringFieldLayout(FieldIndex = 17, IsOptional = true, RecordProperty = "ExecVersion"),
    StringFieldLayout(FieldIndex = 18, IsOptional = true, RecordProperty = "TestCode"),
    StringFieldLayout(FieldIndex = 19, IsOptional = true, RecordProperty = "TestTemperature"),
    StringFieldLayout(FieldIndex = 20, IsOptional = true, RecordProperty = "UserText"),
    StringFieldLayout(FieldIndex = 21, IsOptional = true, RecordProperty = "AuxiliaryFile"),
    StringFieldLayout(FieldIndex = 22, IsOptional = true, RecordProperty = "PackageType"),
    StringFieldLayout(FieldIndex = 23, IsOptional = true, RecordProperty = "FamilyId"),
    StringFieldLayout(FieldIndex = 24, IsOptional = true, RecordProperty = "DateCode"),
    StringFieldLayout(FieldIndex = 25, IsOptional = true, RecordProperty = "FacilityId"),
    StringFieldLayout(FieldIndex = 26, IsOptional = true, RecordProperty = "FloorId"),
    StringFieldLayout(FieldIndex = 27, IsOptional = true, RecordProperty = "ProcessId"),
    StringFieldLayout(FieldIndex = 28, IsOptional = true, RecordProperty = "OperationFrequency"),
    StringFieldLayout(FieldIndex = 29, IsOptional = true, RecordProperty = "SpecificationName"),
    StringFieldLayout(FieldIndex = 30, IsOptional = true, RecordProperty = "SpecificationVersion"),
    StringFieldLayout(FieldIndex = 31, IsOptional = true, RecordProperty = "FlowId"),
    StringFieldLayout(FieldIndex = 32, IsOptional = true, RecordProperty = "SetupId"),
    StringFieldLayout(FieldIndex = 33, IsOptional = true, RecordProperty = "DesignRevision"),
    StringFieldLayout(FieldIndex = 34, IsOptional = true, RecordProperty = "EngineeringId"),
    StringFieldLayout(FieldIndex = 35, IsOptional = true, RecordProperty = "RomCode"),
    StringFieldLayout(FieldIndex = 36, IsOptional = true, RecordProperty = "SerialNumber"),
    StringFieldLayout(FieldIndex = 37, IsOptional = true, RecordProperty = "SupervisorName")]
    public class Mir : StdfRecord
    {

        public override RecordType RecordType
        {
            get { return new RecordType(1, 10); }
        }

        public DateTime? SetupTime { get; set; }
        public DateTime? StartTime { get; set; }
        public byte StationNumber { get; set; }
        /// <summary>
        /// Known values are: A, C, D, E, M, P, Q, 0-9
        /// </summary>
        public string ModeCode { get; set; }
        /// <summary>
        /// Known values are: Y, N, 0-9
        /// </summary>
        public string RetestCode { get; set; }
        /// <summary>
        /// Known values are A-Z, 0-9
        /// </summary>
        public string ProtectionCode { get; set; }
        public ushort? BurnInTime { get; set; }
        /// <summary>
        /// Known values are A-Z, 0-9
        /// </summary>
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
