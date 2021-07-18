// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StdfRecordGenerator
{
    class ConverterEmittingVisitor : CodeNodeVisitor
    {
        static ConverterEmittingVisitor()
        {
            _LinqToStdfNamespace = AliasQualifiedName(
                    IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                    IdentifierName("LinqToStdf"));
            _UnknownRecordType = QualifiedName(
                _LinqToStdfNamespace,
                IdentifierName("UnknownRecord"));
        }
        public ConverterEmittingVisitor(TypeSyntax recordType, bool enableLog =false)
        {
            _RecordType = recordType;
            EnableLog = enableLog;

        }
        public bool EnableLog { get; }

        readonly TypeSyntax _RecordType;
        readonly static SyntaxToken _ConcreteRecordLocal = Identifier("record");
        readonly static SyntaxToken _Reader = Identifier("reader");
        readonly static SyntaxToken _DoneLabel = Identifier("DoneAssigning");

        List<StatementSyntax> _CurrentBlockContents = new();
        Queue<SyntaxToken> _PendingLabels = new();
        void AddStatement(StatementSyntax statement)
        {
            while (_PendingLabels.Count > 0)
            {
                var label = _PendingLabels.Dequeue();
                statement = LabeledStatement(label, statement);
            }
            _CurrentBlockContents.Add(statement);
        }

        static readonly AliasQualifiedNameSyntax _LinqToStdfNamespace;
        static readonly TypeSyntax _UnknownRecordType;
        static readonly IdentifierNameSyntax _UnknownRecordParameter = IdentifierName("unknownRecord");

        bool _InFieldAssignmentBlock = false;
        Label _EndLabel;
        Label _SkipAssignmentLabel;
        readonly Dictionary<int, SyntaxToken> _FieldLocals = new Dictionary<int, SyntaxToken>();

        SyntaxToken? _FieldLocal = null;

        void Log(string msg)
        {
            if (EnableLog)
            {
                ILGen.Log(msg);
            }
        }
        public override CodeNode VisitInitializeRecord(InitializeRecordNode node)
        {
            Log($"Initializing {_RecordType}");
            AddStatement(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName(Token(SyntaxKind.VarKeyword)),
                        SeparatedList<VariableDeclaratorSyntax>(new[] {
                            VariableDeclarator(
                                _ConcreteRecordLocal,
                                argumentList: null,
                                initializer: EqualsValueClause(
                                    ObjectCreationExpression(_RecordType)))}))));
            return node;
        }
        public override CodeNode VisitEnsureCompat(EnsureCompatNode node)
        {
            Log($"Ensuring compatibility of record");
            AddStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _UnknownRecordParameter, IdentifierName("EnsureConvertibleTo")),
                        ArgumentList(SeparatedList<ArgumentSyntax>(new[] { Argument(IdentifierName(_ConcreteRecordLocal)) })))));
            return node;
        }

        public override CodeNode VisitInitReaderNode(InitReaderNode node)
        {
            Log($"Initializing reader");
            AddStatement(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName(Token(SyntaxKind.VarKeyword)),
                        SeparatedList<VariableDeclaratorSyntax>(new[] {
                            VariableDeclarator(
                                _Reader,
                                argumentList: null,
                                initializer: EqualsValueClause(
                                    InvocationExpression(
                                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _UnknownRecordParameter, IdentifierName("GetBinaryReaderForContent"))))) }))));
            return node;
        }
        public override CodeNode VisitTryFinallyNode(TryFinallyNode node)
        {
            var outerBlock = _CurrentBlockContents;
            _CurrentBlockContents = new();
            Visit(node.TryNode);
            var tryBlock = _CurrentBlockContents;
            _CurrentBlockContents = new();
            Visit(node.FinallyNode);
            var finallyBlock = _CurrentBlockContents;
            _CurrentBlockContents = outerBlock;
            AddStatement(TryStatement(Block(tryBlock), List<CatchClauseSyntax>(), FinallyClause(Block(finallyBlock))));
            return node;
        }
        public override CodeNode VisitDisposeReader(DisposeReaderNode node)
        {
            Log($"Disposing reader");
            AddStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName("Dispose")),
                        ArgumentList())));
            return node;
        }
        public override CodeNode VisitFieldAssignmentBlock(FieldAssignmentBlockNode node)
        {
            _InFieldAssignmentBlock = true;
            try
            {
                Log($"Handling field assignments.");

                var visitedBlock = VisitBlock(node.Block);
                //pend the label so it goes on the next statement
                _PendingLabels.Enqueue(_DoneLabel);
                if (visitedBlock == node.Block) return node;
                else return new FieldAssignmentBlockNode(visitedBlock as BlockNode ?? new BlockNode(visitedBlock));
            }
            finally
            {
                _InFieldAssignmentBlock = false;
            }
        }
        public override CodeNode VisitReturnRecord(ReturnRecordNode node)
        {
            Log($"returning record.");
            AddStatement(ReturnStatement(IdentifierName(_ConcreteRecordLocal)));
            return node;
        }
        public override CodeNode VisitSkipRawBytes(SkipRawBytesNode node)
        {
            Log($"Skipping {node.Bytes} bytes.");
            AddStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName("Skip")),
                        ArgumentList(SeparatedList<ArgumentSyntax>(NodeOrTokenList(Literal(node.Bytes)))))));
            return node;
        }

        public override CodeNode VisitSkipType(SkipTypeNode node)
        {
            MethodInfo? skipTypeMethod;
            var argsArray = node.Type.IsArray ? new[] { typeof(int) } : new Type[0];
            var skipTypeRecord = node switch
            {
                { IsNibble: true}=> "SkipNibbleArray",
                { Type: typeof(byte) }=> "Skip1",
                { Type:typeof(byte[]) }=>"Skip1Array",
                { Type:typeof(sbyte) } =>"Skip1",
                { Type:typeof(sbyte[]) } =>"Skip1Array",
                { Type:typeof(ushort) } =>"Skip2",
                { Type:typeof(ushort[]) } =>"Skip2Array",
                { Type:typeof(short) } =>"Skip2",
                { Type:typeof(short[]) =>"Skip2Array",
                { Type:typeof(uint) }=>"Skip4",
                { Type:typeof(uint[]) } =>"Skip4Array",
                { Type:typeof(int) } =>"Skip4",
                { Type:typeof(int[]) }=>"Skip4Array",
                { Type:typeof(ulong) } =>"Skip8",
                { Type:typeof(ulong[]) }=>"Skip8Array",
                { Type:typeof(long) }=>"Skip8",
                { Type:typeof(long[])) skipTypeMethodName = "Skip8Array";
                { Type:typeof(float)) skipTypeMethodName = "Skip4";
                { Type:typeof(float[])) skipTypeMethodName = "Skip4Array";
                { Type:typeof(double)) skipTypeMethodName = "Skip8";
                { Type:typeof(double[])) skipTypeMethodName = "Skip8Array";
                { Type:typeof(string)) skipTypeMethodName = "SkipString";
                { Type:typeof(string[])) skipTypeMethodName = "SkipStringArray";
                { Type:typeof(DateTime)) skipTypeMethodName = "Skip4";
                { Type: typeof(BitArray)) skipTypeMethodName = "SkipBitArray";
                else
                {
                    throw new NotSupportedException(string.Format(Resources.UnsupportedReaderType, node.Type));
                }
                skipTypeMethod = typeof(BinaryReader).GetMethodOrThrow(skipTypeMethodName, argsArray);
                _SkipTypeMethods[node.Type] = skipTypeMethod;
            }
            Log($"Skipping with {skipTypeMethod}.");

            ILGen.Ldloc(_Reader);
            //if we have a length index, load its local (we enforce its presence for arrays in the node)
            if (node.LengthIndex.HasValue)
            {
                ILGen.Ldloc(_FieldLocals[node.LengthIndex.Value]);
            }
            ILGen.Callvirt(skipTypeMethod);
            return node;
        }
        static readonly MethodInfo _ReadFixedStringMethod = typeof(BinaryReader).GetMethodOrThrow("ReadString", typeof(int));
        public override CodeNode VisitReadFixedString(ReadFixedStringNode node)
        {
            if (_FieldLocal == null) throw new InvalidOperationException("Cannot read string outside a FieldAssignmentNode");
            Log($"Reading string of fixed length {node.Length}.");
            ILGen.Ldloc(_Reader);
            ILGen.Ldc_I4(node.Length);
            ILGen.Callvirt(_ReadFixedStringMethod);
            ILGen.Stloc(_FieldLocal);
            return node;
        }

        readonly Dictionary<Type, MethodInfo> _ReadTypeMethods = new Dictionary<Type, MethodInfo>();
        public override CodeNode VisitReadType(ReadTypeNode node)
        {
            if (_FieldLocal == null) throw new InvalidOperationException("Cannot read string outside a FieldAssignmentNode");
            MethodInfo? readTypeMethod;
            var argsArray = node.Type.IsArray ? new[] { typeof(int) } : new Type[0];
            if (node.IsNibble) readTypeMethod = typeof(BinaryReader).GetMethodOrThrow("ReadNibbleArray", argsArray);
            else if (!_ReadTypeMethods.TryGetValue(node.Type, out readTypeMethod))
            {
                string readTypeMethodName;
                if (node.Type == typeof(byte)) readTypeMethodName = "ReadByte";
                else if (node.Type == typeof(byte[])) readTypeMethodName = "ReadByteArray";
                else if (node.Type == typeof(sbyte)) readTypeMethodName = "ReadSByte";
                else if (node.Type == typeof(sbyte[])) readTypeMethodName = "ReadSByteArray";
                else if (node.Type == typeof(ushort)) readTypeMethodName = "ReadUInt16";
                else if (node.Type == typeof(ushort[])) readTypeMethodName = "ReadUInt16Array";
                else if (node.Type == typeof(short)) readTypeMethodName = "ReadInt16";
                else if (node.Type == typeof(short[])) readTypeMethodName = "ReadInt16Array";
                else if (node.Type == typeof(uint)) readTypeMethodName = "ReadUInt32";
                else if (node.Type == typeof(uint[])) readTypeMethodName = "ReadUInt32Array";
                else if (node.Type == typeof(int)) readTypeMethodName = "ReadInt32";
                else if (node.Type == typeof(int[])) readTypeMethodName = "ReadInt32Array";
                else if (node.Type == typeof(ulong)) readTypeMethodName = "ReadUInt64";
                else if (node.Type == typeof(ulong[])) readTypeMethodName = "ReadUInt64Array";
                else if (node.Type == typeof(long)) readTypeMethodName = "ReadInt64";
                else if (node.Type == typeof(long[])) readTypeMethodName = "ReadInt64Array";
                else if (node.Type == typeof(float)) readTypeMethodName = "ReadSingle";
                else if (node.Type == typeof(float[])) readTypeMethodName = "ReadSingleArray";
                else if (node.Type == typeof(double)) readTypeMethodName = "ReadDouble";
                else if (node.Type == typeof(double[])) readTypeMethodName = "ReadDoubleArray";
                else if (node.Type == typeof(string)) readTypeMethodName = "ReadString";
                else if (node.Type == typeof(string[])) readTypeMethodName = "ReadStringArray";
                else if (node.Type == typeof(DateTime)) readTypeMethodName = "ReadDateTime";
                else if (node.Type == typeof(BitArray)) readTypeMethodName = "ReadBitArray";
                else
                {
                    throw new NotSupportedException(string.Format(Resources.UnsupportedReaderType, node.Type));
                }
                readTypeMethod = typeof(BinaryReader).GetMethodOrThrow(readTypeMethodName, argsArray);
                _ReadTypeMethods[node.Type] = readTypeMethod;
            }
            Log($"Reading with {readTypeMethod.Name}.");

            ILGen.Ldloc(_Reader);
            //if we have a length index, load its local (we enforce its presence for arrays in the node)
            if (node.LengthIndex.HasValue)
            {
                ILGen.Ldloc(_FieldLocals[node.LengthIndex.Value]);
            }
            ILGen.Callvirt(readTypeMethod);
            ILGen.Stloc(_FieldLocal);
            return node;
        }
        static readonly MethodInfo _AtEndOfStreamMethod = typeof(BinaryReader).GetProperty("AtEndOfStream")?.GetGetMethod() ?? throw new InvalidOperationException("Could not get getter for AtEndOfStream");
        public override CodeNode VisitFieldAssignment(FieldAssignmentNode node)
        {
            //ensure we're in a FieldAssignmentBlock
            if (!_InFieldAssignmentBlock)
            {
                throw new InvalidOperationException("EndOfStreamCheckNode must occur within a FieldAssignmentBlockNode");
            }
            _FieldLocal = ILGen.DeclareLocal(node.Type);
            try
            {
                Log($"Handling field {node.FieldIndex}.");
                //generate the end of stream check
                ILGen.Ldloc(_Reader);
                ILGen.Callvirt(_AtEndOfStreamMethod);
                ILGen.Brtrue(_EndLabel);

                //declare the local and enable it in the scope of child visiting
                _FieldLocals[node.FieldIndex] = _FieldLocal;

                _SkipAssignmentLabel = ILGen.DefineLabel();
                var assignmentCompleted = ILGen.DefineLabel();

                //visit any read node there is
                Visit(node.ReadNode);

                //visit any assignment block
                if (node.AssignmentBlock != null)
                {
                    Visit(node.AssignmentBlock);
                }
                else
                {
                    Log($"No assignment for {node.FieldIndex}.");
                }
                ILGen.Br(assignmentCompleted);
                ILGen.MarkLabel(_SkipAssignmentLabel);
                Log($"Assignment skipped.");
                ILGen.MarkLabel(assignmentCompleted);
                Log($"Done with {node.FieldIndex}.");

                _FieldLocal = null;

                return node;
            }
            finally
            {
                _FieldLocal = null;
            }
        }
        public override CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("SkipAssignmentIfFlagSetNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based on field {node.FlagFieldIndex}, mask 0x{node.FlagMask:x}.");
            //get the flag field
            LocalBuilder flag = _FieldLocals[node.FlagFieldIndex];
            //TODO: assert that it's a byte?
            ILGen.Ldloc(flag);
            ILGen.Ldc_I4_S(node.FlagMask);
            ILGen.And();
            //skip assignment if the flag is set
            ILGen.Brtrue(_SkipAssignmentLabel);
            return node;
        }
        public override CodeNode VisitSkipAssignmentIfMissingValue(SkipAssignmentIfMissingValueNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("SkipAssignmentIfMissingValueNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based on missing value {node.MissingValue}.");
            ILGen.Ldloca(_FieldLocal);
            ILGen.Ldc(node.MissingValue, node.MissingValue.GetType());
            //BUG: Revisit this.  The purpose of constrained is so you don't have to do different codegen for valuetype vs. reference type
            if (_FieldLocal.LocalType.IsValueType)
            {
                ILGen.Box(_FieldLocal.LocalType);
            }
            ILGen.Constrained(_FieldLocal.LocalType);
            // TODO: if (!node.AllowMissingValue) { do the next two lines... }
            ILGen.Callvirt(typeof(object).GetMethodOrThrow("Equals", typeof(object)));
            ILGen.Brtrue(_SkipAssignmentLabel);
            return node;
        }
        public override CodeNode VisitAssignFieldToProperty(AssignFieldToPropertyNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("AssignFieldToPropertyNode must be in a FieldAssignmentNode");
            Log($"Assigning value to {node.Property.Name}.");
            ILGen.Ldloc(_ConcreteRecordLocal);
            ILGen.Ldloc(_FieldLocal);
            //handle the case where the property is a nullable version of the field type
            if (node.FieldType.IsValueType)
            {
                Type genericType = typeof(Nullable<>).MakeGenericType(node.FieldType);
                if (node.Property.PropertyType == genericType)
                {
                    ILGen.Newobj(genericType, node.FieldType);
                }
            }
            //assign the value to the property
            ILGen.Callvirt(node.Property.GetSetMethod() ?? throw new InvalidOperationException("Could not get setter for property."));
            return node;
        }
        public override CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("SkipArrayAssignmentIfLengthIsZeroNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based zero length array.");
            //If the length field is zero, skip past assignment
            var lengthLocal = _FieldLocals[node.LengthIndex];
            ILGen.Ldloc(lengthLocal);
            ILGen.Brfalse(_SkipAssignmentLabel);
            return node;
        }
    }
}
