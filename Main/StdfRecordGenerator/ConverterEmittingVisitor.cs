// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
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
            _ActiveBlock = new BlockScope(this);

        }
        public bool EnableLog { get; }

        readonly TypeSyntax _RecordType;
        readonly static SyntaxToken _ConcreteRecordLocal = Identifier("record");
        readonly static SyntaxToken _Reader = Identifier("reader");
        readonly static SyntaxToken _DoneLabel = Identifier("DoneAssigning");

        class BlockScope : IDisposable
        {
            bool _Disposed = false;
            readonly List<StatementSyntax> _Statements = new();
            readonly Queue<SyntaxToken> _PendingLabels = new();
            readonly object _Parent;
            BlockScope(BlockScope parent)
            {
                _Parent = parent;
            }

            public BlockScope(ConverterEmittingVisitor visitor)
            {
                _Parent = visitor;
            }

            ConverterEmittingVisitor GetVisitor() => _Parent switch
            {
                ConverterEmittingVisitor cve => cve,
                BlockScope bs => bs.GetVisitor(),
                _ => throw new InvalidOperationException("Something bizarre assigned to the "),
            };

            public BlockScope CreateChildScope() => new BlockScope(this);

            public void AddStatement(StatementSyntax statement)
            {
                if (_Disposed) throw new ObjectDisposedException(nameof(BlockScope));
                while (_PendingLabels.Count > 0)
                {
                    var label = _PendingLabels.Dequeue();
                    statement = LabeledStatement(label, statement);
                }
                _Statements.Add(statement);
            }

            public void PrependLabel(SyntaxToken label)
            {
                if (_Disposed) throw new ObjectDisposedException(nameof(BlockScope));
                _PendingLabels.Enqueue(label);
            }

            public BlockSyntax GetBlock() => _Disposed ? Block(_Statements) : throw new InvalidOperationException("Scope not yet disposed");

            public void Dispose()
            {
                if (_Disposed) return;
                if (_PendingLabels.Count > 0)
                {
                    StatementSyntax statement = ExpressionStatement(IdentifierName(MissingToken(SyntaxKind.IdentifierToken)));
                    //emit any labels
                    while (_PendingLabels.Count > 0)
                    {
                        var label = _PendingLabels.Dequeue();
                        statement = LabeledStatement(label, statement);
                    }
                }
                _Disposed = true;
            }
        }
        readonly BlockScope _ActiveBlock;

        static readonly AliasQualifiedNameSyntax _LinqToStdfNamespace;
        static readonly TypeSyntax _UnknownRecordType;
        static readonly IdentifierNameSyntax _UnknownRecordParameter = IdentifierName("unknownRecord");

        bool _InFieldAssignmentBlock = false;
        SyntaxToken _EndLabel;
        SyntaxToken _SkipAssignmentLabel;
        readonly Dictionary<int, SyntaxToken> _FieldLocals = new Dictionary<int, SyntaxToken>();

        SyntaxToken? _FieldLocal = null;

        void Log(string msg)
        {
            if (EnableLog)
            {
                _ActiveBlock.AddStatement(
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    AliasQualifiedName(
                                        IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                                        IdentifierName("System")),
                                    IdentifierName("Console")),
                                IdentifierName("WriteLine")))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList<ArgumentSyntax>(
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(msg))))))));
            }
        }
        public override CodeNode VisitInitializeRecord(InitializeRecordNode node)
        {
            Log($"Initializing {_RecordType}");
            _ActiveBlock.AddStatement(
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
            _ActiveBlock.AddStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _UnknownRecordParameter, IdentifierName("EnsureConvertibleTo")),
                        ArgumentList(SeparatedList<ArgumentSyntax>(new[] { Argument(IdentifierName(_ConcreteRecordLocal)) })))));
            return node;
        }

        public override CodeNode VisitInitReaderNode(InitReaderNode node)
        {
            Log($"Initializing reader");
            _ActiveBlock.AddStatement(
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
            var tryBlock = _ActiveBlock.CreateChildScope();
            using (tryBlock)
            {
                Visit(node.TryNode);
            }
            var finallyBlock = _ActiveBlock.CreateChildScope();
            using (finallyBlock)
            {
                Visit(node.FinallyNode);
            }
            _ActiveBlock.AddStatement(TryStatement(tryBlock.GetBlock(), List<CatchClauseSyntax>(), FinallyClause(finallyBlock.GetBlock())));
            return node;
        }
        public override CodeNode VisitDisposeReader(DisposeReaderNode node)
        {
            Log($"Disposing reader");
            _ActiveBlock.AddStatement(
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
                _ActiveBlock.PrependLabel(_DoneLabel);
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
            _ActiveBlock.AddStatement(ReturnStatement(IdentifierName(_ConcreteRecordLocal)));
            return node;
        }
        public override CodeNode VisitSkipRawBytes(SkipRawBytesNode node)
        {
            Log($"Skipping {node.Bytes} bytes.");
            _ActiveBlock.AddStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName("Skip")),
                        ArgumentList(SeparatedList<ArgumentSyntax>(NodeOrTokenList(Literal(node.Bytes)))))));
            return node;
        }

        public override CodeNode VisitSkipType(SkipTypeNode node)
        {
            throw new InvalidOperationException("Skipping no longer supported");
            /*
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
            */
        }
        public override CodeNode VisitReadFixedString(ReadFixedStringNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("Cannot read string outside a FieldAssignmentNode");
            Log($"Reading string of fixed length {node.Length}.");
            _ActiveBlock.AddStatement(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(_FieldLocal.Value),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName("ReadString")),
                            ArgumentList(SeparatedList<ArgumentSyntax>(NodeOrTokenList(Literal(node.Length))))))));
            return node;
        }

        public override CodeNode VisitReadType(ReadTypeNode node)
        {
            if (_FieldLocal == null) throw new InvalidOperationException("Cannot read value outside a FieldAssignmentNode");

            var readTypeMethod = node.LengthIndex switch
            {
                null => node.Type switch
                {
                    FieldTypes.U1 => "ReadByte",
                    FieldTypes.I1 => "ReadSByte",
                    FieldTypes.U2 => "ReadUInt16",
                    FieldTypes.I2 => "ReadInt16",
                    FieldTypes.U4 => "ReadUInt32",
                    FieldTypes.I4 => "ReadInt32",
                    FieldTypes.R4 => "ReadSingle",
                    FieldTypes.R8 => "ReadDouble",
                    FieldTypes.DateTime => "ReadDateTime",
                    FieldTypes.BitField => "ReadBitArray",
                    _ => throw new InvalidOperationException($"Unsupported type {node.Type}"),
                },
                _ => node.Type switch
                {
                    FieldTypes.U1 => "ReadByteArray",
                    FieldTypes.I1 => "ReadSByteArray",
                    FieldTypes.U2 => "ReadUInt16Array",
                    FieldTypes.I2 => "ReadInt16Array",
                    FieldTypes.U4 => "ReadUInt32Array",
                    FieldTypes.I4 => "ReadInt32Array",
                    FieldTypes.R4 => "ReadSingleArray",
                    FieldTypes.R8 => "ReadDoubleArray",
                    FieldTypes.Nibble => "ReadNibbleArray",
                    _ => throw new InvalidOperationException($"Unsupported type {node.Type}"),
                },
            };

            Log($"Reading with {readTypeMethod}.");
            var args = node.LengthIndex is null ?
                ArgumentList() :
                ArgumentList(SeparatedList<ArgumentSyntax>(NodeOrTokenList(_FieldLocals[node.LengthIndex.Value])));
            _ActiveBlock.AddStatement(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(_FieldLocal.Value),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName(readTypeMethod)),
                            args))));
            return node;
        }
        public override CodeNode VisitFieldAssignment(FieldAssignmentNode node)
        {
            //ensure we're in a FieldAssignmentBlock
            if (!_InFieldAssignmentBlock)
            {
                throw new InvalidOperationException("Field assignment must occur within a FieldAssignmentBlockNode");
            }
            _FieldLocal = Identifier($"field{node.FieldIndex}");
            _FieldLocals[node.FieldIndex] = _FieldLocal.Value;
            try
            {
                Log($"Handling field {node.FieldIndex}.");
                //generate the end of stream check
                _ActiveBlock.AddStatement(
                    IfStatement(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName("AtEndOfStream"))),
                            GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_EndLabel))));

                _SkipAssignmentLabel = Identifier($"SkipAssignment{node.FieldIndex}");
                var assignmentCompleted = Identifier($"AssignmentComplete{node.FieldIndex}");

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
                _ActiveBlock.AddStatement(GotoStatement(SyntaxKind.GotoStatement, IdentifierName(assignmentCompleted)));
                _ActiveBlock.PrependLabel(_SkipAssignmentLabel);
                Log($"Assignment skipped.");
                _ActiveBlock.PrependLabel(assignmentCompleted);
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
            var flag = _FieldLocals[node.FlagFieldIndex];
            //TODO: assert that it's a byte?
            _ActiveBlock.AddStatement(IfStatement(
                BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.BitwiseAndExpression,
                            IdentifierName(flag),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(node.FlagMask)))),
                    LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        Literal(0))),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_SkipAssignmentLabel))));
            return node;
        }
        public override CodeNode VisitSkipAssignmentIfMissingValue(SkipAssignmentIfMissingValueNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("SkipAssignmentIfMissingValueNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based on missing value {node.MissingValue}.");
            var literal = node.MissingValue switch
            {
                int i => Literal(i),
                _ => throw new InvalidOperationException($"Unsupported missing value type {node.MissingValue.GetType()}"),
            };
            // TODO: use node.AllowMissingValue
            _ActiveBlock.AddStatement(IfStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(_FieldLocal.Value),
                        IdentifierName("Equals")))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                LiteralExpression(literal.Kind(), literal))))),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_SkipAssignmentLabel))));
            return node;
        }
        public override CodeNode VisitAssignFieldToProperty(AssignFieldToPropertyNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("AssignFieldToPropertyNode must be in a FieldAssignmentNode");
            Log($"Assigning value to {node.Property}.");
            _ActiveBlock.AddStatement(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(_ConcreteRecordLocal),
                            IdentifierName(node.Property)),
                        IdentifierName(_FieldLocal.Value))));
            return node;
        }
        public override CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node)
        {
            if (_FieldLocal is null) throw new InvalidOperationException("SkipArrayAssignmentIfLengthIsZeroNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based zero length array.");
            //If the length field is zero, skip past assignment
            var lengthLocal = _FieldLocals[node.LengthIndex];
            _ActiveBlock.AddStatement(IfStatement(
                BinaryExpression(SyntaxKind.EqualsExpression,
                    IdentifierName(lengthLocal),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_SkipAssignmentLabel))));
            return node;
        }
    }
}
