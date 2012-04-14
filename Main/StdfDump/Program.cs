// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LinqToStdf;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;

namespace StdfDump {
    class Program {
        static void Main(string[] args) {
            var file = new StdfFile(args[0]);
            StdfFileWriter outFile = null;
            if (args.Length > 1) {
                outFile = new StdfFileWriter(args[1]);
            }
            try {
                int bytesWritten = 0;
                long bytesRead = 0;
                foreach (var r in file.GetRecords()) {
                    Console.WriteLine("Read Length: {0}", r.Offset - bytesRead);
                    bytesRead = r.Offset;
                    Console.WriteLine("{0}", r.GetType());
                    DumpRecord(r);
                    if (outFile != null) {
                        bytesWritten = outFile.WriteRecord(r);
                        Console.WriteLine("Written Length: {0}", bytesWritten);
                    }
                }
            }
            finally {
                if (outFile != null) outFile.Dispose();
            }
        }

        static Dictionary<Type, Action<StdfRecord>> _Dumpers = new Dictionary<Type, Action<StdfRecord>>();
        private static void DumpRecord(StdfRecord r) {
            var type = r.GetType();
            Action<StdfRecord> dumper;
            if (!_Dumpers.TryGetValue(type, out dumper)) {
                dumper = CreateDumperForType(type);
                _Dumpers[type] = dumper;
            }
            dumper(r);
        }

        /// <summary>
        /// Creates a string representation of an array
        /// </summary>
        private static string DumpArrayRepresentation<T>(T[] array) {
            if (array == null) return null;
            var builder = new StringBuilder();
            foreach (var t in array) {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(t.ToString());
            }
            return builder.ToString();
        }

        private static string DumpBitArrayRepresentation(BitArray bitArray) {
            if (bitArray == null) return null;
            return DumpArrayRepresentation(bitArray.Cast<bool>().ToArray());
        }

        //crazy codegen for building record dumpers
        private static Action<StdfRecord> CreateDumperForType(Type type) {
            var dynDumper = new DynamicMethod(String.Format("Dump{0}", type.Name), null, new[] { typeof(StdfRecord) }, typeof(Program));
            var ilgen = dynDumper.GetILGenerator();
            var record = ilgen.DeclareLocal(type);
            var array = ilgen.DeclareLocal(typeof(object[]));

            //create our array for passing into Console.WriteLine
            ilgen.Emit(OpCodes.Ldc_I4_2);
            ilgen.Emit(OpCodes.Newarr, typeof(object));
            ilgen.Emit(OpCodes.Stloc, array);
            //cast our record to the target type
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Castclass, type);
            ilgen.Emit(OpCodes.Stloc, record);
            var toString = typeof(object).GetMethod("ToString");
            var writeLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(string), typeof(object[]) });
            var dumpArray = typeof(Program).GetMethod("DumpArrayRepresentation", BindingFlags.Static | BindingFlags.NonPublic);
            var dumpBitArray = typeof(Program).GetMethod("DumpBitArrayRepresentation", BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var prop in type.GetProperties()) {
                if (prop.Name == "RecordType") continue;
                if (prop.Name == "StdfFile") continue;
                if (prop.Name == "Offset") continue;
                LocalBuilder propLocal;
                if (prop.PropertyType.IsArray || prop.PropertyType == typeof(BitArray)) {
                    //if it is an array, we'll store its string representation
                    propLocal = ilgen.DeclareLocal(typeof(string));
                }
                else {
                    propLocal = ilgen.DeclareLocal(prop.PropertyType);
                }
                var getter = prop.GetGetMethod();

                //store the value in the local
                ilgen.Emit(OpCodes.Ldloc, record);
                ilgen.Emit(OpCodes.Callvirt, getter);
                //if it's an array, get its structural representation
                if (prop.PropertyType.IsArray) {
                    //call the dump array method to get a string (or null)
                    ilgen.EmitCall(OpCodes.Call, dumpArray.MakeGenericMethod(prop.PropertyType.GetElementType()), null);
                }
                else if (prop.PropertyType == typeof(BitArray)) {
                    //call the dump array method to get a string (or null)
                    ilgen.EmitCall(OpCodes.Call, dumpBitArray, null);
                }
                ilgen.Emit(OpCodes.Stloc, propLocal);

                //store the property name in the array
                ilgen.Emit(OpCodes.Ldloc, array);
                ilgen.Emit(OpCodes.Ldc_I4_0);
                ilgen.Emit(OpCodes.Ldstr, prop.Name);
                ilgen.Emit(OpCodes.Stelem_Ref);

                //store the value in the array
                ilgen.Emit(OpCodes.Ldloc, array);
                ilgen.Emit(OpCodes.Ldc_I4_1);

                var notNullLabel = ilgen.DefineLabel();
                var storeLabel = ilgen.DefineLabel();
                //check the value for null
                if (!prop.PropertyType.IsValueType) {
                    ilgen.Emit(OpCodes.Ldloc, propLocal);
                    ilgen.Emit(OpCodes.Brtrue, notNullLabel);
                    ilgen.Emit(OpCodes.Ldstr, "[NULL]");
                    ilgen.Emit(OpCodes.Br, storeLabel);
                }
                ilgen.MarkLabel(notNullLabel);
                ilgen.Emit(OpCodes.Ldloca, propLocal);
                ilgen.Emit(OpCodes.Constrained, prop.PropertyType);
                ilgen.Emit(OpCodes.Callvirt, toString);

                //do the store operation
                ilgen.MarkLabel(storeLabel);
                ilgen.Emit(OpCodes.Stelem_Ref);

                //writeline
                ilgen.Emit(OpCodes.Ldstr, "{0}: {1}");
                ilgen.Emit(OpCodes.Ldloc, array);
                ilgen.Emit(OpCodes.Call, writeLine);
            }
            ilgen.Emit(OpCodes.Ret);
            return (Action<StdfRecord>)dynDumper.CreateDelegate(typeof(Action<StdfRecord>));
        }
    }
}
