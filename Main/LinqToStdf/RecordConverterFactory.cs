// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToStdf {
    using Attributes;
    using CompiledQuerySupport;
    using LinqToStdf.Records;

    /// <summary>
    /// <para>
    /// Manages appropriate converter and unconverter delegates for various registered <see cref="RecordType">RecordTypes</see>.
    /// Is capable of registering converters directly, or building them dynamically from
    /// attribute metadata on record types.
    /// </para>
    /// <para>
    /// While <see cref="StdfFile"/> contains logic for record hopping and seeking, this is where the "meat" of the processing
    /// is implemented.  This class contains what amounts to a record parser compiler that uses attributes on the record
    /// classes to construct IL code capable of parsing the contents of STDF records.  Some records either are too complex or
    /// sufficiently different from standard records to be described by the attritibutes.  In these cases, converters/unconverters
    /// can be hand-written and registered with the converter factory.
    /// </para>
    /// </summary>
    /// <seealso cref="StdfFieldLayoutAttribute"/>
    public class RecordConverterFactory {

        /// <summary>
        /// Creates a new factory
        /// </summary>
        public RecordConverterFactory() : this((RecordsAndFields)null) { }

        internal RecordConverterFactory(RecordsAndFields recordsAndFields) {
            _RecordsAndFields = recordsAndFields;
            _Converters = new Dictionary<RecordType, Converter<UnknownRecord, StdfRecord>>();
            _Unconverters = new Dictionary<Type, Func<StdfRecord, Endian, UnknownRecord>>();
        }

        /// <summary>
        /// Creates a new factory that will reuse any registered converters and unconverters of another factory.
        /// </summary>
        /// <remarks>
        /// This is useful to eliminate the LCG and JIT hit for the dynamic converters and unconverters.
        /// </remarks>
        /// <param name="factoryToClone"></param>
        public RecordConverterFactory(RecordConverterFactory factoryToClone) {
            _Converters = new Dictionary<RecordType, Converter<UnknownRecord, StdfRecord>>(factoryToClone._Converters);
            _Unconverters = new Dictionary<Type, Func<StdfRecord, Endian, UnknownRecord>>(factoryToClone._Unconverters);
       }

        /// <summary>
        /// Internal storage for the converter delegates
        /// </summary>
        Dictionary<RecordType, Converter<UnknownRecord, StdfRecord>> _Converters;
        /// <summary>
        /// Internal storage for the unconverter delegates
        /// </summary>
        Dictionary<Type, Func<StdfRecord, Endian, UnknownRecord>> _Unconverters;

        RecordsAndFields _RecordsAndFields;

        /// <summary>
        /// If this is set to true, rather than use LCG, a dynamic assembly will
        /// be emitted and saved.  This is mostly to debug problems with the
        /// LCG infrastructure and/or the layout attributes.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Registers a <see cref="RecordType"/> and dynamically builds a converter from the metadata on <paramref name="type"/>
        /// </summary>
        /// <param name="recordType">The recordType to register.  This must match the <see cref="StdfRecord.RecordType"/>
        /// on <paramref name="type"/>.</param>
        /// <param name="type">The type that contains the metadata to build a converter.
        /// Must inherit from <see cref="StdfRecord"/></param>
        public void RegisterRecordType(RecordType recordType, Type type) {
            var converter = CreateConverterForType(type);
            var unconverter = CreateUnconverterForType(type);
            RegisterRecordConverter(recordType, converter);
            RegisterRecordUnconverter(type, unconverter);
        }

        /// <summary>
        /// Registers a converter for a <see cref="RecordType"/> directly.
        /// Useful for special-casing odd records that cannot be described with supported meta-data.
        /// </summary>
        /// <param name="recordType">The recordType to register.</param>
        /// <param name="converter">A converter delegate that will handle conversions</param>
        public void RegisterRecordConverter(RecordType recordType, Converter<UnknownRecord, StdfRecord> converter) {
            lock (_Converters) {
                _Converters.Add(recordType, converter);
            }
        }

        /// <summary>
        /// Registers an "unconverter" for a <see cref="RecordType"/> directly.
        /// Useful for special-casing odd records that cannot be described with supported meta-data.
        /// </summary>
        /// <param name="type">The type to be unconverted</param>
        /// <param name="unconverter">The "unconverter" delegate.</param>
        public void RegisterRecordUnconverter(Type type, Func<StdfRecord, Endian, UnknownRecord> unconverter) {
            lock (_Unconverters) {
                _Unconverters.Add(type, unconverter);
            }
        }

        /// <summary>
        /// Gets a converter for a given <see cref="RecordType"/>
        /// </summary>
        /// <param name="recordType">the record type to retrieve</param>
        /// <returns>If a converter for <paramref name="recordType"/> is register, the registered converter delegate,
        /// otherwise, a "null" converter that passes the unknown record through.</returns>
        public Converter<UnknownRecord, StdfRecord> GetConverter(RecordType recordType) {
            if (!_Converters.ContainsKey(recordType)) {
                return (r) => r;
            }
            else {
                return _Converters[recordType];
            }
        }

        /// <summary>
        /// Gets a unconverter for a given type
        /// </summary>
        /// <param name="type">type to be unconverted</param>
        /// <returns>If an "unconverter" for <paramref name="type"/> is register, the registered unconverter delegate,
        /// otherwise, an invalid operation exception is thrown.</returns>
        public Func<StdfRecord, Endian, UnknownRecord> GetUnconverter(Type type) {
            if (type == typeof(UnknownRecord)) {
                return (r, e) => {
                           var ur = (UnknownRecord)r;
                           if (ur.Endian != e) {
                               throw new InvalidOperationException(Resources.UnconvertEndianMismatch);
                           }
                           return ur;
                       };
            }
            else if (!_Unconverters.ContainsKey(type)) {
                throw new InvalidOperationException(string.Format(Resources.NoRegisteredUnconverter, type));
            }
            else {
                return _Unconverters[type];
            }
        }

        /// <summary>
        /// Converts an unknown record into a concrete record
        /// </summary>
        public StdfRecord Convert(UnknownRecord unknownRecord) {
            return GetConverter(unknownRecord.RecordType)(unknownRecord);
        }

        /// <summary>
        /// Converts a concrete record into an unknown record of the given endianness
        /// </summary>
        public UnknownRecord Unconvert(StdfRecord record, Endian endian) {
            return GetUnconverter(record.GetType())(record, endian);
        }

#if !SILVERLIGHT
        AssemblyBuilder _DynamicAssembly;
        ModuleBuilder _DynamicModule;

        void InitializeDynamicAssembly() {
            _DynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("DynamicConverters"),
                AssemblyBuilderAccess.RunAndSave);
            _DynamicModule = _DynamicAssembly.DefineDynamicModule(
                "DynamicConverters",
                "DynamicConverters.dll",
                true);
        }

        /// <summary>
        /// If <see cref="Debug"/> is set, saves a dynamic assembly
        /// with all the converters and unconverters created so far.
        /// </summary>
        public void SaveDynamicAssembly() {
            if (!Debug || _DynamicAssembly == null) throw new InvalidOperationException(Resources.InvalidSaveAssembly);
            _DynamicAssembly.Save("DynamicConverters.dll");
        }
