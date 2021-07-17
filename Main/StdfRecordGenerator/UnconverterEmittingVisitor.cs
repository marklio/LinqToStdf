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

namespace StdfRecordGenerator
{
    /*
    class UnconverterEmittingVisitor : CodeNodeVisitor
    {
        public UnconverterEmittingVisitor(ILGenerator ilGen, Type concreteType, bool enableLog=false)
        {
            ILGen = ilGen;
            ConcreteType = concreteType;
            EnableLog = enableLog;
            _ConcreteRecordLocal = ILGen.DeclareLocal(ConcreteType);
            _StartedWriting = ILGen.DeclareLocal<bool>();
            _Writer = ILGen.DeclareLocal<BinaryWriter>();
        }
        public ILGenerator ILGen { get; }
        public Type ConcreteType { get; }
        public bool EnableLog { get; }

        readonly LocalBuilder _ConcreteRecordLocal;
        readonly LocalBuilder _StartedWriting;
        readonly LocalBuilder _Writer;
        readonly Dictionary<int, LocalBuilder> _FieldLocals = new Dictionary<int, LocalBuilder>();

        void Log(string msg)
        {
            if (EnableLog)
            {
                ILGen.Log(msg);
            }
        }

        public override CodeNode VisitUnconverterShell(UnconverterShellNode node)
        {
            Log($"Unconverter for {ConcreteType}");
            //initialize the concrete record and cast the arg to it
            Log($"Creating instance");
            ILGen.Ldarg_0();
            ILGen.Castclass(ConcreteType);
            ILGen.Stloc(_ConcreteRecordLocal);

            //initialize _StartedWriting
            ILGen.Ldc_I4_0();
            ILGen.Stloc(_StartedWriting);

            //create a memory stream for writing
            Log($"Generating writer over stream");
            LocalBuilder memoryStream = ILGen.DeclareLocal<MemoryStream>();
            ILGen.Newobj<MemoryStream>();
            ILGen.Stloc(memoryStream);
            ILGen.BeginExceptionBlock();

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
            Log($"Getting data and reversing");
            ILGen.Ldloc(memoryStream);
            ILGen.Callvirt(typeof(MemoryStream).GetMethodOrThrow("ToArray"));
            var content = ILGen.DeclareLocal<byte[]>();
            ILGen.Stloc(content);
            ILGen.Ldloc(content);
            ILGen.Call(typeof(Array).GetMethodOrThrow("Reverse", typeof(Array)));

            ILGen.BeginFinallyBlock();
            //dispose the memorystream
            Log($"Cleaning up");
            ILGen.Ldloc(memoryStream);
            ILGen.Callvirt(typeof(Stream).GetMethodOrThrow("Dispose"));
            ILGen.EndExceptionBlock();

            //get the record type
            ILGen.Ldarg_0();
            ILGen.Callvirt(typeof(StdfRecord).GetProperty("RecordType")?.GetGetMethod() ?? throw new InvalidOperationException("Can't get the getter for RecordType."));

            //load the content
            ILGen.Ldloc(content);

            //get the endian
            ILGen.Ldarg_1();

            //new up a new UnknownRecord!
            Log($"returning the populated UnknownRecord");
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
                Log($"Creating new object for field {node.FieldIndex}");
                ILGen.Newobj(local.LocalType);
                ILGen.Stloc(local);
            }
            else if (local.LocalType.IsValueType)
            {
                Log($"Initializing valuetype for field {node.FieldIndex}");
                ILGen.Ldloca(local);
                ILGen.Initobj(local.LocalType);
            }
            else
            {
                Log($"Loading null for field {node.FieldIndex}");
                ILGen.Ldnull();
                ILGen.Stloc(local);
            }
            return node;
        }
        public override CodeNode VisitWriteField(WriteFieldNode node)
        {
            Log($"Writing field {node.FieldIndex}");
            //TODO: do the right kind of checks for the optional node properties

            //do any initialization (this is typically creating field locals)
            if (node.Initialization is not null) Visit (node.Initialization);

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

                Log($"Reading {node.Property.Name} for field {node.FieldIndex}");
                //get the value of the property
                ILGen.Ldloc(_ConcreteRecordLocal);
                ILGen.Callvirt(node.Property.GetGetMethod() ?? throw new InvalidOperationException("Cannot get property getter"));

                //generate the check for whether we have a value to write
                if (node.Property.PropertyType.IsValueType)
                {
                    Log($"Processing value type");
                    //it's a value type.  Check for Nullable
                    Type nullable = typeof(Nullable<>).MakeGenericType(node.FieldType);
                    if (node.Property.PropertyType == nullable)
                    {
                        Log($"Checking Nullable for value");
                        //it is nullable, check to see whether we have a value
                        //create a local for the nullable and store the value in it
                        var nullableLocal = ILGen.DeclareLocal(nullable);
                        ILGen.Stloc(nullableLocal);
                        //call .HasValue
                        ILGen.Ldloca(nullableLocal); //load address so we can call methods
                        ILGen.Callvirt(nullable.GetProperty("HasValue")?.GetGetMethod() ?? throw new InvalidOperationException("Cannot get getter for HasValue"));
                        //dup and store hasValue
                        ILGen.Dup();
                        ILGen.Stloc(hasValueLocal);

                        //if we don't have a value, branch to decide if we should be writing something anyway
                        ILGen.Brfalse(decideLabel);

                        //otherwise, get the value 
                        Log($"Getting Value");
                        ILGen.Ldloca(nullableLocal);
                        ILGen.Callvirt(nullable.GetProperty("Value")?.GetGetMethod()  ?? throw new InvalidOperationException("Cannot get getter for Value"));
                    }
                    //BUG: if we have a field that we persist AND represents the state of another field,
                    // then there should be a merge operation here (or a consistency check should fail earlier).
                    //for now, we assume consistency.
                    //store the value and branch to the write
                    ILGen.Stloc(fieldLocal);
                    ILGen.Br(doWriteLabel);
                }
                else
                {
                    Log($"Processing Reference Type");
                    //copy so we can store (remember we're dup'ing the thing returned from the property)
                    ILGen.Dup();
                    ILGen.Stloc(fieldLocal);

                    Log($"Checking For null");
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
            Log($"No value. Deciding whether to write.");

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

            if (node.WriteOperation is not null) Visit (node.WriteOperation);

            ILGen.MarkLabel(skipWritingLabel);
            Log($"Done writing.");

            //if we have an optional field index, we need to emit code to set it
            if (node.OptionalFieldIndex.HasValue)
            {
                Log($"Setting optional field bits if necessary.");
                var optionalLocal = _FieldLocals[node.OptionalFieldIndex.Value];
                var skipOptField = ILGen.DefineLabel();
                //if we have a value, skip setting the optional field
                ILGen.Ldloc(hasValueLocal);
                ILGen.Brtrue(skipOptField);
                //load the optional local and the field mask, and "or" them together
                Log($"Setting optional field bits 0x{node.OptionaFieldMask:x}");
                ILGen.Ldloc(optionalLocal);
                ILGen.Ldc_I4_S(node.OptionaFieldMask);
                ILGen.Or();
                //store the value back in the local 
                ILGen.Stloc(optionalLocal);
                ILGen.MarkLabel(skipOptField);
                Log($"Done setting optional field bits");
            }

            return node;
        }
        public override CodeNode VisitWriteFixedString(WriteFixedStringNode node)
        {
            Log($"Writing fixed string of length {node.StringLength}.");
            ILGen.Ldloc(_Writer);
            Visit(node.ValueSource);
            ILGen.Ldc_I4(node.StringLength);
            ILGen.Callvirt(typeof(BinaryWriter).GetMethodOrThrow("WriteString", typeof(string), typeof(int)));
            return node;
        }
        static readonly Dictionary<Type, MethodInfo> _WriteMethods = new Dictionary<Type, MethodInfo>();
        public override CodeNode VisitWriteType(WriteTypeNode node)
        {
            MethodInfo? writeMethod;
            if (node.IsNibble) writeMethod = typeof(BinaryWriter).GetMethodOrThrow(nameof(BinaryWriter.WriteNibbleArray), node.Type);
            else if (!_WriteMethods.TryGetValue(node.Type, out writeMethod))
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
                else if (node.Type == typeof(string[])) writeMethodName = "WriteStringArray";
                else if (node.Type == typeof(DateTime)) writeMethodName = "WriteDateTime";
                else if (node.Type == typeof(BitArray)) writeMethodName = "WriteBitArray";
                else
                {
                    throw new NotSupportedException(string.Format(Resources.UnsupportedWriterType, node.Type));
                }
                writeMethod = typeof(BinaryWriter).GetMethodOrThrow(writeMethodName, node.Type);
                _WriteMethods[node.Type] = writeMethod;
            }

            ILGen.Ldloc(_Writer);
            Visit(node.ValueSource);
            Log($"Writing with {writeMethod.Name}.");
            ILGen.Callvirt(writeMethod);
            return node;
        }
        public override CodeNode VisitLoadMissingValue(LoadMissingValueNode node)
        {
            Log($"Loading missing value {node.MissingValue}.");
            ILGen.Ldc(node.MissingValue, node.Type);
            return node;
        }
        public override CodeNode VisitLoadFieldLocal(LoadFieldLocalNode node)
        {
            Log($"Loading local var for field {node.FieldIndex}.");
            ILGen.Ldloc(_FieldLocals[node.FieldIndex]);
            return node;
        }
        public override CodeNode VisitLoadNull(LoadNullNode node)
        {
            Log($"Loading null.");
            ILGen.Ldnull();
            return node;
        }
        public override CodeNode VisitThrowInvalidOperation(ThrowInvalidOperationNode node)
        {
            Log($"Throwing invalid operation {node.Message}.");
            ILGen.Ldstr(node.Message);
            ILGen.Newobj<InvalidOperationException>(typeof(string));
            ILGen.Throw();
            return node;
        }
        public override CodeNode VisitValidateSharedLengthLocal(ValidateSharedLengthLocalNode node)
        {
            Log($"Validationg shared length with index {node.LengthFieldIndex}.");
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
            Log($"Array is null (treat as zero length).");

            ILGen.Ldc_I4_0();
            ILGen.Br(compareLength);

            //array is not null. get its real length
            ILGen.MarkLabel(notNull);
            Log($"Getting array length.");
            ILGen.Ldloc(arrayLocal);
            ILGen.Ldlen();

            ILGen.MarkLabel(compareLength);
            Log($"Comparing length array length.");
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
            Log($"Lengths aren't equal. Throw.");

            ILGen.Ldstr(string.Format(Resources.SharedLengthViolation, node.LengthFieldIndex));
            ILGen.Newobj<InvalidOperationException>(typeof(string));
            ILGen.Throw();
            ILGen.MarkLabel(dontThrow);
            Log($"Done.");
            return node;
        }
        public override CodeNode VisitSetLengthLocal(SetLengthLocalNode node)
        {
            Log($"Setting length for field {node.LengthFieldIndex}.");
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
            Log($"Array is null (treat as zero length).");
            ILGen.Ldc_I4_0();
            ILGen.Br(storeLength);

            //array is not null. get its real length
            ILGen.MarkLabel(notNull);
            Log($"Getting array length.");
            ILGen.Ldloc(arrayFieldLocal);
            ILGen.Ldlen();
            ILGen.Br(storeLength);

            //store the length
            ILGen.MarkLabel(storeLength);
            Log($"Storing array length.");
            ILGen.Stloc(lengthFieldLocal);
            return node;
        }
    }
    */
}
