﻿// (c) Copyright Mark Miller.
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

        LocalBuilder _ConcreteRecordLocal;
        LocalBuilder _Reader;
        bool _InFieldAssignmentBlock = false;
        bool _InFieldAssignment = false;
        Label _EndLabel;
        Label _SkipAssignmentLabel;
        Dictionary<int, LocalBuilder> _FieldLocals = new Dictionary<int, LocalBuilder>();

        LocalBuilder _FieldLocal = null;

        public override CodeNode VisitInitializeRecord(InitializeRecordNode node)
        {
            _ConcreteRecordLocal = ILGen.DeclareLocal(ConcreteType);
            ILGen.Newobj(ConcreteType);
            ILGen.Stloc(_ConcreteRecordLocal);
            return node;
        }
        static MethodInfo _EnsureConvertibleToMethod = typeof(UnknownRecord).GetMethod("EnsureConvertibleTo", typeof(StdfRecord));
        public override CodeNode VisitEnsureCompat(EnsureCompatNode node)
        {
            ILGen.Ldarg_0();
            ILGen.Ldloc(_ConcreteRecordLocal);
            ILGen.Callvirt(_EnsureConvertibleToMethod);
            return node;
        }
#if SILVERLIGHT
            static MethodInfo _GetBinaryReaderForContentMethod = typeof(UnknownRecord).GetMethod("GetBinaryReaderForContent");
#else
        static MethodInfo _GetBinaryReaderForContentMethod = typeof(UnknownRecord).GetMethod("GetBinaryReaderForContent", BindingFlags.Instance | BindingFlags.Public);