#endif

        #region CreateConverterForType

        /// <summary>
        /// Generates a converter delegate from the attribute metadata on <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The destination record type. (must be assignable to <see cref="StdfRecord"/>)</param>
        /// <returns></returns>
        Converter<UnknownRecord, StdfRecord> CreateConverterForType(Type type) {
            if (!typeof(StdfRecord).IsAssignableFrom(type)) {
                throw new InvalidOperationException(string.Format(Resources.ConverterTargetNotStdfRecord, type));
            }
            Converter<UnknownRecord, StdfRecord> converter = null;
            //only bother creating a converter if we need to parse fields
            if (_RecordsAndFields == null || _RecordsAndFields.GetFieldsForType(type).Count > 0) {
                return (ur) => {
                    //there's a subtle race condition here, but unlikely to cause problems
                    //or actually be hit in normal scenarios.
                    if (converter == null) {
                        converter = LazyCreateConverterForType(type);
                    }
                    return converter(ur);
                };
            }
            else {
                return (ur) => new SkippedRecord(type);
            }
        }

        Converter<UnknownRecord, StdfRecord> LazyCreateConverterForType(Type type) {
            ILGenerator ilGenerator = null;
            Func<Converter<UnknownRecord, StdfRecord>> returner = null;
            if (Debug) {
#if SILVERLIGHT
                throw new NotSupportedException(Resources.NoDebugInSilverlight);
#else
                if (_DynamicAssembly == null) InitializeDynamicAssembly();
                var methodName = string.Format("ConvertTo{0}", type.Name);
                var dynamicType = _DynamicModule.DefineType(string.Format("{0}Converter", type.Name));
                var dynamicMethod = dynamicType.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    type,
                    new Type[] { typeof(UnknownRecord) });
                ilGenerator = dynamicMethod.GetILGenerator();
                returner = () => {
                               var newType = dynamicType.CreateType();
                               return (Converter<UnknownRecord, StdfRecord>)Delegate.CreateDelegate(
                                   typeof(Converter<UnknownRecord, StdfRecord>),
                                   newType.GetMethod(methodName));
                           };
#endif
            }
            else {
                var converter = new DynamicMethod(
                    string.Format("ConvertTo{0}", type.Name),
                    type,
                    new Type[] { typeof(UnknownRecord) }
#if !SILVERLIGHT
                    ,true //skip visibility for desktop
#endif
                    );
                ilGenerator = converter.GetILGenerator();
                returner = () => (Converter<UnknownRecord, StdfRecord>)converter.CreateDelegate(typeof(Converter<UnknownRecord, StdfRecord>));
            }
            var generator = new ConverterGenerator(ilGenerator, type, _RecordsAndFields == null ? null : _RecordsAndFields.GetFieldsForType(type));
            generator.GenerateConverter();
            return returner();
        }

        #endregion

        #region CreateUnconverterForType

        Func<StdfRecord, Endian, UnknownRecord> CreateUnconverterForType(Type type) {
            if (!typeof(StdfRecord).IsAssignableFrom(type)) {
                throw new InvalidOperationException(string.Format(Resources.ConverterTargetNotStdfRecord, type));
            }
            Func<StdfRecord, Endian, UnknownRecord> unconverter = null;
            return (r, e) => {
                if (unconverter == null) {
                    unconverter = LazyCreateUnconverterForType(type);
                }
                return unconverter(r, e);
            };
        }

        Func<StdfRecord, Endian, UnknownRecord> LazyCreateUnconverterForType(Type type) {
            ILGenerator ilGenerator = null;
            Func<Func<StdfRecord,Endian, UnknownRecord>> returner = null;
            if (Debug) {
#if SILVERLIGHT
                throw new NotSupportedException(Resources.NoDebugInSilverlight);
#else
                if (_DynamicAssembly == null) InitializeDynamicAssembly();
                var methodName = string.Format("UnconvertFrom{0}", type.Name);
                var dynamicType = _DynamicModule.DefineType(string.Format("{0}Unconverter", type.Name));
                var dynamicMethod = dynamicType.DefineMethod(
                    methodName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    typeof(UnknownRecord),
                    new Type[] { typeof(StdfRecord), typeof(Endian) });
                ilGenerator = dynamicMethod.GetILGenerator();
                returner = () => {
                               var newType = dynamicType.CreateType();
                               return (Func<StdfRecord, Endian, UnknownRecord>)Delegate.CreateDelegate(
                                   typeof(Func<StdfRecord, Endian, UnknownRecord>),
                                   newType.GetMethod(methodName));
                           };
#endif
            }
            else {
                var unconverter = new DynamicMethod(
                                    string.Format("UnconvertFrom{0}", type.Name),
                                    typeof(UnknownRecord),
                                    new Type[] { typeof(StdfRecord), typeof(Endian) },
                                    typeof(UnknownRecord),
                                    false);
                ilGenerator = unconverter.GetILGenerator();
                returner = () => (Func<StdfRecord, Endian, UnknownRecord>)unconverter.CreateDelegate(typeof(Func<StdfRecord, Endian, UnknownRecord>));
            }
            var generator = new UnconverterGenerator(ilGenerator, type);
            generator.GenerateUnconverter();
            return returner();
        }

        #endregion

        #region CodeNodes
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
            public virtual CodeNode VisitSkipType<T>(SkipTypeNode<T> node)
            {
                return node;
            }
            public virtual CodeNode VisitReadFixedString(ReadFixedStringNode node)
            {
                return node;
            }
            public virtual CodeNode VisitReadType<T>(ReadTypeNode<T> node)
            {
                return node;
            }
            public virtual CodeNode VisitFieldAssignment<T>(FieldAssignmentNode<T> node)
            {
                var visitedReadNode = Visit(node.ReadNode);
                var visitedConditionalsBlock = Visit(node.AssignmentBlock);
                if (visitedReadNode == node.ReadNode && visitedConditionalsBlock == node.AssignmentBlock) return node;
                else return new FieldAssignmentNode<T>(node.FieldIndex, visitedReadNode, visitedConditionalsBlock as BlockNode ?? new BlockNode(visitedConditionalsBlock));
            }
            public virtual CodeNode VisitSkipAssignmentIfFlagSet(SkipAssignmentIfFlagSetNode node)
            {
                return node;
            }
            public virtual CodeNode VisitSkipAssignmentIfMissingValue<T>(SkipAssignmentIfMissingValueNode<T> node)
            {
                return node;
            }
            public virtual CodeNode VisitAssignFieldToProperty<T>(AssignFieldToPropertyNode<T> node)
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
            static MethodInfo _GetBinaryReaderForContentMethod = typeof(UnknownRecord).GetMethod("GetBinaryReaderForContent", BindingFlags.Instance | BindingFlags.NonPublic);
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
        class UnconverterEmittingVisitor : CodeNodeVisitor
        {
            public ILGenerator ILGen;
            public Type ConcreteType;

            LocalBuilder _ConcreteRecordLocal;
            LocalBuilder _StartedWriting;
            LocalBuilder _Writer;
            Dictionary<int, LocalBuilder> _FieldLocals = new Dictionary<int, LocalBuilder>();

            public override CodeNode VisitUnconverterShell(UnconverterShellNode node)
            {
                //initialize the concrete record and cast the arg to it
                _ConcreteRecordLocal = ILGen.DeclareLocal(ConcreteType);
                ILGen.Ldarg_0();
                ILGen.Castclass(ConcreteType);
                ILGen.Stloc(_ConcreteRecordLocal);

                //initialize _StartedWriting
                _StartedWriting = ILGen.DeclareLocal<bool>();
                ILGen.Ldc_I4_0();
                ILGen.Stloc(_StartedWriting);

                //create a memory stream for writing
                LocalBuilder memoryStream = ILGen.DeclareLocal<MemoryStream>();
                ILGen.Newobj<MemoryStream>();
                ILGen.Stloc(memoryStream);
                ILGen.BeginExceptionBlock();

                //create a binary writer
                _Writer = ILGen.DeclareLocal<BinaryWriter>();
                //load args for binary writer .ctor
                ILGen.Ldloc(memoryStream);
                ILGen.Ldarg_1(); //the endianness
                ILGen.Ldc_I4_1(); //true for writing backwards
                ILGen.Newobj<BinaryWriter>(typeof(MemoryStream), typeof(Endian), typeof(bool));
                ILGen.Stloc(_Writer);
                //nothing on the stack

                //visit the children and let them emit
                Visit(node.Block);

                //at this point, the memory stream should have all the bytes for the content in it, but backwards

                //get the content
                ILGen.Ldloc(memoryStream);
                ILGen.Callvirt(typeof(MemoryStream).GetMethod("ToArray"));
                var content = ILGen.DeclareLocal<byte[]>();
                ILGen.Stloc(content);
                ILGen.Ldloc(content);
                ILGen.Callvirt(typeof(Array).GetMethod("Reverse", typeof(Array)));

                ILGen.BeginFinallyBlock();
                //dispose the memorystream
                ILGen.Ldloc(memoryStream);
                ILGen.Callvirt(typeof(IDisposable).GetMethod("Dispose"));
                ILGen.EndExceptionBlock();

                //get the record type
                ILGen.Ldarg_0();
                ILGen.Callvirt(typeof(StdfRecord).GetProperty("RecordType").GetGetMethod());

                //load the content
                ILGen.Ldloc(content);

                //get the endian
                ILGen.Ldarg_1();

                //new up a new UnknownRecord!
                ILGen.Newobj<UnknownRecord>(typeof(RecordType), typeof(byte[]), typeof(Endian));

                ILGen.Ret();

                return node;
            }
            public override CodeNode VisitCreateFieldLocalForWriting(CreateFieldLocalForWritingNode node)
            {
                if (_FieldLocals.ContainsKey(node.FieldIndex)) throw new InvalidOperationException(string.Format("Local for index {0} has already been created", node.FieldIndex));
                var local = ILGen.DeclareLocal(node.LocalType);
                _FieldLocals.Add(node.FieldIndex, local);
                if (node.LocalType.GetConstructor(new Type[0]) != null)
                {
                    ILGen.Newobj(local.LocalType);
                    ILGen.Stloc(local);
                }
                else if (local.LocalType.IsValueType)
                {
                    ILGen.Ldloca(local);
                    ILGen.Initobj(local.LocalType);
                }
                else
                {
                    ILGen.Ldnull();
                    ILGen.Stloc(local);
                }
                return node;
            }
            public override CodeNode VisitWriteField(WriteFieldNode node)
            {
                //TODO: do the right kind of checks for the optional node properties

                //do any initialization (this is typically creating field locals)
                Visit(node.Initialization);

                //initialize the local that indicates whether we have a local value to write
                var hasValueLocal = ILGen.DeclareLocal<bool>();
                ILGen.Ldc_I4_0();
                ILGen.Stloc(hasValueLocal);

                //declare a bunch of labels
                var decideLabel = ILGen.DefineLabel();
                var skipWritingLabel = ILGen.DefineLabel();
                var doWriteLabel = ILGen.DefineLabel();

                //if we have a property, we need to read from it
                if (node.Property != null)
                {
                    //get the field local
                    var fieldLocal = _FieldLocals[node.FieldIndex];
                    if (fieldLocal.LocalType != node.FieldType) throw new InvalidOperationException("Field assignment is occuring on a mismatched field local type.");

                    //get the value of the property
                    ILGen.Ldloc(_ConcreteRecordLocal);
                    ILGen.Callvirt(node.Property.GetGetMethod());

                    //generate the check for whether we have a value to write
                    if (node.Property.PropertyType.IsValueType)
                    {
                        //it's a value type.  Check for Nullable
                        Type nullable = typeof(Nullable<>).MakeGenericType(node.FieldType);
                        if (node.Property.PropertyType == nullable)
                        {
                            //it is nullable, check to see whether we have a value
                            //create a local for the nullable and store the value in it
                            var nullableLocal = ILGen.DeclareLocal(nullable);
                            ILGen.Stloc(nullableLocal);
                            //call .HasValue
                            ILGen.Ldloca(nullableLocal); //load address so we can call methods
                            ILGen.Callvirt(nullable.GetProperty("HasValue").GetGetMethod());
                            //dup and store hasValue
                            ILGen.Dup();
                            ILGen.Stloc(hasValueLocal);

                            //if we don't have a value, branch to decide if we should be writing something anyway
                            ILGen.Brfalse(decideLabel);

                            //otherwise, get the value 
                            ILGen.Ldloca(nullableLocal);
                            ILGen.Callvirt(nullable.GetProperty("Value").GetGetMethod());
                        }
                        //store the value and branch to the write
                        ILGen.Stloc(fieldLocal);
                        ILGen.Br(doWriteLabel);
                    }
                    else
                    {
                        //copy so we can store (remember we're dup'ing the thing returned from the property)
                        ILGen.Dup();
                        ILGen.Stloc(fieldLocal);

                        //it's a ref type, check for null (more complicated than I thought)
                        ILGen.Ldnull(); //load null
                        ILGen.Ceq(); //compare to null
                        //0 on stack if has value (it didn't equal null)
                        //now compare with 0
                        ILGen.Ldc_I4_0();
                        ILGen.Ceq(); //compare to 0
                        //1 on stack if has value
                        //dup and store as hasValue
                        ILGen.Dup();
                        ILGen.Stloc(hasValueLocal);

                        //if we don't have a value, branch to decide if we should be writing something anyway
                        ILGen.Brfalse(decideLabel);

                        //branch to the write
                        ILGen.Br(doWriteLabel);
                    }
                }

                ILGen.MarkLabel(decideLabel);
                //at this point, we don't have a value from the record directly,
                //but we may need to write if we have started already
                ILGen.Ldloc(_StartedWriting);
                //we can skip writing if we haven't started already
                ILGen.Brfalse(skipWritingLabel);

                //emit contingency for no value
                if (node.NoValueWriteContingency != null) Visit(node.NoValueWriteContingency);

                ILGen.Br(skipWritingLabel);

                //this is where we will start doing real writes
                ILGen.MarkLabel(doWriteLabel);
                //we've started writing, so set that
                ILGen.Ldc_I4_1();
                ILGen.Stloc(_StartedWriting);

                Visit(node.WriteOperation);

                ILGen.MarkLabel(skipWritingLabel);

                //if we have an optional field index, we need to emit code to set it
                if (node.OptionalFieldIndex.HasValue)
                {
                    var optionalLocal = _FieldLocals[node.OptionalFieldIndex.Value];
                    var skipOptField = ILGen.DefineLabel();
                    //if we have a value, skip setting the optional field
                    ILGen.Ldloc(hasValueLocal);
                    ILGen.Brtrue(skipOptField);
                    //load the optional local and the field mask, and "or" them together
                    ILGen.Ldloc(optionalLocal);
                    ILGen.Ldc_I4_S(node.OptionaFieldMask);
                    ILGen.Or();
                    //store the value back in the local 
                    ILGen.Stloc(optionalLocal);
                    ILGen.MarkLabel(skipOptField);
                }

                return node;
            }
            public override CodeNode VisitWriteFixedString(WriteFixedStringNode node)
            {
                ILGen.Ldloc(_Writer);
                Visit(node.ValueSource);
                ILGen.Ldc_I4(node.StringLength);
                ILGen.Callvirt(typeof(BinaryWriter).GetMethod("WriteString", typeof(string), typeof(int)));
                return node;
            }
            static Dictionary<Type, MethodInfo> _WriteMethods = new Dictionary<Type, MethodInfo>();
            public override CodeNode VisitWriteType(WriteTypeNode node)
            {
                MethodInfo writeMethod;
                if (!_WriteMethods.TryGetValue(node.Type, out writeMethod))
                {
                    string writeMethodName;
                    if (node.Type == typeof(byte)) writeMethodName = "WriteByte";
                    else if (node.Type == typeof(byte[])) writeMethodName = "WriteByteArray";
                    else if (node.Type == typeof(sbyte)) writeMethodName = "WriteSByte";
                    else if (node.Type == typeof(sbyte[])) writeMethodName = "WriteSByteArray";
                    else if (node.Type == typeof(ushort)) writeMethodName = "WriteUInt16";
                    else if (node.Type == typeof(ushort[])) writeMethodName = "WriteUInt16Array";
                    else if (node.Type == typeof(short)) writeMethodName = "WriteInt16";
                    else if (node.Type == typeof(short[])) writeMethodName = "WriteInt16Array";
                    else if (node.Type == typeof(uint)) writeMethodName = "WriteUInt32";
                    else if (node.Type == typeof(uint[])) writeMethodName = "WriteUInt32Array";
                    else if (node.Type == typeof(int)) writeMethodName = "WriteInt32";
                    else if (node.Type == typeof(int[])) writeMethodName = "WriteInt32Array";
                    else if (node.Type == typeof(ulong)) writeMethodName = "WriteUInt64";
                    else if (node.Type == typeof(ulong[])) writeMethodName = "WriteUInt64Array";
                    else if (node.Type == typeof(long)) writeMethodName = "WriteInt64";
                    else if (node.Type == typeof(long[])) writeMethodName = "WriteInt64Array";
                    else if (node.Type == typeof(float)) writeMethodName = "WriteSingle";
                    else if (node.Type == typeof(float[])) writeMethodName = "WriteSingleArray";
                    else if (node.Type == typeof(double)) writeMethodName = "WriteDouble";
                    else if (node.Type == typeof(double[])) writeMethodName = "WriteDoubleArray";
                    else if (node.Type == typeof(string)) writeMethodName = "WriteString";
                    else if (node.Type == typeof(DateTime)) writeMethodName = "WriteDateTime";
                    else if (node.Type == typeof(BitArray)) writeMethodName = "WriteBitArray";
                    else
                    {
                        throw new NotSupportedException(string.Format(Resources.UnsupportedWriterType, node.Type));
                    }
                    writeMethod = typeof(BinaryWriter).GetMethod(writeMethodName, node.Type);
                    _WriteMethods[node.Type] = writeMethod;
                }
                ILGen.Ldloc(_Writer);
                Visit(node.ValueSource);
                ILGen.Callvirt(writeMethod);
                return node;
            }
            public override CodeNode VisitLoadMissingValue(LoadMissingValueNode node)
            {
                ILGen.Ldc(node.MissingValue, node.Type);
                return node;
            }
            public override CodeNode VisitLoadFieldLocal(LoadFieldLocalNode node)
            {
                ILGen.Ldloc(_FieldLocals[node.FieldIndex]);
                return node;
            }
            public override CodeNode VisitLoadNull(LoadNullNode node)
            {
                ILGen.Ldnull();
                return node;
            }
            public override CodeNode VisitThrowInvalidOperation(ThrowInvalidOperationNode node)
            {
                ILGen.Ldstr(node.Message);
                ILGen.Newobj<InvalidOperationException>(typeof(string));
                ILGen.Throw();
                return node;
            }
            public override CodeNode VisitValidateSharedLengthLocal(ValidateSharedLengthLocalNode node)
            {
                var arrayLocal = _FieldLocals[node.ArrayFieldIndex];
                var lengthLocal = _FieldLocals[node.LengthFieldIndex];

                //someone else created the length, we need to verify they are equal
                var dontThrow = ILGen.DefineLabel();
                var notNull = ILGen.DefineLabel();
                var compareLength = ILGen.DefineLabel();
                ILGen.Ldloc(arrayLocal);
                //if the array is null, goto notNulls
                ILGen.Ldnull();
                ILGen.Ceq();
                ILGen.Brfalse(notNull);
                //it is null, treat the length as 0 and compare the length
                ILGen.Ldc_I4_0();
                ILGen.Br(compareLength);

                //array is not null. get its real length
                ILGen.MarkLabel(notNull);
                ILGen.Ldloc(arrayLocal);
                ILGen.Ldlen();

                ILGen.MarkLabel(compareLength);
                //FYI, we don't need a special case for nibble since we represent it as a byte array
                if (lengthLocal.LocalType == typeof(ushort))
                {
                    ILGen.Conv_U2();
                }
                else if (lengthLocal.LocalType == typeof(byte))
                {
                    ILGen.Conv_I4();
                }
                else throw new NotSupportedException(string.Format(Resources.UnsupportedArrayLengthType, lengthLocal.LocalType));
                ILGen.Ldloc(lengthLocal);
                ILGen.Ceq();
                ILGen.Brtrue(dontThrow);
                ILGen.Ldstr(string.Format(Resources.SharedLengthViolation, node.LengthFieldIndex));
                ILGen.Newobj<InvalidOperationException>(typeof(string));
                ILGen.Throw();
                ILGen.MarkLabel(dontThrow);
                return node;
            }
            public override CodeNode VisitSetLengthLocal(SetLengthLocalNode node)
            {
                var arrayFieldLocal = _FieldLocals[node.ArrayFieldIndex];
                var lengthFieldLocal = _FieldLocals[node.LengthFieldIndex];

                //TODO: this code is duplicated in VisitValidateSharedLengthLocal
                var notNull = ILGen.DefineLabel();
                var storeLength = ILGen.DefineLabel();

                //if the array is not null, goto notNull
                ILGen.Ldloc(arrayFieldLocal);
                ILGen.Ldnull();
                ILGen.Ceq();
                ILGen.Brfalse(notNull);

                //it is null, treat the length as 0 and compare the length
                ILGen.Ldc_I4_0();
                ILGen.Br(storeLength);

                //array is not null. get its real length
                ILGen.MarkLabel(notNull);
                ILGen.Ldloc(arrayFieldLocal);
                ILGen.Ldlen();
                ILGen.Br(storeLength);

                //store the length
                ILGen.MarkLabel(storeLength);
                ILGen.Stloc(lengthFieldLocal);
                return node;
            }
        }
        class PrettyPrintVisitor : CodeNodeVisitor
        {
            StringWriter _Output;
            System.Xml.XmlTextWriter _Writer;
            public PrettyPrintVisitor(CodeNode node)
            {
                _Output = new StringWriter();
                _Writer = new System.Xml.XmlTextWriter(_Output)
                {
                    Formatting = System.Xml.Formatting.Indented,
                };
                Visit(node);
            }
            public override CodeNode VisitAssignFieldToProperty<T>(AssignFieldToPropertyNode<T> node)
            {
                _Writer.WriteStartElement("AssignFieldToProperty");
                _Writer.WriteAttributeString("Type", typeof(T).ToString());
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
            public override CodeNode VisitFieldAssignment<T>(FieldAssignmentNode<T> node)
            {
                _Writer.WriteStartElement("FieldAssignment");
                _Writer.WriteAttributeString("Type", typeof(T).ToString());
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
            public override CodeNode VisitReadType<T>(ReadTypeNode<T> node)
            {
                _Writer.WriteStartElement("ReadType");
                _Writer.WriteAttributeString("IsNibble", node.IsNibble.ToString());
                _Writer.WriteAttributeString("LengthIndex", node.LengthIndex.ToString());
                _Writer.WriteAttributeString("Type", typeof(T).ToString());
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
            public override CodeNode VisitSkipAssignmentIfMissingValue<T>(SkipAssignmentIfMissingValueNode<T> node)
            {
                _Writer.WriteStartElement("SkipAssignmentIfMissingValue");
                _Writer.WriteAttributeString("MissingValue", node.MissingValue.ToString());
                _Writer.WriteAttributeString("Type", typeof(T).ToString());
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
            public override CodeNode VisitSkipType<T>(SkipTypeNode<T> node)
            {
                _Writer.WriteStartElement("SkipType");
                _Writer.WriteAttributeString("LengthIndex", node.LengthIndex.ToString());
                _Writer.WriteAttributeString("IsNibble", node.IsNibble.ToString());
                _Writer.WriteAttributeString("Type", typeof(T).ToString());
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
        abstract class CodeNode
        {
            public abstract CodeNode Accept(CodeNodeVisitor visitor);
        }
        class InitializeRecordNode : CodeNode
        {
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitInitializeRecord(this);
            }
        }
        class EnsureCompatNode : CodeNode
        {
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitEnsureCompat(this);
            }
        }
        class InitReaderNode : CodeNode
        {
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitInitReaderNode(this);
            }
        }
        class TryFinallyNode : CodeNode
        {
            public TryFinallyNode(CodeNode tryNode, CodeNode finallyNode)
            {
                TryNode = tryNode;
                FinallyNode = finallyNode;
            }
            public CodeNode TryNode { get; private set; }
            public CodeNode FinallyNode { get; private set; }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitTryFinallyNode(this);
            }
        }
        class DisposeReaderNode : CodeNode
        {
            static MethodInfo _DisposeMethod = typeof(IDisposable).GetMethod("Dispose", new Type[0]);
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitDisposeReader(this);
            }
        }
        class BlockNode : CodeNode
        {
            public BlockNode(params CodeNode[] nodes) : this((IEnumerable<CodeNode>)nodes) { }
            public BlockNode(IEnumerable<CodeNode> nodes)
            {
                Nodes = nodes.ToList();
            }
            public List<CodeNode> Nodes { get; private set; }

            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitBlock(this);
            }
        }
        class FieldAssignmentBlockNode : CodeNode
        {
            public BlockNode Block { get; private set; }
            public FieldAssignmentBlockNode(BlockNode node)
            {
                Block = node;
            }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitFieldAssignmentBlock(this);
            }
        }
        class ReturnRecordNode : CodeNode
        {
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitReturnRecord(this);
            }
        }
        class SkipRawBytesNode : CodeNode
        {
            static MethodInfo _SkipMethod = typeof(BinaryReader).GetMethod("Skip", typeof(int));
            public int Bytes { get; private set; }
            public SkipRawBytesNode(int bytes)
            {
                Bytes = bytes;
            }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitSkipRawBytes(this);
            }

        }
        class SkipTypeNode<T> : CodeNode
        {
            public int? LengthIndex { get; private set; }
            public bool IsNibble { get; private set; }
            public SkipTypeNode() {
                if (typeof(T).IsArray)
                {
                    throw new InvalidOperationException("SkipTypeNode on an array type must be constructed with a length index.");
                }
            }
            public SkipTypeNode(int lengthIndex, bool isNibble = false)
            {
                if (!typeof(T).IsArray)
                {
                    throw new InvalidOperationException("SkipTypeNode on an non-array type can't be constructed with a length index.");
                }
                LengthIndex = lengthIndex;
                if (isNibble && typeof(T) != typeof(byte)) throw new InvalidOperationException("Nibble arrays can only be read into byte arrays.");
                IsNibble = isNibble;
            }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitSkipType(this);
            }
        }
        class ReadFixedStringNode : CodeNode
        {
            public int Length { get; private set; }
            public ReadFixedStringNode(int length)
            {
                Length = length;
            }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitReadFixedString(this);
            }
        }
        class ReadTypeNode<T> : CodeNode
        {
            public int? LengthIndex { get; private set; }
            public bool IsNibble { get; private set; }
            public ReadTypeNode() {
                if (typeof(T).IsArray)
                {
                    throw new InvalidOperationException("ReadTypeNode on an array type must be constructed with a length index.");
                }
            }
            public ReadTypeNode(int lengthIndex, bool isNibble = false)
            {
                if (!typeof(T).IsArray)
                {
                    throw new InvalidOperationException("ReadTypeNode on an non-array type can't be constructed with a length index.");
                }
                LengthIndex = lengthIndex;
                if (isNibble && typeof(T) != typeof(byte[])) throw new InvalidOperationException("Nibble arrays can only be read into byte arrays.");
                IsNibble = isNibble;
            }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitReadType(this);
            }
        }
        class FieldAssignmentNode<T> : CodeNode
        {
            public FieldAssignmentNode(int index, CodeNode readNode, BlockNode assignmentBlock)
            {
                FieldIndex = index;
                ReadNode = readNode;
                AssignmentBlock = assignmentBlock;
            }
            public int FieldIndex { get; private set; }
            public CodeNode ReadNode { get; private set; }
            public BlockNode AssignmentBlock { get; private set; }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitFieldAssignment(this);
            }
        }
        class SkipAssignmentIfFlagSetNode : CodeNode
        {
            public SkipAssignmentIfFlagSetNode(int flagFieldIndex, byte flagMask)
            {
                FlagFieldIndex = flagFieldIndex;
                FlagMask = flagMask;
            }
            public int FlagFieldIndex { get; private set; }
            public byte FlagMask { get; private set; }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitSkipAssignmentIfFlagSet(this);
            }
        }
        class SkipAssignmentIfMissingValueNode<T> : CodeNode
        {
            public SkipAssignmentIfMissingValueNode(T missingValue)
            {
                MissingValue = missingValue;
            }
            public T MissingValue { get; private set; }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitSkipAssignmentIfMissingValue(this);
            }
        }
        class AssignFieldToPropertyNode<T> : CodeNode
        {
            public AssignFieldToPropertyNode(PropertyInfo property)
            {
                Property = property;
            }
            public PropertyInfo Property { get; private set; }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitAssignFieldToProperty(this);
            }
        }
        class SkipArrayAssignmentIfLengthIsZeroNode : CodeNode
        {
            public int LengthIndex { get; private set; }
            public SkipArrayAssignmentIfLengthIsZeroNode(int lengthIndex)
            {
                LengthIndex = lengthIndex;
            }

            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitSkipArrayAssignmentIfLengthIsZero(this);
            }
        }
        class ThrowInvalidOperationNode : CodeNode
        {
            public string Message {get;private set;}
            public ThrowInvalidOperationNode(string message)
            {
                Message = message;
            }
            public override CodeNode Accept(CodeNodeVisitor visitor)
            {
                return visitor.VisitThrowInvalidOperation(this);
            }
        }

        //unconverter nodes
        //TODO: how to keep these separate?
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
            public int FieldIndex {get;private set;}
            public Type LocalType {get;private set;}
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
            public int ArrayFieldIndex {get;private set;}
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
        #endregion

        /// <summary>
        /// This helper class encapsulates the generation of IL for the converters
        /// </summary>
        class ConverterGenerator {
            ILGenerator _ILGen;
            Type _Type;
            HashSet<string> _Fields;

            /// <summary>
            /// Constructs a converter using the supplied il generator and the type we're converting to.
            /// </summary>
            /// <param name="ilgen">The il generator to use</param>
            /// <param name="type">The type we're converting to</param>
            /// <param name="fields">The fields we should parse (null if we should parse everything, empty if we shouldn't parse at all)</param>
            public ConverterGenerator(ILGenerator ilgen, Type type, HashSet<string> fields) {
                if (ilgen == null) throw new ArgumentNullException("ilgen");
                if (type == null) throw new ArgumentNullException("type");
                _ILGen = ilgen;
                _Type = type;
                _Fields = fields;
            }

            bool ShouldParseField(string field) {
                return _Fields == null ? true : _Fields.Contains(field);
            }

            /// <summary>
            /// Does the work of generating the appropriate code.
            /// </summary>
            internal void GenerateConverter()
            {
                List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> fields = GetFieldLayoutsAndAssignments(_Type);
                //pick up any fields that are required to parse the fields we need
                if (_Fields != null)
                {
                    foreach (var pair in fields)
                    {
                        //if it's an assigned field that we are parsing
                        if (pair.Value != null && _Fields.Contains(pair.Value.Name))
                        {
                            //if it's an optional field
                            var op = pair.Key as StdfOptionalFieldLayoutAttribute;
                            if (op != null)
                            {
                                //if the flag index is an assigned field, add it to our list of parsed fields
                                var prop = fields[op.FlagIndex].Value;
                                if (prop != null) _Fields.Add(prop.Name);
                            }
                            else
                            {
                                var dep = pair.Key as StdfDependencyProperty;
                                if (dep != null)
                                {
                                    var prop = fields[dep.DependentOnIndex].Value;
                                    if (prop != null) _Fields.Add(prop.Name);
                                }

                            }
                        }
                    }
                }

                //generate the assignment nodes
                var assignments = from pair in fields
                                  //don't generate code for dependency properties
                                  where !(pair.Key is StdfDependencyProperty)
                                  //this call through reflection is icky, but marginally better than the hard-coded table
                                  //we're just binding to the generic GenerateAssignment method for the field's type
                                  let callInfo = typeof(ConverterGenerator).GetMethod("GenerateAssignment", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(pair.Key.FieldType)
                                  select (CodeNode)callInfo.Invoke(this, new object[] { pair });

                //add the end label to the end of the assignments
                var assignmentBlock = new FieldAssignmentBlockNode(new BlockNode(assignments));

                //This is the list of nodes to emit to create the converter
                var block = new BlockNode(
                    new InitializeRecordNode(),
                    new EnsureCompatNode(),
                    new InitReaderNode(),
                    //TODO: Replace TryFinally with something more semantic like BinaryReaderScopeBlock
                    new TryFinallyNode(
                        assignmentBlock,
                        new DisposeReaderNode()),
                    new ReturnRecordNode());

                //visit the block with an emitting visitor
                new ConverterEmittingVisitor()
                {
                    ILGen = _ILGen,
                    ConcreteType = _Type,
                }.Visit(block);
            }

            static List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> GetFieldLayoutsAndAssignments(Type recordType) {
                //get the list
                var attributes = from a in ((Type)recordType).GetCustomAttributes(typeof(StdfFieldLayoutAttribute), true).Cast<StdfFieldLayoutAttribute>()
                                 orderby a.FieldIndex
                                 select a;
                var list = new List<StdfFieldLayoutAttribute>(attributes);
                //make sure they are consecutive
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].FieldIndex != i) throw new NonconsecutiveFieldIndexException(recordType);
                }
                var withPropInfo = from l in list
                                   select new KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>(
                                              l,
                                              (l.AssignTo == null) ? null : ((Type)recordType).GetProperty(l.AssignTo));

                return new List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>>(withPropInfo);
            }

            CodeNode GenerateAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
                //if this is an array, defer to GenerateArrayAssignment
                if (pair.Key is StdfArrayLayoutAttribute)
                {
                    if (typeof(T) == typeof(string)) {
                        throw new InvalidOperationException(Resources.NoStringArrays);
                    }
                    return GenerateArrayAssignment<T>(pair);
                }

                //get the length if this is a fixed-length string
                StdfStringLayoutAttribute stringLayout = pair.Key as StdfStringLayoutAttribute;
                var stringLength = -1;
                if (stringLayout != null && stringLayout.Length > 0) {
                    stringLength = stringLayout.Length;
                }

                //just skip this field if we have an assignment, but shouldn't be parsing it
                if (pair.Value != null && !ShouldParseField(pair.Value.Name)) {
                    if (stringLength > 0) {
                        return new SkipRawBytesNode(stringLength);
                    }
                    else {
                        return new SkipTypeNode<T>();
                    }
                }

                //determine how we'll read the field
                CodeNode readerNode;
                if (stringLength > 0) {
                    readerNode = new ReadFixedStringNode(stringLength);
                }
                else {
                    readerNode = new ReadTypeNode<T>();
                }

                BlockNode assignmentBlock = null;
                //if we have a property to assign to, generate the appropriate assignment statements
                if (pair.Value != null) {
                    var assignmentNodes = new List<CodeNode>();
                    //if this is optional, set us up to skip if the missing flag is set
                    StdfOptionalFieldLayoutAttribute optionalLayout = pair.Key as StdfOptionalFieldLayoutAttribute;
                    if (optionalLayout != null) {
                        assignmentNodes.Add(new SkipAssignmentIfFlagSetNode(optionalLayout.FlagIndex, optionalLayout.FlagMask));
                    }
                    //if we have a missing value, set us up to skip if the value matches the missing value
                    else if (pair.Key.MissingValue != null) {
                        if (!(pair.Key.MissingValue is T)) throw new InvalidOperationException(string.Format("Missing value {0} is not a {1}.", pair.Key.MissingValue, typeof(T)));
                        assignmentNodes.Add(new SkipAssignmentIfMissingValueNode<T>((T)pair.Key.MissingValue));
                    }
                    //set us up to assign to the property
                    assignmentNodes.Add(new AssignFieldToPropertyNode<T>(pair.Value));
                    assignmentBlock = new BlockNode(assignmentNodes);
                }
                return new FieldAssignmentNode<T>(pair.Key.FieldIndex, readerNode, assignmentBlock);
            }

            CodeNode GenerateArrayAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair)
            {
                bool isNibbleArray = pair.Key is StdfNibbleArrayLayoutAttribute;
                int lengthIndex = ((StdfArrayLayoutAttribute)pair.Key).ArrayLengthFieldIndex;

                //we can skip entirely if the length field was zero
                //we'll combine this as part of the "reading" of the field
                var parseConditionNode = new SkipArrayAssignmentIfLengthIsZeroNode(lengthIndex);

                //find out if we should even parse this field
                if (pair.Value != null && !ShouldParseField(pair.Value.Name)) {
                    //we can simply return this skip node since it effectively encapsulates the length check as well
                    return new SkipTypeNode<T[]>(lengthIndex);
                }
                else {
                    var readNode = new ReadTypeNode<T[]>(lengthIndex, isNibble: isNibbleArray);
                    BlockNode assignmentBlock = null;
                    if (pair.Value != null) {
                        assignmentBlock = new BlockNode(new AssignFieldToPropertyNode<T[]>(pair.Value));
                    }
                    //return a FieldAssignmentNode.  Note we're combining the parseConditionNode and the readNode.
                    return new FieldAssignmentNode<T[]>(pair.Key.FieldIndex, new BlockNode(parseConditionNode, readNode), assignmentBlock);
                }
            }
        }

        class UnconverterGenerator {
            ILGenerator _ILGen;
            Type _Type;
            LocalBuilder _ConcreteRecord;
            LocalBuilder _StartedWriting;
            HashSet<int> _FieldLocalsCreated = new HashSet<int>();
            List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> _Fields;
            LocalBuilder _Writer;

            public UnconverterGenerator(ILGenerator ilgen, Type type) {
                if (ilgen == null) throw new ArgumentNullException("ilgen");
                if (type == null) throw new ArgumentNullException("type");
                _ILGen = ilgen;
                _Type = type;
            }

            public void GenerateUnconverter()
            {
                var _Fields = GetFieldLayoutsAndAssignments();

                var node = new UnconverterShellNode(
                        new BlockNode(
                            from pair in _Fields.AsEnumerable().Reverse()
                            //don't generate code for dependency properties
                            where !(pair.Key is StdfDependencyProperty)
                            //this call through reflection is icky, but marginally better than the hard-coded table
                            //we're just binding to the generic GenerateAssignment method for the field's type
                            let callInfo = typeof(ConverterGenerator).GetMethod("GenerateAssignment", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(pair.Key.FieldType)
                            select (CodeNode)callInfo.Invoke(this, new object[] { pair })
                            ));

            }

            //TODO: refactor this so we're not duplicated with ConverterFactory
            private List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> GetFieldLayoutsAndAssignments() {
                //get the list
                var attributes = from a in _Type.GetCustomAttributes(typeof(StdfFieldLayoutAttribute), true).Cast<StdfFieldLayoutAttribute>()
                                 orderby a.FieldIndex
                                 select a;
                var list = new List<StdfFieldLayoutAttribute>(attributes);
                //make sure they are consecutive
                for (int i = 0; i < list.Count; i++) {
                    if (list[i].FieldIndex != i) throw new NonconsecutiveFieldIndexException(_Type);
                }
                var withPropInfo = from l in list
                                   select new KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>(
                                              l,
                                              (l.AssignTo == null) ? null : _Type.GetProperty(l.AssignTo));

                return new List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>>(withPropInfo);
            }

            CodeNode GenerateAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair)
            {
                StdfStringLayoutAttribute stringLayout = pair.Key as StdfStringLayoutAttribute;
                StdfArrayLayoutAttribute arrayLayout = pair.Key as StdfArrayLayoutAttribute;
                //if it is an array, defer to GenerateArrayAssignment
                if (arrayLayout != null)
                {
                    if (typeof(T) == typeof(string))
                    {
                        throw new InvalidOperationException(Resources.NoStringArrays);
                    }
                    if (typeof(T) == typeof(BitArray))
                    {
                        throw new InvalidOperationException(Resources.NoBitArrayArrays);
                    }
                    return GenerateArrayAssignment<T>(pair);
                }

                var initNodes = new List<CodeNode>();

                bool localWasPresent = false;
                if (!(localWasPresent = _FieldLocalsCreated.Contains(pair.Key.FieldIndex)))
                {
                    //add a create local node
                    initNodes.Add(new CreateFieldLocalForWritingNode(pair.Key.FieldIndex, pair.Key.FieldType));
                }

                //find out if there is an optional field flag that we need to manage
                StdfFieldLayoutAttribute optionalFieldLayout = null;
                StdfOptionalFieldLayoutAttribute currentAsOptionalFieldLayout = pair.Key as StdfOptionalFieldLayoutAttribute;
                if (currentAsOptionalFieldLayout != null)
                {
                    optionalFieldLayout = _Fields[currentAsOptionalFieldLayout.FlagIndex].Key;
                    if (!_FieldLocalsCreated.Contains(currentAsOptionalFieldLayout.FlagIndex))
                    {
                        initNodes.Add(new CreateFieldLocalForWritingNode(currentAsOptionalFieldLayout.FlagIndex, optionalFieldLayout.FieldType));
                    }
                }

                //this will hold a node that will write in the case we have no value to write
                CodeNode noValueWriteContingency = null;
                //this will hold the source of the write that will happen above
                CodeNode noValueWriteContingencySource = null;

                //Decide what to do if we don't have a value to write.
                //This will happen if we don't store the value in a property, it is "missing" from the property source, or something else
                //TODO: should these have a different precedence?
                if (pair.Key.MissingValue != null)
                {
                    //if we have a missing value, set that as the write source
                    noValueWriteContingencySource = new LoadMissingValueNode(pair.Key.MissingValue, typeof(T));
                }
                else if (localWasPresent) //TODO: this used to be here, but I can't figure out why || optionalField != null)
                {
                    //if the local was present when we started, that means it was initialized by another field. We can safely write it
                    noValueWriteContingencySource = new LoadFieldLocalNode(pair.Key.FieldIndex);
                }
                else if (typeof(T).IsValueType)
                {
                    //if T is a value type, we're up a creek with nothing to write.
                    //this is obviously not a good place, so throw in the converter
                    noValueWriteContingency = new ThrowInvalidOperationNode(string.Format(Resources.NonNullableField, pair.Key.FieldIndex, _Type));
                }
                else
                {
                    //otherwise, we can try to write null (unless it is a fixed-length string, which should have a default instead)
                    if (stringLayout != null && stringLayout.Length > 0)
                    {
                        //TODO: move this check into StdfStringLayout if we can, along with a check that the missing value length matches
                        throw new NotSupportedException(Resources.FixedLengthStringMustHaveDefault);
                    }
                    noValueWriteContingencySource = new LoadNullNode();
                }

                //create the write node and the no-value contingency if we don't already have one
                CodeNode writeNode;
                if (stringLayout != null && stringLayout.Length > 0)
                {
                    noValueWriteContingency = noValueWriteContingency ?? new WriteFixedStringNode(stringLayout.Length, noValueWriteContingencySource);
                    writeNode = new WriteFixedStringNode(stringLayout.Length, new LoadFieldLocalNode(pair.Key.FieldIndex));
                }
                else
                {
                    noValueWriteContingency = noValueWriteContingency ?? new WriteTypeNode(typeof(T), noValueWriteContingencySource);
                    writeNode = new WriteTypeNode(typeof(T), new LoadFieldLocalNode(pair.Key.FieldIndex));
                }
                //return the crazy node
                //TODO: refactor this better, this sucks
                return new WriteFieldNode(pair.Key.FieldIndex, typeof(T),
                    initialization: new BlockNode(initNodes),
                    sourceProperty: pair.Value,
                    writeOperation: writeNode,
                    noValueWriteContingency: noValueWriteContingency,
                    optionalFieldIndex: optionalFieldLayout == null ? null : (int?)optionalFieldLayout.FieldIndex,
                    optionalFieldMask: optionalFieldLayout == null ? (byte)0 : currentAsOptionalFieldLayout.FlagMask);
            }

            CodeNode GenerateArrayAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
                StdfArrayLayoutAttribute arrayLayout = (StdfArrayLayoutAttribute)pair.Key;

                var initNodes = new List<CodeNode>();

                //there are no array optionals, we should always have to create the local here
                if (_FieldLocalsCreated.Contains(arrayLayout.FieldIndex)) throw new InvalidOperationException("Array local was created before we generated code for it.");
                initNodes.Add(new CreateFieldLocalForWritingNode(arrayLayout.FieldIndex, typeof(T[])));

                if (pair.Value == null) {
                    throw new InvalidOperationException(Resources.ArraysMustBeAssignable);
                }

                CodeNode writeNode;
                StdfFieldLayoutAttribute lengthLayout = _Fields[arrayLayout.ArrayLengthFieldIndex].Key;
                LocalBuilder lengthLocal = null;
                if (_FieldLocalsCreated.Contains(arrayLayout.ArrayLengthFieldIndex)) {
                    writeNode = new ValidateSharedLengthLocalNode(arrayLayout.FieldIndex, arrayLayout.ArrayLengthFieldIndex);
                }
                else {
                    writeNode = new BlockNode(
                        new CreateFieldLocalForWritingNode(arrayLayout.ArrayLengthFieldIndex, _Fields[arrayLayout.ArrayLengthFieldIndex].Key.FieldType),
                        new SetLengthLocalNode(arrayLayout.FieldIndex, arrayLayout.ArrayLengthFieldIndex));
                }

                writeNode = new BlockNode(
                    writeNode,
                    new WriteTypeNode(typeof(T[]), new LoadFieldLocalNode(arrayLayout.FieldIndex)));

                return new WriteFieldNode(arrayLayout.FieldIndex, typeof(T[]),
                    initialization: new BlockNode(initNodes),
                    sourceProperty: pair.Value,
                    writeOperation: writeNode);
            }

            void InitializeLocal(LocalBuilder local) {
                if (local.LocalType.GetConstructor(new Type[0]) != null) {
                    _ILGen.Newobj(local.LocalType);
                    _ILGen.Stloc(local);
                }
                else if (local.LocalType.IsValueType) {
                    _ILGen.Ldloca(local);
                    _ILGen.Initobj(local.LocalType);
                }
                else {
                    _ILGen.Ldnull();
                    _ILGen.Stloc(local);
                }
            }
        }
    }
}
