// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;

namespace LinqToStdf.RecordConverting
{
    abstract class CodeNodeVisitor
    {
        public CodeNode Visit(CodeNode node)
        {
            return node.Accept(this);
        }
        public virtual CodeNode VisitInitializeRecord(InitializeRecordNode node)
        {
            return node;
        }
        public virtual CodeNode VisitEnsureCompat(EnsureCompatNode node)
        {
            return node;
        }
        public virtual CodeNode VisitInitReaderNode(InitReaderNode node)
        {
            return node;
        }
        public virtual CodeNode VisitTryFinallyNode(TryFinallyNode node)
        {
            var tryNode = Visit(node.TryNode);
            var finallyNode = Visit(node.FinallyNode);
            if (tryNode == node.TryNode && finallyNode == node.FinallyNode) return node;
            else return new TryFinallyNode(tryNode, finallyNode);
        }
        public virtual CodeNode VisitDisposeReader(DisposeReaderNode node)
        {
            return node;
        }
        public virtual CodeNode VisitBlock(BlockNode node)
        {
            return new BlockNode(from n in node.Nodes select Visit(n));
        }
        public virtual CodeNode VisitFieldAssignmentBlock(FieldAssignmentBlockNode node)
        {
            return node;
        }
        public virtual CodeNode VisitReturnRecord(ReturnRecordNode node)
        {
            return node;
        }
        public virtual CodeNode VisitSkipRawBytes(SkipRawBytesNode node)
        {
            return node;
        }
        public virtual CodeNode VisitSkipType(SkipTypeNode node)
        {
            return node;
        }
        public virtual CodeNode VisitReadFixedString(ReadFixedStringNode node)
        {
            return node;
        }
        public virtual CodeNode VisitReadType(ReadTypeNode node)
        {
            return node;
        }
        public virtual CodeNode VisitFieldAssignment(FieldAssignmentNode node)
        {
            var visitedReadNode = Visit(node.ReadNode);
            var visitedConditionalsBlock = Visit(node.AssignmentBlock);
            if (visitedReadNode == node.ReadNode && visitedConditionalsBlock == node.AssignmentBlock) return node;
            else return new FieldAssignmentNode(node.Type, node.FieldIndex, visitedReadNode, visitedConditionalsBlock as BlockNode ?? new BlockNode(visitedConditionalsBlock));
        }
        public virtual CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node)
        {
            return node;
        }
        public virtual CodeNode VisitSkipAssignmentIfMissingValue(SkipAssignmentIfMissingValueNode node)
        {
            return node;
        }
        public virtual CodeNode VisitAssignFieldToProperty(AssignFieldToPropertyNode node)
        {
            return node;
        }
        public virtual CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node)
        {
            return node;
        }

        //unconverter node visiting
        public virtual CodeNode VisitUnconverterShell(UnconverterShellNode node)
        {
            var visitedBlock = Visit(node.Block);
            if (visitedBlock == node.Block) return node;
            else return new UnconverterShellNode(visitedBlock as BlockNode ?? new BlockNode(visitedBlock));
        }
        public virtual CodeNode VisitCreateFieldLocalForWriting(CreateFieldLocalForWritingNode node)
        {
            return node;
        }
        public virtual CodeNode VisitWriteField(WriteFieldNode node)
        {
            //TODO: do this right;
            throw new NotSupportedException("WriteFieldNodes are too complicated to transform during visiting. :)");
        }
        public virtual CodeNode VisitWriteFixedString(WriteFixedStringNode node)
        {
            var visited = Visit(node.ValueSource);
            if (visited == node.ValueSource) return node;
            else return new WriteFixedStringNode(node.StringLength, visited);
        }
        public virtual CodeNode VisitWriteType(WriteTypeNode node)
        {
            var visited = Visit(node.ValueSource);
            if (visited == node.ValueSource) return node;
            else return new WriteTypeNode(node.Type, visited);
        }
        public virtual CodeNode VisitLoadMissingValue(LoadMissingValueNode node)
        {
            return node;
        }
        public virtual CodeNode VisitLoadFieldLocal(LoadFieldLocalNode node)
        {
            return node;
        }
        public virtual CodeNode VisitLoadNull(LoadNullNode node)
        {
            return node;
        }
        public virtual CodeNode VisitThrowInvalidOperation(ThrowInvalidOperationNode node)
        {
            return node;
        }
        public virtual CodeNode VisitValidateSharedLengthLocal(ValidateSharedLengthLocalNode node)
        {
            return node;
        }
        public virtual CodeNode VisitSetLengthLocal(SetLengthLocalNode node)
        {
            return node;
        }
    }
}
