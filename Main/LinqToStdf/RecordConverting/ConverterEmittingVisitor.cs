// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LinqToStdf.RecordConverting
{
    class ConverterEmittingVisitor : CodeNodeVisitor
    {
        public ILGenerator ILGen;
        public Type ConcreteType;
        public bool EnableLog = false;

        LocalBuilder _ConcreteRecordLocal;
        LocalBuilder _Reader;
        bool _InFieldAssignmentBlock = false;
        bool _InFieldAssignment = false;
        Label _EndLabel;
        Label _SkipAssignmentLabel;
        readonly Dictionary<int, LocalBuilder> _FieldLocals = new Dictionary<int, LocalBuilder>();

        LocalBuilder _FieldLocal = null;

        void Log(string msg)
        {
            if (EnableLog)
            {
                ILGen.Log(msg);
            }
        }
        public override CodeNode VisitInitializeRecord(InitializeRecordNode node)
        {
            Log($"Initializing {ConcreteType}");
            _ConcreteRecordLocal = ILGen.DeclareLocal(ConcreteType);
            ILGen.Newobj(ConcreteType);
            ILGen.Stloc(_ConcreteRecordLocal);
            return node;
        }
        static readonly MethodInfo _EnsureConvertibleToMethod = typeof(UnknownRecord).GetMethod("EnsureConvertibleTo", typeof(StdfRecord));
        public override CodeNode VisitEnsureCompat(EnsureCompatNode node)
        {
            Log($"Ensuring compatibility of record");
            ILGen.Ldarg_0();
            ILGen.Ldloc(_ConcreteRecordLocal);
            ILGen.Callvirt(_EnsureConvertibleToMethod);
            return node;
        }
        static readonly MethodInfo _GetBinaryReaderForContentMethod = typeof(UnknownRecord).GetMethod("GetBinaryReaderForContent", BindingFlags.Instance | BindingFlags.Public);

        public override CodeNode VisitInitReaderNode(InitReaderNode node)
        {
            Log($"Initializing reader");
            _Reader = ILGen.DeclareLocal<BinaryReader>();
            ILGen.Ldarg_0();
            ILGen.Callvirt(_GetBinaryReaderForContentMethod);
            ILGen.Stloc(_Reader);
            return node;
        }
        public override CodeNode VisitTryFinallyNode(TryFinallyNode node)
        {
            ILGen.BeginExceptionBlock();
            Visit(node.TryNode);
            ILGen.BeginFinallyBlock();
            Visit(node.FinallyNode);
            ILGen.EndExceptionBlock();

            return node;
        }
        static readonly MethodInfo _DisposeMethod = typeof(IDisposable).GetMethod("Dispose", new Type[0]);
        public override CodeNode VisitDisposeReader(DisposeReaderNode node)
        {
            Log($"Disposing reader");
            ILGen.Ldloc(_Reader);
            ILGen.Callvirt(_DisposeMethod);
            return node;
        }
        public override CodeNode VisitFieldAssignmentBlock(FieldAssignmentBlockNode node)
        {
            _InFieldAssignmentBlock = true;
            _EndLabel = ILGen.DefineLabel();
            try
            {
                Log($"Handling field assignments.");

                var visitedBlock = VisitBlock(node.Block);
                ILGen.MarkLabel(_EndLabel);
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
            ILGen.Ldloc(_ConcreteRecordLocal);
            ILGen.Ret();
            return node;
        }
        static readonly MethodInfo _SkipRawMethod = typeof(BinaryReader).GetMethod("Skip", typeof(int));
        public override CodeNode VisitSkipRawBytes(SkipRawBytesNode node)
        {
            Log($"Skipping {node.Bytes} bytes.");
            ILGen.Ldloc(_Reader);
            ILGen.Ldc_I4(node.Bytes);
            ILGen.Callvirt(_SkipRawMethod);
            return node;
        }

        readonly Dictionary<Type, MethodInfo> _SkipTypeMethods = new Dictionary<Type, MethodInfo>();
        public override CodeNode VisitSkipType(SkipTypeNode node)
        {
            MethodInfo skipTypeMethod;
            var argsArray = node.Type.IsArray ? new[] { typeof(int) } : new Type[0];
            if (node.IsNibble) skipTypeMethod = typeof(BinaryReader).GetMethod("SkipNibbleArray", argsArray);
            else if (!_SkipTypeMethods.TryGetValue(node.Type, out skipTypeMethod))
            {
                string skipTypeMethodName;
                if (node.Type == typeof(byte)) skipTypeMethodName = "Skip1";
                else if (node.Type == typeof(byte[])) skipTypeMethodName = "Skip1Array";
                else if (node.Type == typeof(sbyte)) skipTypeMethodName = "Skip1";
                else if (node.Type == typeof(sbyte[])) skipTypeMethodName = "Skip1Array";
                else if (node.Type == typeof(ushort)) skipTypeMethodName = "Skip2";
                else if (node.Type == typeof(ushort[])) skipTypeMethodName = "Skip2Array";
                else if (node.Type == typeof(short)) skipTypeMethodName = "Skip2";
                else if (node.Type == typeof(short[])) skipTypeMethodName = "Skip2Array";
                else if (node.Type == typeof(uint)) skipTypeMethodName = "Skip4";
                else if (node.Type == typeof(uint[])) skipTypeMethodName = "Skip4Array";
                else if (node.Type == typeof(int)) skipTypeMethodName = "Skip4";
                else if (node.Type == typeof(int[])) skipTypeMethodName = "Skip4Array";
                else if (node.Type == typeof(ulong)) skipTypeMethodName = "Skip8";
                else if (node.Type == typeof(ulong[])) skipTypeMethodName = "Skip8Array";
                else if (node.Type == typeof(long)) skipTypeMethodName = "Skip8";
                else if (node.Type == typeof(long[])) skipTypeMethodName = "Skip8Array";
                else if (node.Type == typeof(float)) skipTypeMethodName = "Skip4";
                else if (node.Type == typeof(float[])) skipTypeMethodName = "Skip4Array";
                else if (node.Type == typeof(double)) skipTypeMethodName = "Skip8";
                else if (node.Type == typeof(double[])) skipTypeMethodName = "Skip8Array";
                else if (node.Type == typeof(string)) skipTypeMethodName = "SkipString";
                else if (node.Type == typeof(string[])) skipTypeMethodName = "SkipStringArray";
                else if (node.Type == typeof(DateTime)) skipTypeMethodName = "Skip4";
                else if (node.Type == typeof(BitArray)) skipTypeMethodName = "SkipBitArray";
                else
                {
                    throw new NotSupportedException(string.Format(Resources.UnsupportedReaderType, node.Type));
                }
                skipTypeMethod = typeof(BinaryReader).GetMethod(skipTypeMethodName, argsArray);
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
        static readonly MethodInfo _ReadFixedStringMethod = typeof(BinaryReader).GetMethod("ReadString", typeof(int));
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
            MethodInfo readTypeMethod;
            var argsArray = node.Type.IsArray ? new[] { typeof(int) } : new Type[0];
            if (node.IsNibble) readTypeMethod = typeof(BinaryReader).GetMethod("ReadNibbleArray", argsArray);
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
                readTypeMethod = typeof(BinaryReader).GetMethod(readTypeMethodName, argsArray);
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
        static readonly MethodInfo _AtEndOfStreamMethod = typeof(BinaryReader).GetProperty("AtEndOfStream").GetGetMethod();
        public override CodeNode VisitFieldAssignment(FieldAssignmentNode node)
        {
            //ensure we're in a FieldAssignmentBlock
            if (!_InFieldAssignmentBlock)
            {
                throw new InvalidOperationException("EndOfStreamCheckNode must occur within a FieldAssignmentBlockNode");
            }
            _InFieldAssignment = true;
            try
            {
                Log($"Handling field {node.FieldIndex}.");
                //generate the end of stream check
                ILGen.Ldloc(_Reader);
                ILGen.Callvirt(_AtEndOfStreamMethod);
                ILGen.Brtrue(_EndLabel);

                //declare the local and enable it in the scope of child visiting
                _FieldLocal = ILGen.DeclareLocal(node.Type);
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
                _InFieldAssignment = false;
            }
        }
        public override CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node)
        {
            if (!_InFieldAssignment) throw new InvalidOperationException("SkipAssignmentIfFlagSetNode must be in a FieldAssignmentNode");
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
            if (!_InFieldAssignment) throw new InvalidOperationException("SkipAssignmentIfMissingValueNode must be in a FieldAssignmentNode");
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
            ILGen.Callvirt(typeof(object).GetMethod("Equals", typeof(object)));
            ILGen.Brtrue(_SkipAssignmentLabel);
            return node;
        }
        public override CodeNode VisitAssignFieldToProperty(AssignFieldToPropertyNode node)
        {
            if (!_InFieldAssignment) throw new InvalidOperationException("AssignFieldToPropertyNode must be in a FieldAssignmentNode");
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
            ILGen.Callvirt(node.Property.GetSetMethod());
            return node;
        }
        public override CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node)
        {
            if (!_InFieldAssignment) throw new InvalidOperationException("SkipArrayAssignmentIfLengthIsZeroNode must be in a FieldAssignmentNode");
            Log($"Handling conditional assignment based zero length array.");
            //If the length field is zero, skip past assignment
            var lengthLocal = _FieldLocals[node.LengthIndex];
            ILGen.Ldloc(lengthLocal);
            ILGen.Brfalse(_SkipAssignmentLabel);
            return node;
        }
    }
}
