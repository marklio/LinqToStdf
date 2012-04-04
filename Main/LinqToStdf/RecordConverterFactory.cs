// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LinqToStdf {
    using Attributes;
    using CompiledQuerySupport;
    using LinqToStdf.RecordConverting;
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
        /// Keep in mind that this will cause you to reuse all codegen from the one factory.
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

    }
}
