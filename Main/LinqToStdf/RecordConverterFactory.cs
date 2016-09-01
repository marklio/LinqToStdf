// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace LinqToStdf {
    using Attributes;
    using CompiledQuerySupport;
    using LinqToStdf.RecordConverting;
    using LinqToStdf.Records;

    public enum ConverterType
    {
        Converter,
        Unconverter,
    }
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
    /// <seealso cref="FieldLayoutAttribute"/>
    public class RecordConverterFactory {

        public event Action<ConverterType, Type> ConverterGenerated;

        /// <summary>
        /// Creates a new factory
        /// </summary>
        public RecordConverterFactory() : this((RecordsAndFields)null) { }

        /// <summary>
        /// Used internally by compiled query support
        /// </summary>
        /// <param name="recordsAndFields"></param>
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
        /// Keep in mind that this will cause you to reuse all codegen from the one factory.  Note that the settings
        /// of the new factory will not affect any converters already created by the factory to clone.
        /// </remarks>
        /// <param name="factoryToClone"></param>
        public RecordConverterFactory(RecordConverterFactory factoryToClone)
        {
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
        public void RegisterRecordConverter(RecordType recordType, Converter<UnknownRecord, StdfRecord> converter)
        {
            _Converters.Add(recordType, converter);
        }

        /// <summary>
        /// Registers an "unconverter" for a <see cref="RecordType"/> directly.
        /// Useful for special-casing odd records that cannot be described with supported meta-data.
        /// </summary>
        /// <param name="type">The type to be unconverted</param>
        /// <param name="unconverter">The "unconverter" delegate.</param>
        public void RegisterRecordUnconverter(Type type, Func<StdfRecord, Endian, UnknownRecord> unconverter)
        {
            _Unconverters.Add(type, unconverter);
        }

        /// <summary>
        /// Gets a converter for a given <see cref="RecordType"/>
        /// </summary>
        /// <param name="recordType">the record type to retrieve</param>
        /// <returns>If a converter for <paramref name="recordType"/> is register, the registered converter delegate,
        /// otherwise, a "null" converter that passes the unknown record through.</returns>
        public Converter<UnknownRecord, StdfRecord> GetConverter(RecordType recordType) {
            //if we don't have a converter, return identity conversion
            Converter<UnknownRecord, StdfRecord> converter;
            if (_Converters.TryGetValue(recordType, out converter)) {
                return converter;
                
            }
            return converter = (r) => r;
        }

        /// <summary>
        /// Gets a unconverter for a given type
        /// </summary>
        /// <param name="type">type to be unconverted</param>
        /// <returns>If an "unconverter" for <paramref name="type"/> is register, the registered unconverter delegate,
        /// otherwise, an invalid operation exception is thrown.</returns>
        public Func<StdfRecord, Endian, UnknownRecord> GetUnconverter(Type type)
        {
            Func<StdfRecord, Endian, UnknownRecord> unconverter;
            //if it is already an UnknownRecord, make sure it is the right endianness
            if (type == typeof(UnknownRecord))
            {
                return (r, e) =>
                {
                    var ur = (UnknownRecord)r;
                    if (ur.Endian != e)
                    {
                        throw new InvalidOperationException(Resources.UnconvertEndianMismatch);
                    }
                    return ur;
                };
            }
            else if (_Unconverters.TryGetValue(type, out unconverter))
            {
                return unconverter;
            }
            else
            {
                //we can't tolerate not being able to unparse a record
                throw new InvalidOperationException(string.Format(Resources.NoRegisteredUnconverter, type));
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
        public void SaveDynamicAssembly()
        {
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
            if (_RecordsAndFields == null || _RecordsAndFields.TypeHasFields(type)) {
                //TODO:consider making the pattern more clear here
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

        /// <summary>
        /// Does the actual work of creating the converter.  Note that this does not operate lazily,
        /// it is merely called lazily.
        /// </summary>
        Converter<UnknownRecord, StdfRecord> LazyCreateConverterForType(Type type)
        {
            ILGenerator ilGenerator = null;
            Func<Converter<UnknownRecord, StdfRecord>> finalizeConverter = null;
            if (Debug)
            {
#if SILVERLIGHT
                throw new NotSupportedException(Resources.NoDebugInSilverlight);
#else
                ilGenerator = CreateNewRefEmitMethod<Converter<UnknownRecord, StdfRecord>>(string.Format("{0}Converter", type.Name), string.Format("ConvertTo{0}", type.Name), ref finalizeConverter);
#endif
            }
            else
            {
                ilGenerator = CreateNewLCGMethod<Converter<UnknownRecord, StdfRecord>>(string.Format("ConvertTo{0}", type.Name), ref finalizeConverter);
            }
            var generator = new ConverterGenerator(ilGenerator, type, _RecordsAndFields?.GetFieldsForType(type));
            {
                generator.GenerateConverter();
            }
            var converter = finalizeConverter();
            ConverterGenerated?.Invoke(ConverterType.Converter, type);
            return converter;
        }

        #endregion

        #region CreateUnconverterForType

        Func<StdfRecord, Endian, UnknownRecord> CreateUnconverterForType(Type type)
        {
            if (!typeof(StdfRecord).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(string.Format(Resources.ConverterTargetNotStdfRecord, type));
            }
            Func<StdfRecord, Endian, UnknownRecord> unconverter = null;
            return (r, e) =>
            {
                if (unconverter == null)
                {
                    unconverter = LazyCreateUnconverterForType(type);
                }
                return unconverter(r, e);
            };
        }

        Func<StdfRecord, Endian, UnknownRecord> LazyCreateUnconverterForType(Type type)
        {
            ILGenerator ilGenerator = null;
            Func<Func<StdfRecord, Endian, UnknownRecord>> finalizeUnconverter = null;
            if (Debug)
            {
#if SILVERLIGHT
                throw new NotSupportedException(Resources.NoDebugInSilverlight);
#else
                ilGenerator = CreateNewRefEmitMethod<Func<StdfRecord, Endian, UnknownRecord>>(string.Format("{0}Unconverter", type.Name), string.Format("UnconvertFrom{0}", type.Name), ref finalizeUnconverter);
#endif
            }
            else
            {
                ilGenerator = CreateNewLCGMethod<Func<StdfRecord, Endian, UnknownRecord>>(string.Format("UnconvertFrom{0}", type.Name), ref finalizeUnconverter);
            }
            var generator = new UnconverterGenerator(ilGenerator, type);
            generator.GenerateUnconverter();
            var unconverter = finalizeUnconverter();
            ConverterGenerated?.Invoke(ConverterType.Unconverter, type);
            return unconverter;
        }

        #endregion

        /// <summary>
        /// Gets signature information in the form of a MethodInfo for a type that is a delegate.
        /// </summary>
        MethodInfo GetSignatureInfo<TSig>()
        {
            var delegateType = typeof(TSig);
            if (!typeof(Delegate).IsAssignableFrom(delegateType))
            {
                throw new InvalidOperationException("TSig must be a delegate");
            }
            return delegateType.GetMethod("Invoke");
        }

        /// <summary>
        /// Creates a new dynamic Reflection.Emit method and provides the ILGenerator for it, as well as a delegate to call
        /// to "bake" the method for use.
        /// </summary>
        /// <typeparam name="TSig">A delegate that represents the signature of the method to be created.</typeparam>
        /// <param name="typeName">The name for a type to create to hold the method</param>
        /// <param name="methodName">The name for the method</param>
        /// <param name="bakeMethod">A delegate to call with IL generation is complete to get the created method in
        /// the form of a delegate</param>
        /// <returns>The ILGenerator for the new method</returns>
        private ILGenerator CreateNewRefEmitMethod<TSig>(string typeName, string methodName, ref Func<TSig> bakeMethod)
        {
            var sigMethod = GetSignatureInfo<TSig>();
            if (_DynamicAssembly == null) InitializeDynamicAssembly();
            var dynamicType = _DynamicModule.DefineType(typeName);
            var dynamicMethod = dynamicType.DefineMethod(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static,
                sigMethod.ReturnType,
                (from p in sigMethod.GetParameters() select p.ParameterType).ToArray());
            bakeMethod = () =>
            {
                var newType = dynamicType.CreateType();
                return (TSig)(object)Delegate.CreateDelegate(
                    typeof(TSig),
                    newType.GetMethod(methodName));
            };
            return dynamicMethod.GetILGenerator();
        }

        /// <summary>
        /// Creates a new dynamic method using LCG and provides the ILGenerator for it, as well as a delegate to call
        /// to "bake" the method for use.
        /// </summary>
        /// <typeparam name="TSig">A delegate that represents the signature of the method to be created.</typeparam>
        /// <param name="methodName">The name for the method</param>
        /// <param name="bakeMethod">A delegate to call with IL generation is complete to get the created method in
        /// the form of a delegate</param>
        /// <returns>The ILGenerator for the new method</returns>
        private ILGenerator CreateNewLCGMethod<TSig>(string methodName, ref Func<TSig> bakeMethod)
        {
            var sigMethod = GetSignatureInfo<TSig>();
            var dynamicMethod = new DynamicMethod(
                                methodName,
                                sigMethod.ReturnType,
                                (from p in sigMethod.GetParameters() select p.ParameterType).ToArray(),
                                typeof(RecordConverterFactory).Assembly.ManifestModule,
                                true);
            bakeMethod = () => (TSig)(object)dynamicMethod.CreateDelegate(typeof(TSig));
            return dynamicMethod.GetILGenerator();
        }
    }
}
