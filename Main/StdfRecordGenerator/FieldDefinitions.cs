using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.CompilerServices
{
    using global::System.ComponentModel;
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }

        public NotNullWhenAttribute(bool returnValue)
        {
            ReturnValue = returnValue;
        }
    }
}

namespace StdfRecordGenerator
{
    record FieldLayoutDefinition(int? FieldIndex = null, string? FieldType = null, string? RecordProperty = null, bool IsOptional = false, object? MissingValue = null, bool PersistMissingValue = true);
    record ArrayFieldLayoutDefinition(int? FieldIndex = null, string? FieldType = null, int? ArrayLengthFieldIndex=null, string? RecordProperty = null, bool IsOptional = false, object? MissingValue = null, bool PersistMissingValue = true, bool AllowTruncation = false)
        : FieldLayoutDefinition(FieldIndex, FieldType, RecordProperty, IsOptional, MissingValue, PersistMissingValue);
    record DependencyPropertyDefinition(int? FieldIndex = null, string? FieldType = null, int? DependentOnIndex=null, string? RecordProperty = null, bool IsOptional = false, object? MissingValue = null, bool PersistMissingValue = true)
        : FieldLayoutDefinition(FieldIndex, FieldType, RecordProperty, IsOptional, MissingValue, PersistMissingValue);
    record FlaggedFieldLayoutDefinition(int? FieldIndex = null, string? FieldType = null, int? FlagIndex=null, byte? FlagMask=null, string? RecordProperty = null, bool IsOptional = false, object? MissingValue = null, bool PersistMissingValue = true)
        : FieldLayoutDefinition(FieldIndex, FieldType, RecordProperty, IsOptional, MissingValue, PersistMissingValue);
    record NibbleArrayFieldLayoutDefinition(int? FieldIndex = null, int? ArrayLengthFieldIndex=null, string? RecordProperty = null, bool IsOptional = false, object? MissingValue = null, bool PersistMissingValue = true, bool AllowTruncation = false)
        : FieldLayoutDefinition(FieldIndex, "System.Byte", RecordProperty, IsOptional, MissingValue, PersistMissingValue);
    record StringFieldLayoutDefinition(int? FieldIndex = null, string? RecordProperty = null, bool IsOptional = false, object? MissingValue = null, bool PersistMissingValue = true, int? Length = null, char PadCharacter = ' ')
        : FieldLayoutDefinition(FieldIndex, "System.String", RecordProperty, IsOptional, MissingValue, PersistMissingValue);
    record TimeFieldLayoutDefinition(int? FieldIndex = null, string? RecordProperty = null, bool IsOptional = false, bool PersistMissingValue = true)
        : FieldLayoutDefinition(FieldIndex, "System.DateTime", RecordProperty, IsOptional, Epoch, PersistMissingValue)
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1);
    }
}
