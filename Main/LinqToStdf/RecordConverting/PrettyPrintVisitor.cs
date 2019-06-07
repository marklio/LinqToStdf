// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System.IO;

#nullable enable

namespace LinqToStdf.RecordConverting
{
    class PrettyPrintVisitor : CodeNodeVisitor
    {
        readonly StringWriter _Output;
        readonly System.Xml.XmlTextWriter _Writer;
        public PrettyPrintVisitor(CodeNode node)
        {
            _Output = new StringWriter();
            _Writer = new System.Xml.XmlTextWriter(_Output)
            {
                Formatting = System.Xml.Formatting.Indented,
            };
            Visit(node);
        }
        public override CodeNode VisitAssignFieldToProperty(AssignFieldToPropertyNode node)
        {
            _Writer.WriteStartElement("AssignFieldToProperty");
            _Writer.WriteAttributeString("Type", node.FieldType.ToString());
            _Writer.WriteAttributeString("Property", node.Property.Name);
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitBlock(BlockNode node)
        {
            _Writer.WriteStartElement("Block");
            try
            {
                return base.VisitBlock(node);
            }
            finally
            {
                _Writer.WriteEndElement();
            }
        }
        public override CodeNode VisitCreateFieldLocalForWriting(CreateFieldLocalForWritingNode node)
        {
            _Writer.WriteStartElement("CreateFieldLocalForWriting");
            _Writer.WriteAttributeString("FieldIndex", node.FieldIndex.ToString());
            _Writer.WriteAttributeString("LocalType", node.LocalType.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitDisposeReader(DisposeReaderNode node)
        {
            _Writer.WriteStartElement("DisposeReader");
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitEnsureCompat(EnsureCompatNode node)
        {
            _Writer.WriteStartElement("EnsureCompat");
            try
            {
                return base.VisitEnsureCompat(node);
            }
            finally
            {
                _Writer.WriteEndElement();
            }
        }
        public override CodeNode VisitFieldAssignment(FieldAssignmentNode node)
        {
            _Writer.WriteStartElement("FieldAssignment");
            _Writer.WriteAttributeString("Type", node.Type.ToString());
            _Writer.WriteAttributeString("FieldIndex", node.FieldIndex.ToString());
            _Writer.WriteStartElement("ReadNode");
            Visit(node.ReadNode);
            _Writer.WriteEndElement();
            _Writer.WriteStartElement("AssignmentBlock");
            Visit(node.AssignmentBlock);
            _Writer.WriteEndElement();
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitFieldAssignmentBlock(FieldAssignmentBlockNode node)
        {
            _Writer.WriteStartElement("FieldAssignmentBlock");
            Visit(node.Block);
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitInitializeRecord(InitializeRecordNode node)
        {
            _Writer.WriteStartElement("InitializeRecord");
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitInitReaderNode(InitReaderNode node)
        {
            _Writer.WriteStartElement("InitReader");
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitLoadFieldLocal(LoadFieldLocalNode node)
        {
            _Writer.WriteStartElement("LoadFieldLocal");
            _Writer.WriteAttributeString("FieldIndex", node.FieldIndex.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitLoadMissingValue(LoadMissingValueNode node)
        {
            _Writer.WriteStartElement("LoadMissingValue");
            _Writer.WriteAttributeString("MissingValue", node.MissingValue.ToString());
            _Writer.WriteAttributeString("Type", node.Type.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitLoadNull(LoadNullNode node)
        {
            _Writer.WriteStartElement("LoadNull");
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitReadFixedString(ReadFixedStringNode node)
        {
            _Writer.WriteStartElement("ReadFixedString");
            _Writer.WriteAttributeString("Length", node.Length.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitReadType(ReadTypeNode node)
        {
            _Writer.WriteStartElement("ReadType");
            _Writer.WriteAttributeString("IsNibble", node.IsNibble.ToString());
            _Writer.WriteAttributeString("LengthIndex", node.LengthIndex.ToString());
            _Writer.WriteAttributeString("Type", node.Type.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitReturnRecord(ReturnRecordNode node)
        {
            _Writer.WriteStartElement("ReturnRecord");
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitSetLengthLocal(SetLengthLocalNode node)
        {
            _Writer.WriteStartElement("SetLengthLocal");
            _Writer.WriteAttributeString("ArrayFieldIndex", node.ArrayFieldIndex.ToString());
            _Writer.WriteAttributeString("LengthFieldIndex", node.LengthFieldIndex.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node)
        {
            _Writer.WriteStartElement("SkipArrayAssignmentIfLengthIsZero");
            _Writer.WriteAttributeString("LengthIndex", node.LengthIndex.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node)
        {
            _Writer.WriteStartElement("SkipAssignmentIfFlagSet");
            _Writer.WriteAttributeString("FlagFieldIndex", node.FlagFieldIndex.ToString());
            _Writer.WriteAttributeString("FlagMask", string.Format("0x{0:x}", node.FlagMask));
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitSkipAssignmentIfMissingValue(SkipAssignmentIfMissingValueNode node)
        {
            _Writer.WriteStartElement("SkipAssignmentIfMissingValue");
            _Writer.WriteAttributeString("MissingValue", node.MissingValue.ToString());
            _Writer.WriteAttributeString("Type", node.MissingValue.GetType().ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitSkipRawBytes(SkipRawBytesNode node)
        {
            _Writer.WriteStartElement("SkipRawBytes");
            _Writer.WriteAttributeString("Bytes", node.Bytes.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitSkipType(SkipTypeNode node)
        {
            _Writer.WriteStartElement("SkipType");
            _Writer.WriteAttributeString("LengthIndex", node.LengthIndex.ToString());
            _Writer.WriteAttributeString("IsNibble", node.IsNibble.ToString());
            _Writer.WriteAttributeString("Type", node.Type.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitThrowInvalidOperation(ThrowInvalidOperationNode node)
        {
            _Writer.WriteStartElement("ThrowInvalidOperation");
            _Writer.WriteAttributeString("Message", node.Message);
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitTryFinallyNode(TryFinallyNode node)
        {
            _Writer.WriteStartElement("TryFinally");
            _Writer.WriteStartElement("Try");
            Visit(node.TryNode);
            _Writer.WriteEndElement();
            _Writer.WriteStartElement("Finally");
            Visit(node.FinallyNode);
            _Writer.WriteEndElement();
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitUnconverterShell(UnconverterShellNode node)
        {
            _Writer.WriteStartElement("UnconverterShell");
            Visit(node.Block);
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitValidateSharedLengthLocal(ValidateSharedLengthLocalNode node)
        {
            _Writer.WriteStartElement("ValidateSharedLengthLocal");
            _Writer.WriteAttributeString("ArrayFieldIndex", node.ArrayFieldIndex.ToString());
            _Writer.WriteAttributeString("LengthFieldIndex", node.LengthFieldIndex.ToString());
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitWriteField(WriteFieldNode node)
        {
            _Writer.WriteStartElement("WriteField");
            _Writer.WriteAttributeString("FieldIndex", node.FieldIndex.ToString());
            _Writer.WriteAttributeString("FieldType", node.FieldType.ToString());
            _Writer.WriteAttributeString("Property", node.Property.Name);
            _Writer.WriteAttributeString("OptionalFieldIndex", node.OptionalFieldIndex.ToString());
            _Writer.WriteAttributeString("OptionaFieldMask", string.Format("0x{0:x}", node.OptionaFieldMask));
            _Writer.WriteStartElement("Initialization");
            Visit(node.Initialization);
            _Writer.WriteEndElement();
            _Writer.WriteStartElement("NoValueWriteContingency");
            Visit(node.NoValueWriteContingency);
            _Writer.WriteEndElement();
            _Writer.WriteStartElement("WriteOperation");
            Visit(node.WriteOperation);
            _Writer.WriteEndElement();
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitWriteFixedString(WriteFixedStringNode node)
        {
            _Writer.WriteStartElement("WriteFixedString");
            _Writer.WriteAttributeString("StringLength", node.StringLength.ToString());
            _Writer.WriteStartElement("ValueSource");
            Visit(node.ValueSource);
            _Writer.WriteEndElement();
            _Writer.WriteEndElement();
            return node;
        }
        public override CodeNode VisitWriteType(WriteTypeNode node)
        {
            _Writer.WriteStartElement("WriteType");
            _Writer.WriteAttributeString("Type", node.Type.ToString());
            _Writer.WriteStartElement("ValueSource");
            Visit(node.ValueSource);
            _Writer.WriteEndElement();
            _Writer.WriteEndElement();
            return node;
        }
        public override string ToString()
        {
            return _Output.ToString();
        }
    }
}
