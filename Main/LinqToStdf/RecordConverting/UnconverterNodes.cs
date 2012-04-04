// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Reflection;

namespace LinqToStdf.RecordConverting
{
    class UnconverterShellNode : CodeNode
    {
        public BlockNode Block { get; private set; }
        public UnconverterShellNode(BlockNode block)
        {
            Block = block;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitUnconverterShell(this);
        }
    }
    class CreateFieldLocalForWritingNode : CodeNode
    {
        public int FieldIndex { get; private set; }
        public Type LocalType { get; private set; }
        public CreateFieldLocalForWritingNode(int fieldIndex, Type localType)
        {
            FieldIndex = fieldIndex;
            LocalType = localType;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitCreateFieldLocalForWriting(this);
        }
    }
    class WriteFieldNode : CodeNode
    {
        public int FieldIndex { get; private set; }
        public Type FieldType { get; private set; }
        public CodeNode Initialization { get; private set; }
        public PropertyInfo Property { get; private set; }
        public CodeNode WriteOperation { get; private set; }
        public CodeNode NoValueWriteContingency { get; private set; }
        public int? OptionalFieldIndex { get; private set; }
        public byte OptionaFieldMask { get; private set; }

        public WriteFieldNode(
            int fieldIndex,
            Type fieldType,
            CodeNode initialization = null,
            PropertyInfo sourceProperty = null,
            CodeNode writeOperation = null,
            CodeNode noValueWriteContingency = null,
            int? optionalFieldIndex = null,
            byte optionalFieldMask = 0)
        {
            FieldIndex = fieldIndex;
            FieldType = fieldType;
            Initialization = initialization;
            Property = sourceProperty;
            WriteOperation = writeOperation;
            NoValueWriteContingency = noValueWriteContingency;
            OptionalFieldIndex = optionalFieldIndex;
            OptionaFieldMask = optionalFieldMask;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitWriteField(this);
        }
    }
    class WriteFixedStringNode : CodeNode
    {
        public int StringLength { get; private set; }
        public CodeNode ValueSource { get; set; }
        public WriteFixedStringNode(int stringLength, CodeNode valueSource)
        {
            StringLength = stringLength;
            ValueSource = valueSource;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitWriteFixedString(this);
        }
    }
    class WriteTypeNode : CodeNode
    {
        public Type Type { get; private set; }
        public CodeNode ValueSource { get; set; }
        public WriteTypeNode(Type type, CodeNode valueSource)
        {
            Type = type;
            ValueSource = valueSource;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitWriteType(this);
        }
    }
    class LoadMissingValueNode : CodeNode
    {
        public object MissingValue { get; private set; }
        public Type Type { get; private set; }
        public LoadMissingValueNode(object missingValue, Type type)
        {
            MissingValue = missingValue;
            Type = type;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitLoadMissingValue(this);
        }

    }
    class LoadFieldLocalNode : CodeNode
    {
        public int FieldIndex { get; private set; }
        public LoadFieldLocalNode(int fieldIndex)
        {
            FieldIndex = fieldIndex;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitLoadFieldLocal(this);
        }

    }
    class LoadNullNode : CodeNode
    {
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitLoadNull(this);
        }

    }
    class ValidateSharedLengthLocalNode : CodeNode
    {
        public int ArrayFieldIndex { get; private set; }
        public int LengthFieldIndex { get; private set; }
        public ValidateSharedLengthLocalNode(int arrayFieldIndex, int lengthFieldIndex)
        {
            ArrayFieldIndex = arrayFieldIndex;
            LengthFieldIndex = lengthFieldIndex;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitValidateSharedLengthLocal(this);
        }
    }
    class SetLengthLocalNode : CodeNode
    {

        public int ArrayFieldIndex { get; private set; }
        public int LengthFieldIndex { get; private set; }
        public SetLengthLocalNode(int arrayFieldIndex, int lengthFieldIndex)
        {
            ArrayFieldIndex = arrayFieldIndex;
            LengthFieldIndex = lengthFieldIndex;
        }
        public override CodeNode Accept(CodeNodeVisitor visitor)
        {
            return visitor.VisitSetLengthLocal(this);
        }
    }
}
