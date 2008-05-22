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
                               throw new InvalidOperationException("Can't unconvert unknown records with the wrong Endianness.");
                           }
                           return ur;
                       };
            }
            else if (!_Unconverters.ContainsKey(type)) {
                throw new InvalidOperationException(string.Format("The type {0} has no registered unconverter", type));
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
            if (!Debug || _DynamicAssembly == null) throw new InvalidOperationException("You can only save the dynamic assembly if debug is turned on and code has been generated.");
            throw new NotSupportedException();
            //_DynamicAssembly.Save("DynamicConverters.dll");
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
                throw new InvalidOperationException(string.Format("{0} is not assignable from StdfRecord.", type));
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
                throw new NotSupportedException("Silverlight doesn't support debug mode");
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
                    new Type[] { typeof(UnknownRecord) });
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
                throw new InvalidOperationException(string.Format("{0} is not assignable from StdfRecord.", type));
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
                throw new NotSupportedException("Silverlight doesn't support debug mode");
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

        /// <summary>
        /// This helper class encapsulates the generation of IL for the converters
        /// </summary>
        class ConverterGenerator {
            ILGenerator _ILGen;
            Type _Type;
            LocalBuilder _NewRecord;
            Dictionary<int, LocalBuilder> _FieldLocals = new Dictionary<int, LocalBuilder>();
            LocalBuilder _Reader;
            Label _EndLabel;
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
            internal void GenerateConverter() {
                //TODO: Even though this has already gone through some hefty refactoring,
                //it could use some more.

                //create a new record and store it in _NewRecord
                _NewRecord = _ILGen.DeclareLocal(_Type);
                _ILGen.Newobj(_Type);
                _ILGen.Stloc(_NewRecord);

                //ensure the target record is compatible (call UnknownRecord.EnsureConvertibleTo)
                _ILGen.Ldarg_0();
                _ILGen.Ldloc(_NewRecord);
                _ILGen.Callvirt(typeof(UnknownRecord).GetMethod("EnsureConvertibleTo", typeof(StdfRecord)));
                //if we're still here, then we can continue (the above throws otherwise)

                //get a memory stream on the record content (call UnknownRecord.GetMemoryStreamForContent)
                _Reader = _ILGen.DeclareLocal(typeof(BinaryReader));
                _ILGen.Ldarg_0();
                _ILGen.Callvirt(typeof(UnknownRecord).GetMethod("GetBinaryReaderForContent"));
                _ILGen.Stloc(_Reader);
                //nothing on the stack

                //start try
                _ILGen.BeginExceptionBlock();

                List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> fields = GetFieldLayoutsAndAssignments();

                //pick up any fields that are required to parse the fields we need
                if (_Fields != null) {
                    foreach (var pair in fields) {
                        //if it's an assigned field that we are parsing
                        if (pair.Value != null && _Fields.Contains(pair.Value.Name)) {
                            //if it's an optional field
                            var op = pair.Key as StdfOptionalFieldLayoutAttribute;
                            if (op != null) {
                                //if the flag index is an assigned field, add it to our list of parsed fields
                                var prop = fields[op.FlagIndex].Value;
                                if (prop != null) _Fields.Add(prop.Name);
                            }
                            else {
                                var dep = pair.Key as StdfDependencyProperty;
                                if (dep != null) {
                                    var prop = fields[dep.DependentOnIndex].Value;
                                    if (prop != null) _Fields.Add(prop.Name);
                                }

                            }
                        }
                    }
                }

                _EndLabel = _ILGen.DefineLabel();
                foreach (KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair in fields) {
                    //don't generate code for dependency properties
                    if (pair.Key is StdfDependencyProperty) continue;
                    GenerateEndOfStreamCheck();

                    //TODO: this is an ugly list, would be nice to get T from somewhere directly
                    if (pair.Key.FieldType == typeof(string)) GenerateAssignment<string>(pair);
                    else if (pair.Key.FieldType == typeof(byte)) GenerateAssignment<byte>(pair);
                    else if (pair.Key.FieldType == typeof(sbyte)) GenerateAssignment<sbyte>(pair);
                    else if (pair.Key.FieldType == typeof(ushort)) GenerateAssignment<ushort>(pair);
                    else if (pair.Key.FieldType == typeof(short)) GenerateAssignment<short>(pair);
                    else if (pair.Key.FieldType == typeof(uint)) GenerateAssignment<uint>(pair);
                    else if (pair.Key.FieldType == typeof(int)) GenerateAssignment<int>(pair);
                    else if (pair.Key.FieldType == typeof(ulong)) GenerateAssignment<ulong>(pair);
                    else if (pair.Key.FieldType == typeof(long)) GenerateAssignment<long>(pair);
                    else if (pair.Key.FieldType == typeof(float)) GenerateAssignment<float>(pair);
                    else if (pair.Key.FieldType == typeof(double)) GenerateAssignment<double>(pair);
                    else if (pair.Key.FieldType == typeof(DateTime)) GenerateAssignment<DateTime>(pair);
                    else if (pair.Key.FieldType == typeof(BitArray)) GenerateAssignment<BitArray>(pair);
                    else {
                        throw new NotSupportedException(string.Format(Resources.UnsupportedStdfFieldTypeMessage, pair.Key.FieldType));
                    }
                }
                _ILGen.MarkLabel(_EndLabel);

                //start finally
                _ILGen.BeginFinallyBlock();
                //dispose the reader
                _ILGen.Ldloc(_Reader);
                _ILGen.Callvirt(typeof(IDisposable).GetMethod("Dispose"));
                _ILGen.EndExceptionBlock();

                //return the record
                _ILGen.Ldloc(_NewRecord);
                _ILGen.Ret();
            }

            private List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> GetFieldLayoutsAndAssignments() {
                //get the list
                var attributes = from a in ((Type)_Type).GetCustomAttributes(typeof(StdfFieldLayoutAttribute), true).Cast<StdfFieldLayoutAttribute>()
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
                                              (l.AssignTo == null) ? null : ((Type)_Type).GetProperty(l.AssignTo));

                return new List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>>(withPropInfo);
            }

            void GenerateEndOfStreamCheck() {
                _ILGen.Ldloc(_Reader);
                _ILGen.Callvirt(typeof(BinaryReader).GetProperty("AtEndOfStream").GetGetMethod());
                _ILGen.Brtrue(_EndLabel);
            }

            void GenerateAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
                StdfArrayLayoutAttribute arrayLayout = pair.Key as StdfArrayLayoutAttribute;
                if (arrayLayout != null) {
                    if (typeof(T) == typeof(string)) {
                        throw new InvalidOperationException("String fields do not support array layout attributes.");
                    }
                    GenerateArrayAssignment<T>(pair);
                    return;
                }

                StdfStringLayoutAttribute stringLayout = pair.Key as StdfStringLayoutAttribute;
                var stringLength = -1;
                if (stringLayout != null && stringLayout.Length > 0) {
                    stringLength = stringLayout.Length;
                }

                //find out if we should even parse this field
                if (pair.Value != null && !ShouldParseField(pair.Value.Name)) {
                    _ILGen.Ldloc(_Reader);
                    if (stringLength > 0) {
                        _ILGen.Ldc_I4(stringLength);
                        _ILGen.Callvirt(typeof(BinaryReader).GetMethod("Skip", typeof(int)));
                    }
                    else {
                        _ILGen.Callvirt(typeof(BinaryReader).GetMethod(GetSkipMethodForType(typeof(T))));
                    }
                    return;
                }

                LocalBuilder local = _ILGen.DeclareLocal<T>();
                _FieldLocals[pair.Key.FieldIndex] = local;

                _ILGen.Ldloc(_Reader);
                if (stringLength > 0) {
                    _ILGen.Ldc_I4(stringLength);
                    _ILGen.Callvirt(typeof(BinaryReader).GetMethod("ReadString", typeof(int)));
                }
                else {
                    _ILGen.Callvirt(typeof(BinaryReader).GetMethod(GetReaderMethodForType(typeof(T)), new Type[0]));
                }
                //store the value in the local variable
                _ILGen.Stloc(local);

                if (pair.Value != null) {
                    Label keepGoing = _ILGen.DefineLabel();
                    StdfOptionalFieldLayoutAttribute optionalLayout = pair.Key as StdfOptionalFieldLayoutAttribute;
                    if (optionalLayout != null) {
                        LocalBuilder flag = _FieldLocals[optionalLayout.FlagIndex];
                        _ILGen.Ldloc(flag);
                        _ILGen.Ldc_I4_S(optionalLayout.FlagMask);
                        _ILGen.And();
                        _ILGen.Brtrue(keepGoing);
                    }
                    else if (pair.Key.MissingValue != null) {
                        //compare to the missing value
                        _ILGen.Ldloca(local);
                        _ILGen.Ldc<T>(pair.Key.MissingValue);
                        if (local.LocalType.IsValueType) {
                            _ILGen.Box<T>();
                        }
                        _ILGen.Constrained<T>();
                        _ILGen.Callvirt(typeof(object).GetMethod("Equals", typeof(object)));
                        _ILGen.Brtrue(keepGoing);
                    }
                    _ILGen.Ldloc(_NewRecord);
                    _ILGen.Ldloc(local);
                    if (typeof(T).IsValueType) {
                        Type genericType = typeof(Nullable<>).MakeGenericType(typeof(T));
                        if (pair.Value.PropertyType == genericType) {
                            _ILGen.Newobj(genericType, typeof(T));
                        }
                    }
                    _ILGen.Callvirt(pair.Value.GetSetMethod());
                    _ILGen.Nop();
                    _ILGen.MarkLabel(keepGoing);
                }
            }

            void GenerateArrayAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
                bool isNibbleArray = pair.Key is StdfNibbleArrayLayoutAttribute;
                LocalBuilder lengthLocal;
                lengthLocal = _FieldLocals[((StdfArrayLayoutAttribute)pair.Key).ArrayLengthFieldIndex];
                Label end = _ILGen.DefineLabel();
                _ILGen.Ldloc(lengthLocal);
                _ILGen.Brfalse(end);

                //find out if we should even parse this field
                if (pair.Value != null && !ShouldParseField(pair.Value.Name)) {
                    _ILGen.Ldloc(_Reader);
                    _ILGen.Ldloc(lengthLocal);
                    _ILGen.Callvirt(typeof(BinaryReader).GetMethod(GetSkipMethodForType(typeof(T[]))));
                }
                else {

                    //create the array and store it
                    LocalBuilder arrayLocal = _ILGen.DeclareLocal<T[]>();
                    _FieldLocals[pair.Key.FieldIndex] = arrayLocal;
                    _ILGen.Ldloc(_Reader);
                    _ILGen.Ldloc(lengthLocal);
                    if (isNibbleArray) {
                        _ILGen.Callvirt(typeof(BinaryReader).GetMethod("ReadNibbleArray", typeof(int)));
                    }
                    else {
                        _ILGen.Callvirt(typeof(BinaryReader).GetMethod(GetReaderMethodForType(typeof(T[])), typeof(int)));
                    }
                    _ILGen.Stloc(arrayLocal);
                    if (pair.Value != null) {
                        //assign the value
                        _ILGen.Ldloc(_NewRecord);
                        _ILGen.Ldloc(arrayLocal);
                        _ILGen.Callvirt(pair.Value.GetSetMethod());
                    }
                }
                _ILGen.MarkLabel(end);
            }

            static string GetReaderMethodForType(Type type) {
                //ewww
                if (type == typeof(byte)) return "ReadByte";
                else if (type == typeof(byte[])) return "ReadByteArray";
                else if (type == typeof(sbyte)) return "ReadSByte";
                else if (type == typeof(sbyte[])) return "ReadSByteArray";
                else if (type == typeof(ushort)) return "ReadUInt16";
                else if (type == typeof(ushort[])) return "ReadUInt16Array";
                else if (type == typeof(short)) return "ReadInt16";
                else if (type == typeof(short[])) return "ReadInt16Array";
                else if (type == typeof(uint)) return "ReadUInt32";
                else if (type == typeof(uint[])) return "ReadUInt32Array";
                else if (type == typeof(int)) return "ReadInt32";
                else if (type == typeof(int[])) return "ReadInt32Array";
                else if (type == typeof(ulong)) return "ReadUInt64";
                else if (type == typeof(ulong[])) return "ReadUInt64Array";
                else if (type == typeof(long)) return "ReadInt64";
                else if (type == typeof(long[])) return "ReadInt64Array";
                else if (type == typeof(float)) return "ReadSingle";
                else if (type == typeof(float[])) return "ReadSingleArray";
                else if (type == typeof(double)) return "ReadDouble";
                else if (type == typeof(double[])) return "ReadDoubleArray";
                else if (type == typeof(string)) return "ReadString";
                else if (type == typeof(DateTime)) return "ReadDateTime";
                else if (type == typeof(BitArray)) return "ReadBitArray";
                else {
                    throw new NotSupportedException(string.Format("Ldc<T> does not support T is {0}", type));
                }
            }

            static string GetSkipMethodForType(Type type) {
                if (type == typeof(byte)) return "Skip1";
                else if (type == typeof(byte[])) return "Skip1Array";
                else if (type == typeof(sbyte)) return "Skip1";
                else if (type == typeof(sbyte[])) return "Skip1Array";
                else if (type == typeof(ushort)) return "Skip2";
                else if (type == typeof(ushort[])) return "Skip2Array";
                else if (type == typeof(short)) return "Skip2";
                else if (type == typeof(short[])) return "Skip2Array";
                else if (type == typeof(uint)) return "Skip4";
                else if (type == typeof(uint[])) return "Skip4Array";
                else if (type == typeof(int)) return "Skip4";
                else if (type == typeof(int[])) return "Skip4Array";
                else if (type == typeof(ulong)) return "Skip8";
                else if (type == typeof(ulong[])) return "Skip8Array";
                else if (type == typeof(long)) return "Skip8";
                else if (type == typeof(long[])) return "Skip8Array";
                else if (type == typeof(float)) return "Skip4";
                else if (type == typeof(float[])) return "Skip4Array";
                else if (type == typeof(double)) return "Skip8";
                else if (type == typeof(double[])) return "Skip8Array";
                else if (type == typeof(string)) return "SkipString";
                else if (type == typeof(DateTime)) return "Skip4";
                else if (type == typeof(BitArray)) return "SkipBitArray";
                else {
                    throw new NotSupportedException(string.Format("Does not support T is {0}", type));
                }
            }

        }

        class UnconverterGenerator {
            ILGenerator _ILGen;
            Type _Type;
            LocalBuilder _ConcreteRecord;
            LocalBuilder _StartedWriting;
            Dictionary<int, LocalBuilder> _FieldLocals = new Dictionary<int, LocalBuilder>();
            List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> _Fields;
            LocalBuilder _Writer;

            public UnconverterGenerator(ILGenerator ilgen, Type type) {
                if (ilgen == null) throw new ArgumentNullException("ilgen");
                if (type == null) throw new ArgumentNullException("type");
                _ILGen = ilgen;
                _Type = type;
            }

            public void GenerateUnconverter() {
                _ConcreteRecord = _ILGen.DeclareLocal(_Type);
                _ILGen.Ldarg_0();
                _ILGen.Castclass(_Type);
                _ILGen.Stloc(_ConcreteRecord);

                _StartedWriting = _ILGen.DeclareLocal<bool>();
                _ILGen.Ldc_I4_0();
                _ILGen.Stloc(_StartedWriting);

                //create a memory stream for writing
                LocalBuilder memoryStream = _ILGen.DeclareLocal<MemoryStream>();
                _ILGen.Newobj<MemoryStream>();
                _ILGen.Stloc(memoryStream);
                _ILGen.BeginExceptionBlock();


                //create a binary writer
                _Writer = _ILGen.DeclareLocal<BinaryWriter>();
                //load args for binary writer .ctor
                _ILGen.Ldloc(memoryStream);
                _ILGen.Ldarg_1(); //the endianness
                _ILGen.Ldc_I4_1(); //true for reading backwards
                _ILGen.Newobj<BinaryWriter>(typeof(MemoryStream), typeof(Endian), typeof(bool));
                _ILGen.Stloc(_Writer);
                //nothing on the stack

                _Fields = GetFieldLayoutsAndAssignments();

                //reverse the fields
                var reversed = _Fields.ToArray();
                Array.Reverse(reversed);
                foreach (KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair in reversed) {
                    //don't generate code for dependency properties
                    if (pair.Key is StdfDependencyProperty) continue;

                    //TODO: this is an ugly list, would be nice to get T from somewhere directly
                    if (pair.Key.FieldType == typeof(string)) GenerateAssignment<string>(pair);
                    else if (pair.Key.FieldType == typeof(byte)) GenerateAssignment<byte>(pair);
                    else if (pair.Key.FieldType == typeof(sbyte)) GenerateAssignment<sbyte>(pair);
                    else if (pair.Key.FieldType == typeof(ushort)) GenerateAssignment<ushort>(pair);
                    else if (pair.Key.FieldType == typeof(short)) GenerateAssignment<short>(pair);
                    else if (pair.Key.FieldType == typeof(uint)) GenerateAssignment<uint>(pair);
                    else if (pair.Key.FieldType == typeof(int)) GenerateAssignment<int>(pair);
                    else if (pair.Key.FieldType == typeof(ulong)) GenerateAssignment<ulong>(pair);
                    else if (pair.Key.FieldType == typeof(long)) GenerateAssignment<long>(pair);
                    else if (pair.Key.FieldType == typeof(float)) GenerateAssignment<float>(pair);
                    else if (pair.Key.FieldType == typeof(double)) GenerateAssignment<double>(pair);
                    else if (pair.Key.FieldType == typeof(DateTime)) GenerateAssignment<DateTime>(pair);
                    else if (pair.Key.FieldType == typeof(BitArray)) GenerateAssignment<BitArray>(pair);
                    else {
                        throw new NotSupportedException(string.Format(Resources.UnsupportedStdfFieldTypeMessage, pair.Key.FieldType));
                    }
                }
                //at this point, the memory stream should have all the bytes for the content in it, but backwards

                //get the content
                _ILGen.Ldloc(memoryStream);
                _ILGen.Callvirt(typeof(MemoryStream).GetMethod("ToArray"));
                var content = _ILGen.DeclareLocal<byte[]>();
                _ILGen.Stloc(content);
                _ILGen.Ldloc(content);
                _ILGen.Callvirt(typeof(Array).GetMethod("Reverse", typeof(Array)));

                _ILGen.BeginFinallyBlock();
                //dispose the memorystream
                _ILGen.Ldloc(memoryStream);
                _ILGen.Callvirt(typeof(IDisposable).GetMethod("Dispose"));
                _ILGen.EndExceptionBlock();

                //get the record type
                _ILGen.Ldarg_0();
                _ILGen.Callvirt(typeof(StdfRecord).GetProperty("RecordType").GetGetMethod());

                //load the content
                _ILGen.Ldloc(content);

                //get the endian
                _ILGen.Ldarg_1();

                //new up a new UnknownRecord!
                _ILGen.Newobj<UnknownRecord>(typeof(RecordType), typeof(byte[]), typeof(Endian));

                _ILGen.Ret();
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

            void GenerateAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
                StdfStringLayoutAttribute stringLayout = pair.Key as StdfStringLayoutAttribute;
                StdfArrayLayoutAttribute arrayLayout = pair.Key as StdfArrayLayoutAttribute;
                if (arrayLayout != null) {
                    if (typeof(T) == typeof(string)) {
                        throw new InvalidOperationException("String fields do not support array layout attributes.");
                    }
                    if (typeof(T) == typeof(BitArray)) {
                        throw new InvalidOperationException("BitArray fields do not support array layout attributes.");
                    }
                    GenerateArrayAssignment<T>(pair);
                    return;
                }

                bool localWasPresent = false;
                LocalBuilder local;
                if (!(localWasPresent = _FieldLocals.TryGetValue(pair.Key.FieldIndex, out local))) {
                    local = _ILGen.DeclareLocal<T>();
                    //initialize it so we can write it if we have no data
                    InitializeLocal(local);
                }

                //find out if there is an optional field flag that we need to manage
                StdfFieldLayoutAttribute optionalFieldLayout = null;
                StdfOptionalFieldLayoutAttribute currentAsOptionalFieldLayout = pair.Key as StdfOptionalFieldLayoutAttribute;
                LocalBuilder optionalField = null;
                if (currentAsOptionalFieldLayout != null) {
                    optionalFieldLayout = _Fields[currentAsOptionalFieldLayout.FlagIndex].Key;
                    if (!_FieldLocals.TryGetValue(currentAsOptionalFieldLayout.FlagIndex, out optionalField)) {
                        //create and initialize the local if we haven't yet.
                        optionalField = _ILGen.DeclareLocal(optionalFieldLayout.FieldType);
                        InitializeLocal(optionalField);
                    }
                }

                var hasValueLocal = _ILGen.DeclareLocal<bool>();
                _ILGen.Ldc_I4_0();
                _ILGen.Stloc(hasValueLocal);

                var decide = _ILGen.DefineLabel();
                var skipWriting = _ILGen.DefineLabel();
                var doWrite = _ILGen.DefineLabel();
                var end = _ILGen.DefineLabel();
                #region getting value from property
                if (pair.Value != null) {
                    //we write to local via a property
                    //load up the value
                    _ILGen.Ldloc(_ConcreteRecord);
                    _ILGen.Callvirt(pair.Value.GetGetMethod());
                    //generate the check for whether we have a value to write
                    if (pair.Value.PropertyType.IsValueType) {
                        //it's a value type.  Check for Nullable
                        Type nullable = typeof(Nullable<>).MakeGenericType(typeof(T));
                        if (pair.Value.PropertyType == nullable) {
                            var nullableLocal = _ILGen.DeclareLocal(nullable);
                            _ILGen.Stloc(nullableLocal);
                            _ILGen.Ldloca(nullableLocal); //load address so we can call methods
                            _ILGen.Callvirt(nullable.GetProperty("HasValue").GetGetMethod());
                            _ILGen.Dup();
                            _ILGen.Stloc(hasValueLocal);
                            //if we don't have a value, branch to decide if we should be writing something anyway
                            _ILGen.Brfalse(decide);
                            _ILGen.Ldloca(nullableLocal);
                            _ILGen.Callvirt(nullable.GetProperty("Value").GetGetMethod());
                        }
                        _ILGen.Stloc(local);
                        _ILGen.Br(doWrite);
                    }
                    else {
                        //copy so we can store
                        _ILGen.Dup();
                        _ILGen.Stloc(local);
                        //it's a ref type, check for null (more complicated than I thought)
                        _ILGen.Ldnull(); //load null
                        _ILGen.Ceq(); //compare to null
                        //0 on stack if has value
                        _ILGen.Ldc_I4_0();
                        _ILGen.Ceq(); //compare to 0
                        //1 on stack if has value
                        _ILGen.Dup();
                        _ILGen.Stloc(hasValueLocal);
                        _ILGen.Brtrue(doWrite);
                    }
                }
                #endregion
                _ILGen.MarkLabel(decide);
                //at this point, we don't have a value from the record directly,
                //but we may need to write if we have started already
                _ILGen.Ldloc(_StartedWriting);
                //we can skip writing if we haven't started already
                _ILGen.Brfalse(skipWriting);
                //threadstart has convenient signature
                ThreadStart doTheWrite = () => {
                                             if (stringLayout != null && stringLayout.Length > 0) {
                                                 _ILGen.Ldc_I4(stringLayout.Length);
                                                 _ILGen.Callvirt(typeof(BinaryWriter).GetMethod("WriteString", typeof(string), typeof(int)));
                                             }
                                             else {
                                                 _ILGen.Callvirt(typeof(BinaryWriter).GetMethod(GetWriterMethodForType(typeof(T)), typeof(T)));
                                             }
                                         };
                //if we have a missing value we can write, write that, so load it up
                if (pair.Key.MissingValue != null) {
                    _ILGen.Ldloc(_Writer);
                    _ILGen.Ldc<T>(pair.Key.MissingValue);
                    doTheWrite();
                }
                else if (localWasPresent || optionalField != null) {
                    _ILGen.Ldloc(_Writer);
                    //our local is initialized already and is ready for writing
                    _ILGen.Ldloc(local);
                    doTheWrite();
                }
                else if (typeof(T).IsValueType) {
                    //if T is a value type, we're up a creek with nothing to write.
                    //this is obviously not a good place, so throw
                    _ILGen.Ldstr(string.Format("There is no contingency for writing \"NULL\" to field index {0} of {1}", pair.Key.FieldIndex, _Type));
                    _ILGen.Newobj<InvalidOperationException>(typeof(string));
                    _ILGen.Throw();
                }
                else {
                    if (stringLayout != null && stringLayout.Length > 0) {
                        //TODO: move this check into StdfStringLayout, along with a check that the missing value length matches
                        throw new NotSupportedException("Fixed-length string layouts must provide a missing value.");
                    }
                    _ILGen.Ldloc(_Writer);
                    //we'll have to write null and hope for the best
                    _ILGen.Ldnull();
                    doTheWrite();
                }
                _ILGen.Br(end);

                _ILGen.MarkLabel(doWrite);
                //we've started writing
                _ILGen.Ldc_I4_1();
                _ILGen.Stloc(_StartedWriting);
                //set any optional flag (if any)
                if (optionalField != null) {
                    var skipOptField = _ILGen.DefineLabel();
                    _ILGen.Ldloc(hasValueLocal);
                    _ILGen.Brfalse(skipOptField);
                    _ILGen.Ldloc(optionalField);
                    _ILGen.Ldc_I4_S(currentAsOptionalFieldLayout.FlagMask);
                    _ILGen.Or();
                    _ILGen.Stloc(optionalField);
                    _ILGen.MarkLabel(skipOptField);
                }
                //load up the writer and do the write
                _ILGen.Ldloc(_Writer);
                _ILGen.Ldloc(local);
                if (stringLayout != null && stringLayout.Length > 0) {
                    _ILGen.Ldc_I4(stringLayout.Length);
                    _ILGen.Callvirt(typeof(BinaryWriter).GetMethod("WriteString", typeof(string), typeof(int)));
                }
                else {
                    _ILGen.Callvirt(typeof(BinaryWriter).GetMethod(GetWriterMethodForType(typeof(T)), typeof(T)));
                }

                _ILGen.MarkLabel(skipWriting);
                _ILGen.MarkLabel(end);
            }

            void GenerateArrayAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
                StdfArrayLayoutAttribute arrayLayout = (StdfArrayLayoutAttribute)pair.Key;
                LocalBuilder local = _ILGen.DeclareLocal<T[]>();
                _FieldLocals.Add(arrayLayout.FieldIndex, local);

                if (pair.Value == null) {
                    throw new InvalidOperationException("Arrays must be assignable.");
                }

                _ILGen.Ldloc(_ConcreteRecord);
                _ILGen.Callvirt(pair.Value.GetGetMethod());
                _ILGen.Stloc(local);

                StdfFieldLayoutAttribute lengthLayout = _Fields[arrayLayout.FieldIndex].Key;
                LocalBuilder lengthLocal = null;
                if (_FieldLocals.TryGetValue(arrayLayout.ArrayLengthFieldIndex, out lengthLocal)) {
                    //someone else created the length, we need to verify they are equal
                    var dontThrow = _ILGen.DefineLabel();
                    var notNull = _ILGen.DefineLabel();
                    var compareLength = _ILGen.DefineLabel();
                    _ILGen.Ldloc(local);
                    _ILGen.Ldnull();
                    _ILGen.Ceq();
                    _ILGen.Brfalse(notNull);
                    _ILGen.Ldc_I4_0();
                    _ILGen.Br(compareLength);
                    //array is not null
                    _ILGen.MarkLabel(notNull);
                    _ILGen.Ldloc(local);
                    _ILGen.Ldlen();
                    _ILGen.MarkLabel(compareLength);
                    //TODO: find out if we need to special case length for nibble? I don't think so
                    if (lengthLayout.FieldType == typeof(ushort))
                        _ILGen.Conv_U2();
                    else if (lengthLayout.FieldType == typeof(byte))
                        _ILGen.Conv_I4();
                    else throw new NotSupportedException(string.Format("Array length fields must be either ushort or byte. ({0} not supported)", lengthLayout.FieldType));
                    _ILGen.Ldloc(lengthLocal);
                    _ILGen.Ceq();
                    _ILGen.Brtrue(dontThrow);
                    _ILGen.Ldstr(string.Format("Shared length arrays have differing lengths (length field index {0})", lengthLayout.FieldIndex));
                    _ILGen.Newobj<InvalidOperationException>(typeof(string));
                    _ILGen.Throw();
                    _ILGen.MarkLabel(dontThrow);
                }
                else {
                    //we create the length
                    lengthLocal = _ILGen.DeclareLocal(lengthLayout.FieldType);
                    _FieldLocals.Add(arrayLayout.ArrayLengthFieldIndex, lengthLocal);
                    InitializeLocal(lengthLocal);
                }

                //load up the value and write it
                _ILGen.Ldloc(_Writer);
                _ILGen.Ldloc(local);
                _ILGen.Callvirt(typeof(BinaryWriter).GetMethod(GetWriterMethodForType(typeof(T[]))));
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

            static string GetWriterMethodForType(Type type) {
                if (type == typeof(byte)) return "WriteByte";
                else if (type == typeof(byte[])) return "WriteByteArray";
                else if (type == typeof(sbyte)) return "WriteSByte";
                else if (type == typeof(sbyte[])) return "WriteSByteArray";
                else if (type == typeof(ushort)) return "WriteUInt16";
                else if (type == typeof(ushort[])) return "WriteUInt16Array";
                else if (type == typeof(short)) return "WriteInt16";
                else if (type == typeof(short[])) return "WriteInt16Array";
                else if (type == typeof(uint)) return "WriteUInt32";
                else if (type == typeof(uint[])) return "WriteUInt32Array";
                else if (type == typeof(int)) return "WriteInt32";
                else if (type == typeof(int[])) return "WriteInt32Array";
                else if (type == typeof(ulong)) return "WriteUInt64";
                else if (type == typeof(ulong[])) return "WriteUInt64Array";
                else if (type == typeof(long)) return "WriteInt64";
                else if (type == typeof(long[])) return "WriteInt64Array";
                else if (type == typeof(float)) return "WriteSingle";
                else if (type == typeof(float[])) return "WriteSingleArray";
                else if (type == typeof(double)) return "WriteDouble";
                else if (type == typeof(double[])) return "WriteDoubleArray";
                else if (type == typeof(string)) return "WriteString";
                else if (type == typeof(DateTime)) return "WriteDateTime";
                else if (type == typeof(BitArray)) return "WriteBitArray";
                else {
                    throw new NotSupportedException(string.Format("Ldc<T> does not support T is {0}", type));
                }
            }
        }
    }
}
