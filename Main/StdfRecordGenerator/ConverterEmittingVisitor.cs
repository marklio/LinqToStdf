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
                    _GlobalNamespace,
                    IdentifierName("LinqToStdf"));
            _SystemNamespace = AliasQualifiedName(
                    _GlobalNamespace,
                    IdentifierName("System"));
            _SystemCollectionsNamespace = QualifiedName(
                _SystemNamespace,
                IdentifierName("Collections"));
            _UnknownRecordType = QualifiedName(
                _LinqToStdfNamespace,
                IdentifierName("UnknownRecord"));
        }
        public ConverterEmittingVisitor(string recordTypeName, TypeSyntax recordType, bool enableLog =false)
        {
            _RecordTypeName = recordTypeName;
            _RecordType = recordType;
            EnableLog = enableLog;
            _ActiveBlock = new BlockScope(this);

        }
        public bool EnableLog { get; }

        readonly string _RecordTypeName;
        readonly TypeSyntax _RecordType;
        readonly static SyntaxToken _ConcreteRecordLocal = Identifier("record");
        readonly static SyntaxToken _Reader = Identifier("reader");
        readonly static LazyLabel _DoneLabel = new ("DoneAssigning");

        class BlockScope : IDisposable
        {
            bool _Disposed = false;
            readonly List<StatementSyntax> _Statements = new();
            readonly Queue<LazyLabel> _PendingLabels = new();
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

            public BlockScope CreateChildScope() => new (this);

            public void AddStatement(StatementSyntax statement)
            {
                if (_Disposed) throw new ObjectDisposedException(nameof(BlockScope));
                while (_PendingLabels.Count > 0)
                {
                    var label = _PendingLabels.Dequeue();
                    if (label.IsUsed)
                    {
                        statement = LabeledStatement(label.GetSyntaxToken(), statement);
                    }
                }
                _Statements.Add(statement);
            }

            public void PrependLabel(LazyLabel label)
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
                        if (label.IsUsed)
                        {
                            statement = LabeledStatement(label.GetSyntaxToken(), statement);
                        }
                    }
                    //TODO: conditionally based on use?
                    _Statements.Add(statement);
                }
                _Disposed = true;
            }
        }
        readonly BlockScope _ActiveBlock;

        static readonly IdentifierNameSyntax _GlobalNamespace = IdentifierName(Token(SyntaxKind.GlobalKeyword));
        static readonly AliasQualifiedNameSyntax _SystemNamespace;
        static readonly QualifiedNameSyntax _SystemCollectionsNamespace;
        static readonly AliasQualifiedNameSyntax _LinqToStdfNamespace;
        static readonly TypeSyntax _UnknownRecordType;
        static readonly IdentifierNameSyntax _UnknownRecordParameter = IdentifierName("unknownRecord");
        static readonly IdentifierNameSyntax _VarType = IdentifierName(
                                            Identifier(
                                                TriviaList(),
                                                SyntaxKind.VarKeyword,
                                                "var",
                                                "var",
                                                TriviaList()));

        class LazyLabel
        {
            SyntaxToken? _LabelToken;
            public LazyLabel(string label)
            {
                Label = label;
            }
            public string Label { get; }

            public SyntaxToken GetSyntaxToken() => _LabelToken ??= Identifier(Label);

            public bool IsUsed => _LabelToken is not null;
        }

        bool _InFieldAssignmentBlock = false;
        LazyLabel? _SkipAssignmentLabel;
        readonly Dictionary<int, SyntaxToken> _FieldLocals = new();

        SyntaxToken? _FieldLocal = null;

        public MethodDeclarationSyntax GetConverterMethod()
        {
            _ActiveBlock.Dispose();
            return MethodDeclaration(_RecordType, $"ConvertTo{_RecordTypeName}")
                .WithParameterList(ParameterList(SeparatedList<ParameterSyntax>(NodeOrTokenList(Parameter(_UnknownRecordParameter.Identifier).WithType(_UnknownRecordType)))))
                .WithBody(_ActiveBlock.GetBlock());
        }

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
                        _VarType,
                        SeparatedList<VariableDeclaratorSyntax>(NodeOrTokenList(
                            VariableDeclarator(
                                _ConcreteRecordLocal,
                                argumentList: null,
                                initializer: EqualsValueClause(
                                    ObjectCreationExpression(_RecordType)
                                    .WithArgumentList(ArgumentList()))))))));
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
                        _VarType,
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
                            ArgumentList(SingletonSeparatedList<ArgumentSyntax>(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(node.Length)))))))));
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
                    FieldTypes.String => "ReadString",
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
                ArgumentList(SeparatedList<ArgumentSyntax>(NodeOrTokenList(Argument(IdentifierName(_FieldLocals[node.LengthIndex.Value])))));
            _ActiveBlock.AddStatement(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(_FieldLocal.Value),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName(readTypeMethod)),
                            args))));
            return node;
        }
        static readonly SyntaxList<ArrayRankSpecifierSyntax> _EmptyArrayRank = SingletonList<ArrayRankSpecifierSyntax>(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())));
        TypeSyntax GetTypeFor(FieldTypes fieldType, bool isArray) => isArray switch {
            true => fieldType switch
            {
                FieldTypes.I1 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.SByteKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.I2 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.ShortKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.I4 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.IntKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.Nibble => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.R4 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.FloatKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.R8 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.DoubleKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.U1 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.U2 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.UShortKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                FieldTypes.U4 => NullableType(ArrayType(PredefinedType(Token(SyntaxKind.UIntKeyword))).WithRankSpecifiers(_EmptyArrayRank)),
                _=> throw new InvalidOperationException($"Unsupported array field type {fieldType}")
            },
            false => fieldType switch
            {
                FieldTypes.BitField => NullableType(QualifiedName(_SystemCollectionsNamespace, IdentifierName("BitArray"))),
                FieldTypes.LongBitField => NullableType(QualifiedName(_SystemCollectionsNamespace, IdentifierName("BitArray"))),
                FieldTypes.DateTime => QualifiedName(_SystemNamespace, IdentifierName("DateTime")),
                FieldTypes.I1 => PredefinedType(Token(SyntaxKind.SByteKeyword)),
                FieldTypes.I2 => PredefinedType(Token(SyntaxKind.ShortKeyword)),
                FieldTypes.I4 => PredefinedType(Token(SyntaxKind.IntKeyword)),
                FieldTypes.U1 => PredefinedType(Token(SyntaxKind.ByteKeyword)),
                FieldTypes.U2 => PredefinedType(Token(SyntaxKind.UShortKeyword)),
                FieldTypes.U4 => PredefinedType(Token(SyntaxKind.UIntKeyword)),
                FieldTypes.R4 => PredefinedType(Token(SyntaxKind.FloatKeyword)),
                FieldTypes.R8 => PredefinedType(Token(SyntaxKind.DoubleKeyword)),
                FieldTypes.String => PredefinedType(Token(SyntaxKind.StringKeyword)),
                _ => throw new InvalidOperationException($"Unsupported field type {fieldType}")
            },
        };


        public override CodeNode VisitFieldAssignment(FieldAssignmentNode node)
        {
            //ensure we're in a FieldAssignmentBlock
            if (!_InFieldAssignmentBlock)
            {
                throw new InvalidOperationException("Field assignment must occur within a FieldAssignmentBlockNode");
            }
            _FieldLocal = Identifier($"field{node.FieldIndex}");
            _SkipAssignmentLabel = new LazyLabel($"SkipAssignment{node.FieldIndex}");
            _FieldLocals[node.FieldIndex] = _FieldLocal.Value;
            _ActiveBlock.AddStatement(
                LocalDeclarationStatement(
                    VariableDeclaration(GetTypeFor(node.Type, node.IsArray))
                    .WithVariables(SingletonSeparatedList<VariableDeclaratorSyntax>(VariableDeclarator(_FieldLocal.Value)))));
            try
            {
                Log($"Handling field {node.FieldIndex}.");
                //generate the end of stream check
                _ActiveBlock.AddStatement(
                    IfStatement(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(_Reader), IdentifierName("AtEndOfStream")),
                        GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_DoneLabel.GetSyntaxToken()))));

                var assignmentCompleted = new LazyLabel($"AssignmentComplete{node.FieldIndex}");

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
                //TODO: all this has to do with the log statement below. we don't need this goto when logging isn't enabled.
                //we need to figure out how to not have logging be so convoluted.
                if (EnableLog)
                {
                    _ActiveBlock.AddStatement(GotoStatement(SyntaxKind.GotoStatement, IdentifierName(assignmentCompleted.GetSyntaxToken())));
                }
                _ActiveBlock.PrependLabel(_SkipAssignmentLabel);
                if (EnableLog)
                {
                    Log($"Assignment skipped.");
                    _ActiveBlock.PrependLabel(assignmentCompleted);
                    Log($"Done with {node.FieldIndex}.");
                }

                return node;
            }
            finally
            {
                _FieldLocal = null;
                _SkipAssignmentLabel = null;
            }
        }
        public override CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node)
        {
            if (_FieldLocal is null || _SkipAssignmentLabel is null) throw new InvalidOperationException("SkipAssignmentIfFlagSetNode must be in a FieldAssignmentNode");
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
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_SkipAssignmentLabel.GetSyntaxToken()))));
            return node;
        }
        public override CodeNode VisitSkipAssignmentIfMissingValue(SkipAssignmentIfMissingValueNode node)
        {
            if (_FieldLocal is null || _SkipAssignmentLabel is null) throw new InvalidOperationException("SkipAssignmentIfMissingValueNode must be in a FieldAssignmentNode");
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
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_SkipAssignmentLabel.GetSyntaxToken()))));
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
            if (_FieldLocal is null || _SkipAssignmentLabel is null) throw new InvalidOperationException("SkipArrayAssignmentIfLengthIsZeroNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based zero length array.");
            //If the length field is zero, skip past assignment
            var lengthLocal = _FieldLocals[node.LengthIndex];
            _ActiveBlock.AddStatement(IfStatement(
                BinaryExpression(SyntaxKind.EqualsExpression,
                    IdentifierName(lengthLocal),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                GotoStatement(SyntaxKind.GotoStatement, IdentifierName(_SkipAssignmentLabel.GetSyntaxToken()))));
            return node;
        }
    }
}