#endif
        public override CodeNode VisitInitReaderNode(InitReaderNode node)
        {
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
        static MethodInfo _DisposeMethod = typeof(IDisposable).GetMethod("Dispose", new Type[0]);
        public override CodeNode VisitDisposeReader(DisposeReaderNode node)
        {
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
            ILGen.Ldloc(_ConcreteRecordLocal);
            ILGen.Ret();
            return node;
        }
        static MethodInfo _SkipRawMethod = typeof(BinaryReader).GetMethod("Skip", typeof(int));
        public override CodeNode VisitSkipRawBytes(SkipRawBytesNode node)
        {
            ILGen.Ldloc(_Reader);
            ILGen.Ldc_I4(node.Bytes);
            ILGen.Callvirt(_SkipRawMethod);
            return node;
        }
        Dictionary<Type, MethodInfo> _SkipTypeMethods = new Dictionary<Type, MethodInfo>();
        public override CodeNode VisitSkipType<T>(SkipTypeNode<T> node)
        {
            MethodInfo skipTypeMethod;
            var argsArray = typeof(T).IsArray ? new[] { typeof(int) } : new Type[0];
            if (node.IsNibble) skipTypeMethod = typeof(BinaryReader).GetMethod("SkipNibbleArray", argsArray);
            else if (!_SkipTypeMethods.TryGetValue(typeof(T), out skipTypeMethod))
            {
                string skipTypeMethodName;
                if (typeof(T) == typeof(byte)) skipTypeMethodName = "Skip1";
                else if (typeof(T) == typeof(byte[])) skipTypeMethodName = "Skip1Array";
                else if (typeof(T) == typeof(sbyte)) skipTypeMethodName = "Skip1";
                else if (typeof(T) == typeof(sbyte[])) skipTypeMethodName = "Skip1Array";
                else if (typeof(T) == typeof(ushort)) skipTypeMethodName = "Skip2";
                else if (typeof(T) == typeof(ushort[])) skipTypeMethodName = "Skip2Array";
                else if (typeof(T) == typeof(short)) skipTypeMethodName = "Skip2";
                else if (typeof(T) == typeof(short[])) skipTypeMethodName = "Skip2Array";
                else if (typeof(T) == typeof(uint)) skipTypeMethodName = "Skip4";
                else if (typeof(T) == typeof(uint[])) skipTypeMethodName = "Skip4Array";
                else if (typeof(T) == typeof(int)) skipTypeMethodName = "Skip4";
                else if (typeof(T) == typeof(int[])) skipTypeMethodName = "Skip4Array";
                else if (typeof(T) == typeof(ulong)) skipTypeMethodName = "Skip8";
                else if (typeof(T) == typeof(ulong[])) skipTypeMethodName = "Skip8Array";
                else if (typeof(T) == typeof(long)) skipTypeMethodName = "Skip8";
                else if (typeof(T) == typeof(long[])) skipTypeMethodName = "Skip8Array";
                else if (typeof(T) == typeof(float)) skipTypeMethodName = "Skip4";
                else if (typeof(T) == typeof(float[])) skipTypeMethodName = "Skip4Array";
                else if (typeof(T) == typeof(double)) skipTypeMethodName = "Skip8";
                else if (typeof(T) == typeof(double[])) skipTypeMethodName = "Skip8Array";
                else if (typeof(T) == typeof(string)) skipTypeMethodName = "SkipString";
                else if (typeof(T) == typeof(DateTime)) skipTypeMethodName = "Skip4";
                else if (typeof(T) == typeof(BitArray)) skipTypeMethodName = "SkipBitArray";
                else
                {
                    throw new NotSupportedException(string.Format(Resources.UnsupportedReaderType, typeof(T)));
                }
                skipTypeMethod = typeof(BinaryReader).GetMethod(skipTypeMethodName, argsArray);
                _SkipTypeMethods[typeof(T)] = skipTypeMethod;
            }
            ILGen.Ldloc(_Reader);
            //if we have a length index, load its local (we enforce its presence for arrays in the node)
            if (node.LengthIndex.HasValue)
            {
                ILGen.Ldloc(_FieldLocals[node.LengthIndex.Value]);
            }
            ILGen.Callvirt(skipTypeMethod);
            return node;
        }
        static MethodInfo _ReadFixedStringMethod = typeof(BinaryReader).GetMethod("ReadString", typeof(int));
        public override CodeNode VisitReadFixedString(ReadFixedStringNode node)
        {
            if (_FieldLocal == null) throw new InvalidOperationException("Cannot read string outside a FieldAssignmentNode");
            ILGen.Ldloc(_Reader);
            ILGen.Ldc_I4(node.Length);
            ILGen.Callvirt(_ReadFixedStringMethod);
            ILGen.Stloc(_FieldLocal);
            return node;
        }
        Dictionary<Type, MethodInfo> _ReadTypeMethods = new Dictionary<Type, MethodInfo>();
        public override CodeNode VisitReadType<T>(ReadTypeNode<T> node)
        {
            if (_FieldLocal == null) throw new InvalidOperationException("Cannot read string outside a FieldAssignmentNode");
            MethodInfo readTypeMethod;
            var argsArray = typeof(T).IsArray ? new[] { typeof(int) } : new Type[0];
            if (node.IsNibble) readTypeMethod = typeof(BinaryReader).GetMethod("ReadNibbleArray", argsArray);
            else if (!_ReadTypeMethods.TryGetValue(typeof(T), out readTypeMethod))
            {
                string readTypeMethodName;
                if (typeof(T) == typeof(byte)) readTypeMethodName = "ReadByte";
                else if (typeof(T) == typeof(byte[])) readTypeMethodName = "ReadByteArray";
                else if (typeof(T) == typeof(sbyte)) readTypeMethodName = "ReadSByte";
                else if (typeof(T) == typeof(sbyte[])) readTypeMethodName = "ReadSByteArray";
                else if (typeof(T) == typeof(ushort)) readTypeMethodName = "ReadUInt16";
                else if (typeof(T) == typeof(ushort[])) readTypeMethodName = "ReadUInt16Array";
                else if (typeof(T) == typeof(short)) readTypeMethodName = "ReadInt16";
                else if (typeof(T) == typeof(short[])) readTypeMethodName = "ReadInt16Array";
                else if (typeof(T) == typeof(uint)) readTypeMethodName = "ReadUInt32";
                else if (typeof(T) == typeof(uint[])) readTypeMethodName = "ReadUInt32Array";
                else if (typeof(T) == typeof(int)) readTypeMethodName = "ReadInt32";
                else if (typeof(T) == typeof(int[])) readTypeMethodName = "ReadInt32Array";
                else if (typeof(T) == typeof(ulong)) readTypeMethodName = "ReadUInt64";
                else if (typeof(T) == typeof(ulong[])) readTypeMethodName = "ReadUInt64Array";
                else if (typeof(T) == typeof(long)) readTypeMethodName = "ReadInt64";
                else if (typeof(T) == typeof(long[])) readTypeMethodName = "ReadInt64Array";
                else if (typeof(T) == typeof(float)) readTypeMethodName = "ReadSingle";
                else if (typeof(T) == typeof(float[])) readTypeMethodName = "ReadSingleArray";
                else if (typeof(T) == typeof(double)) readTypeMethodName = "ReadDouble";
                else if (typeof(T) == typeof(double[])) readTypeMethodName = "ReadDoubleArray";
                else if (typeof(T) == typeof(string)) readTypeMethodName = "ReadString";
                else if (typeof(T) == typeof(DateTime)) readTypeMethodName = "ReadDateTime";
                else if (typeof(T) == typeof(BitArray)) readTypeMethodName = "ReadBitArray";
                else
                {
                    throw new NotSupportedException(string.Format(Resources.UnsupportedReaderType, typeof(T)));
                }
                readTypeMethod = typeof(BinaryReader).GetMethod(readTypeMethodName, argsArray);
                _ReadTypeMethods[typeof(T)] = readTypeMethod;
            }
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
        static MethodInfo _AtEndOfStreamMethod = typeof(BinaryReader).GetProperty("AtEndOfStream").GetGetMethod();
        public override CodeNode VisitFieldAssignment<TField>(FieldAssignmentNode<TField> node)
        {
            //ensure we're in a FieldAssignmentBlock
            if (!_InFieldAssignmentBlock)
            {
                throw new InvalidOperationException("EndOfStreamCheckNode must occur within a FieldAssignmentBlockNode");
            }
            _InFieldAssignment = true;
            try
            {
                //generate the end of stream check
                ILGen.Ldloc(_Reader);
                ILGen.Callvirt(_AtEndOfStreamMethod);
                ILGen.Brtrue(_EndLabel);

                //declare the local and enable it in the scope of child visiting
                _FieldLocal = ILGen.DeclareLocal<TField>();
                _FieldLocals[node.FieldIndex] = _FieldLocal;

                _SkipAssignmentLabel = ILGen.DefineLabel();

                //visit any read node there is
                Visit(node.ReadNode);

                //visit any assignment block
                if (node.AssignmentBlock != null)
                {
                    Visit(node.AssignmentBlock);
                }
                //set the skip assignment label with a nop for fun.
                ILGen.Nop();
                ILGen.MarkLabel(_SkipAssignmentLabel);

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
        public override CodeNode VisitSkipAssignmentIfMissingValue<T>(SkipAssignmentIfMissingValueNode<T> node)
        {
            if (!_InFieldAssignment) throw new InvalidOperationException("SkipAssignmentIfMissingValueNode must be in a FieldAssignmentNode");
            ILGen.Ldloca(_FieldLocal);
            ILGen.Ldc<T>(node.MissingValue);
            if (_FieldLocal.LocalType.IsValueType)
            {
                ILGen.Box<T>();
            }
            ILGen.Constrained<T>();
            ILGen.Callvirt(typeof(object).GetMethod("Equals", typeof(object)));
            ILGen.Brtrue(_SkipAssignmentLabel);
            return node;
        }
        public override CodeNode VisitAssignFieldToProperty<T>(AssignFieldToPropertyNode<T> node)
        {
            if (!_InFieldAssignment) throw new InvalidOperationException("AssignFieldToPropertyNode must be in a FieldAssignmentNode");
            ILGen.Ldloc(_ConcreteRecordLocal);
            ILGen.Ldloc(_FieldLocal);
            //handle the case where the property is a nullable version of the field type
            if (typeof(T).IsValueType)
            {
                Type genericType = typeof(Nullable<>).MakeGenericType(typeof(T));
                if (node.Property.PropertyType == genericType)
                {
                    ILGen.Newobj(genericType, typeof(T));
                }
            }
            //assign the value to the property
            ILGen.Callvirt(node.Property.GetSetMethod());
            return node;
        }
        public override CodeNode VisitSkipArrayAssignmentIfLengthIsZero(SkipArrayAssignmentIfLengthIsZeroNode node)
        {
            if (!_InFieldAssignment) throw new InvalidOperationException("SkipArrayAssignmentIfLengthIsZeroNode must be in a FieldAssignmentNode");
            //If the length field is zero, skip past assignment
            var lengthLocal = _FieldLocals[node.LengthIndex];
            ILGen.Ldloc(lengthLocal);
            ILGen.Brfalse(_SkipAssignmentLabel);
            return node;
        }
    }
}