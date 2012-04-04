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

namespace LinqToStdf.RecordConverting
{
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
            ILGen.Call(typeof(Array).GetMethod("Reverse", typeof(Array)));

            ILGen.BeginFinallyBlock();
            //dispose the memorystream
            ILGen.Ldloc(memoryStream);
            ILGen.Callvirt(typeof(Stream).GetMethod("Dispose"));
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
}
