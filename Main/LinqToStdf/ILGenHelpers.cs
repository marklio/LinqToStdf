// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace LinqToStdf {

    /// <summary>
    /// Contains extension methods useful for IL Generation.
    /// It provides strongly-typed methods corresponding to the 
    /// IL OpCodes, which makes things much more readable.
    /// </summary>
    static class ILGenHelpers {
        static readonly MethodInfo _LogMethod = typeof(RecordConverting.ConverterLog).GetMethodOrThrow(nameof(RecordConverting.ConverterLog.Log), BindingFlags.Static | BindingFlags.Public);

        public static void Log(this ILGenerator ilgen, string msg)
        {
            ilgen.Ldstr(msg);
            ilgen.Call(_LogMethod);
        }

        /// <seealso cref="OpCodes.Ret"/>
        public static void Ret(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ret);
        }

        /// <seealso cref="OpCodes.Pop"/>
        public static void Pop(this ILGenerator ilgen)
        {
            ilgen.Emit(OpCodes.Pop);
        }

        /// <seealso cref="OpCodes.Stloc"/>
        public static void Stloc(this ILGenerator ilgen, LocalBuilder local) {
            ilgen.Emit(OpCodes.Stloc, local);
        }

        /// <seealso cref="OpCodes.Ldloc"/>
        public static void Ldloc(this ILGenerator ilgen, LocalBuilder local) {
            ilgen.Emit(OpCodes.Ldloc, local);
        }

        /// <seealso cref="OpCodes.Ldloca"/>
        public static void Ldloca(this ILGenerator ilgen, LocalBuilder local) {
            ilgen.Emit(OpCodes.Ldloca, local);
        }

        /// <seealso cref="OpCodes.Ldarga"/>
        public static void Ldarga(this ILGenerator ilgen, int argIndex) {
            ilgen.Emit(OpCodes.Ldarga, argIndex);
        }

        /// <seealso cref="OpCodes.Ldarg_0"/>
        public static void Ldarg_0(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldarg_0);
        }

        /// <seealso cref="OpCodes.Ldarg_1"/>
        public static void Ldarg_1(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldarg_1);
        }

        /// <summary>
        /// Loads a constant of a particular type
        /// </summary>
        /// <remarks>
        /// <para>
        /// This helper helps to unify the code for several different situations
        /// by defining a pseudo-opcode that resolves to an appropriate
        /// actual opcode for a given constant value.
        /// </para>
        /// <para>
        /// Not all types are supported.  NotSupportedException will be thrown
        /// for unsupported types.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">The type of constant to load</typeparam>
        /// <param name="ilgen">The ILGenerator</param>
        /// <param name="value">The value of the constant to load</param>
        public static void Ldc<T>(this ILGenerator ilgen, object value)
        {
            ilgen.Ldc(value, typeof(T));
        }

        public static void Ldc(this ILGenerator ilgen, object value, Type type)
        {
            if (type == typeof(byte))
            {
                ilgen.Ldc_I4_S((byte)value);
            }
            else if (type == typeof(sbyte))
            {
                ilgen.Ldc_I4_S((sbyte)value);
            }
            else if (type == typeof(ushort))
            {
                ilgen.Ldc_I4((int)(ushort)value);
                ilgen.Conv_I2();
            }
            else if (type == typeof(short))
            {
                ilgen.Ldc_I4((int)(short)value);
                ilgen.Conv_I2();
            }
            else if (type == typeof(uint))
            {
                ilgen.Ldc_I4((uint)value);
            }
            else if (type == typeof(int))
            {
                ilgen.Ldc_I4((int)value);
            }
            else if (type == typeof(ulong))
            {
                ilgen.Ldc_I8((ulong)value);
            }
            else if (type == typeof(long))
            {
                ilgen.Ldc_I8((long)value);
            }
            else if (type == typeof(float))
            {
                ilgen.Ldc_R4((float)value);
            }
            else if (type == typeof(double))
            {
                ilgen.Ldc_R8((double)value);
            }
            else if (type == typeof(string))
            {
                ilgen.Ldstr((string)value);
            }
            else if (type == typeof(DateTime))
            {
                //we only support loading the Epoch
                if ((DateTime)value != Attributes.TimeFieldLayoutAttribute.Epoch)
                {
                    //TODO:resource
                    throw new NotSupportedException("we can only load the Epoch as a \"constant\"");
                }
                ilgen.Ldsfld(typeof(Attributes.TimeFieldLayoutAttribute).GetField(nameof(Attributes.TimeFieldLayoutAttribute.Epoch), BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException("Could not find Epoch field."));
            }
            else
            {
                throw new NotSupportedException(string.Format(Resources.CodeGen_UnsupportedGenericLdc, type));
            }
        }

        public static void Ldsfld(this ILGenerator ilgen, FieldInfo field)
        {
            ilgen.Emit(OpCodes.Ldsfld, field);
        }

        /// <seealso cref="OpCodes.Ldc_I4"/>
        public static void Ldc_I4(this ILGenerator ilgen, int value) {
            ilgen.Emit(OpCodes.Ldc_I4, value);
        }

        /// <seealso cref="OpCodes.Ldc_I4"/>
        public static void Ldc_I4(this ILGenerator ilgen, uint value) {
            ilgen.Emit(OpCodes.Ldc_I4, value);
        }

        /// <seealso cref="OpCodes.Ldc_I8"/>
        public static void Ldc_I8(this ILGenerator ilgen, long value) {
            ilgen.Emit(OpCodes.Ldc_I8, value);
        }

        /// <seealso cref="OpCodes.Ldc_I8"/>
        public static void Ldc_I8(this ILGenerator ilgen, ulong value) {
            ilgen.Emit(OpCodes.Ldc_I8, value);
        }

        /// <seealso cref="OpCodes.Ldc_I4_0"/>
        public static void Ldc_I4_0(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldc_I4_0);
        }

        /// <seealso cref="OpCodes.Ldc_I4_1"/>
        public static void Ldc_I4_1(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldc_I4_1);
        }

        /// <seealso cref="OpCodes.Ldc_I4_2"/>
        public static void Ldc_I4_2(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldc_I4_2);
        }

        /// <seealso cref="OpCodes.Ldc_I4_3"/>
        public static void Ldc_I4_3(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldc_I4_3);
        }

        /// <seealso cref="OpCodes.Ldc_I4_4"/>
        public static void Ldc_I4_4(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldc_I4_4);
        }

        /// <seealso cref="OpCodes.Ldc_I4_S"/>
        public static void Ldc_I4_S(this ILGenerator ilgen, byte value) {
            ilgen.Emit(OpCodes.Ldc_I4_S, value);
        }

        /// <seealso cref="OpCodes.Ldc_I4_S"/>
        public static void Ldc_I4_S(this ILGenerator ilgen, sbyte value) {
            ilgen.Emit(OpCodes.Ldc_I4_S, value);
        }

        /// <seealso cref="OpCodes.Ldc_R4"/>
        public static void Ldc_R4(this ILGenerator ilgen, float value) {
            ilgen.Emit(OpCodes.Ldc_R4, value);
        }

        /// <seealso cref="OpCodes.Ldc_R8"/>
        public static void Ldc_R8(this ILGenerator ilgen, double value) {
            ilgen.Emit(OpCodes.Ldc_R8, value);
        }

        /// <seealso cref="OpCodes.Ldstr"/>
        public static void Ldstr(this ILGenerator ilgen, string value) {
            ilgen.Emit(OpCodes.Ldstr, value);
        }

        /// <seealso cref="OpCodes.Ldnull"/>
        public static void Ldnull(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldnull);
        }

        public static void Ldtoken(this ILGenerator ilgen, Type type) {
            ilgen.Emit(OpCodes.Ldtoken, type);
        }

        /// <seealso cref="OpCodes.Ceq"/>
        public static void Ceq(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ceq);
        }

        /// <seealso cref="OpCodes.Stelem_I1"/>
        public static void Stelem_I1(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Stelem_I1);
        }

        /// <seealso cref="OpCodes.Stelem_I2"/>
        public static void Stelem_I2(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Stelem_I2);
        }

        /// <seealso cref="OpCodes.Stelem_I4"/>
        public static void Stelem_I4(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Stelem_I4);
        }

        /// <seealso cref="OpCodes.Stelem_I8"/>
        public static void Stelem_I8(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Stelem_I8);
        }

        /// <seealso cref="OpCodes.Stelem_R4"/>
        public static void Stelem_R4(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Stelem_R4);
        }

        /// <seealso cref="OpCodes.Stelem_R8"/>
        public static void Stelem_R8(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Stelem_R8);
        }

        /// <seealso cref="OpCodes.Br"/>
        public static void Br(this ILGenerator ilgen, Label label) {
            ilgen.Emit(OpCodes.Br, label);
        }

        /// <seealso cref="OpCodes.Brtrue"/>
        public static void Brtrue(this ILGenerator ilgen, Label label) {
            ilgen.Emit(OpCodes.Brtrue, label);
        }

        /// <seealso cref="OpCodes.Brfalse"/>
        public static void Brfalse(this ILGenerator ilgen, Label label) {
            ilgen.Emit(OpCodes.Brfalse, label);
        }

        /// <seealso cref="OpCodes.Bge"/>
        public static void Bge(this ILGenerator ilgen, Label label) {
            ilgen.Emit(OpCodes.Bge, label);
        }

        /// <seealso cref="OpCodes.Throw"/>
        public static void Throw(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Throw);
        }

        /// <seealso cref="OpCodes.Castclass"/>
        public static void Castclass(this ILGenerator ilgen, Type type) {
            ilgen.Emit(OpCodes.Castclass, type);
        }

        /// <seealso cref="OpCodes.Conv_I"/>
        public static void Conv_I(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Conv_I);
        }

        /// <seealso cref="OpCodes.Conv_I2"/>
        public static void Conv_I2(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Conv_I2);
        }

        /// <seealso cref="OpCodes.Conv_I4"/>
        public static void Conv_I4(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Conv_I4);
        }

        /// <seealso cref="OpCodes.Conv_U2"/>
        public static void Conv_U2(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Conv_U2);
        }

        /// <seealso cref="OpCodes.And"/>
        public static void And(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.And);
        }

        /// <seealso cref="OpCodes.Add"/>
        public static void Add(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Add);
        }

        /// <seealso cref="OpCodes.Or"/>
        public static void Or(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Or);
        }

        /// <seealso cref="OpCodes.Dup"/>
        public static void Dup(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Dup);
        }

        /// <seealso cref="OpCodes.Shr"/>
        public static void Shr(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Shr);
        }

        /// <seealso cref="OpCodes.Ldlen"/>
        public static void Ldlen(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Ldlen);
        }

        /// <seealso cref="OpCodes.Nop"/>
        public static void Nop(this ILGenerator ilgen) {
            ilgen.Emit(OpCodes.Nop);
        }

        /// <seealso cref="OpCodes.Newobj"/>
        public static void Newobj<T>(this ILGenerator ilgen, params Type[] parameters) {
            Newobj(ilgen, typeof(T), parameters);
        }

        /// <seealso cref="OpCodes.Newobj"/>
        public static void Newobj(this ILGenerator ilgen, Type type, params Type[] parameters) {
            ilgen.Emit(OpCodes.Newobj, type.GetConstructor(parameters) ?? throw new InvalidOperationException("Could not find constructor"));
        }

        /// <seealso cref="OpCodes.Newarr"/>
        public static void Newarr<T>(this ILGenerator ilgen) {
            Newarr(ilgen, typeof(T));
        }

        public static void Constrained<T>(this ILGenerator ilgen) {
            ilgen.Constrained(typeof(T));
        }

        public static void Constrained(this ILGenerator ilgen, Type type) {
            ilgen.Emit(OpCodes.Constrained, type);
        }

        public static void Box<T>(this ILGenerator ilgen) {
            ilgen.Box(typeof(T));
        }

        public static void Box(this ILGenerator ilgen, Type type) {
            ilgen.Emit(OpCodes.Box, type);
        }

        /// <seealso cref="OpCodes.Initobj"/>
        public static void Initobj(this ILGenerator ilgen, Type type) {
            ilgen.Emit(OpCodes.Initobj, type);
        }

        /// <seealso cref="OpCodes.Newarr"/>
        public static void Newarr(this ILGenerator ilgen, Type type) {
            ilgen.Emit(OpCodes.Newarr, type);
        }

        /// <summary>
        /// Generic version of DeclareLobal
        /// </summary>
        /// <typeparam name="T">The type of the local</typeparam>
        public static LocalBuilder DeclareLocal<T>(this ILGenerator ilgen) {
            return ilgen.DeclareLocal(typeof(T));
        }

        /// <seealso cref="OpCodes.Callvirt"/>
        public static void Callvirt(this ILGenerator ilgen, MethodInfo methodInfo) {
            ilgen.Emit(OpCodes.Callvirt, methodInfo);
        }

        /// <seealso cref="OpCodes.Call"/>
        public static void Call(this ILGenerator ilgen, MethodInfo methodInfo)
        {
            ilgen.Emit(OpCodes.Call, methodInfo);
        }

        /// <summary>
        /// Overload for <see cref="Type.GetMethod(string, Type[])">Type.GetMethod</see> that allows a params array
        /// for the parameters, so you don't have to construct an array in the code
        /// </summary>
        public static MethodInfo? GetMethod(this Type type, string methodName, params Type[] parameters) {
            return type.GetMethod(methodName, parameters);
        }
        public static MethodInfo GetMethodOrThrow(this Type type, string methodName, params Type[] parameters) => type.GetMethod(methodName, parameters) ?? throw new InvalidOperationException($"Could not find the {methodName} method on {type.Name}");
        public static MethodInfo GetMethodOrThrow(this Type type, string methodName, BindingFlags flags) => type.GetMethod(methodName, flags) ?? throw new InvalidOperationException($"Could not find the {methodName} method on {type.Name}");
    }
}
