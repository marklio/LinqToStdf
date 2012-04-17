// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LinqToStdf.Attributes;

namespace LinqToStdf.RecordConverting {
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
        internal void GenerateConverter() {
            List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> fields = GetFieldLayoutsAndAssignments(_Type);
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

            //generate the assignment nodes
            var assignments = from pair in fields
                              //don't generate code for dependency properties
                              where !(pair.Key is StdfDependencyProperty)
                              //this call through reflection is icky, but marginally better than the hard-coded table
                              //we're just binding to the generic GenerateAssignment method for the field's type
                              select GenerateAssignment(pair);

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
            new ConverterEmittingVisitor() {
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

        CodeNode GenerateAssignment(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
            var fieldType = pair.Key.FieldType;
            //if this is an array, defer to GenerateArrayAssignment
            if (pair.Key is StdfArrayLayoutAttribute) {
                // TODO: Why do we need these fieldType checks at all?
                if (fieldType == typeof(string)) {
                    // TODO: Accept string arrays
                    throw new InvalidOperationException(Resources.NoStringArrays);
                }
                if (fieldType == typeof(BitArray)) {
                    throw new InvalidOperationException(Resources.NoBitArrayArrays);
                }
                return GenerateArrayAssignment(pair);
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
                    return new SkipTypeNode(fieldType);
                }
            }

            //determine how we'll read the field
            CodeNode readerNode;
            if (stringLength > 0) {
                readerNode = new ReadFixedStringNode(stringLength);
            }
            else {
                readerNode = new ReadTypeNode(fieldType);
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
                    if (!(fieldType.IsAssignableFrom(pair.Key.MissingValue.GetType())))
                        throw new InvalidOperationException(string.Format("Missing value {0} is not assignable to {1}.", pair.Key.MissingValue, fieldType));
                    assignmentNodes.Add(new SkipAssignmentIfMissingValueNode(pair.Key.MissingValue, pair.Key.AllowMissingValue));
                }
                //set us up to assign to the property
                assignmentNodes.Add(new AssignFieldToPropertyNode(fieldType, pair.Value));
                assignmentBlock = new BlockNode(assignmentNodes);
            }
            return new FieldAssignmentNode(fieldType, pair.Key.FieldIndex, readerNode, assignmentBlock);
        }

        CodeNode GenerateArrayAssignment(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair) {
            var fieldType = pair.Key.FieldType;
            bool isNibbleArray = pair.Key is StdfNibbleArrayLayoutAttribute;
            int lengthIndex = ((StdfArrayLayoutAttribute)pair.Key).ArrayLengthFieldIndex;

            //we can skip entirely if the length field was zero
            //we'll combine this as part of the "reading" of the field
            var parseConditionNode = new SkipArrayAssignmentIfLengthIsZeroNode(lengthIndex);

            //find out if we should even parse this field
            if (pair.Value != null && !ShouldParseField(pair.Value.Name)) {
                //we can simply return this skip node since it effectively encapsulates the length check as well
                return new SkipTypeNode(fieldType.MakeArrayType(), lengthIndex);
            }
            else {
                var readNode = new ReadTypeNode(fieldType.MakeArrayType(), lengthIndex, isNibble: isNibbleArray);
                BlockNode assignmentBlock = null;
                if (pair.Value != null) {
                    assignmentBlock = new BlockNode(new AssignFieldToPropertyNode(fieldType.MakeArrayType(), pair.Value));
                }
                //return a FieldAssignmentNode.  Note we're combining the parseConditionNode and the readNode.
                return new FieldAssignmentNode(fieldType.MakeArrayType(), pair.Key.FieldIndex, new BlockNode(parseConditionNode, readNode), assignmentBlock);
            }
        }
    }
}
