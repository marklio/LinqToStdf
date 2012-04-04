// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LinqToStdf.Attributes;

namespace LinqToStdf.RecordConverting
{
    /// <summary>
    /// This helper class encapsulates the generation of IL for the converters
    /// </summary>
    class ConverterGenerator
    {
        ILGenerator _ILGen;
        Type _Type;
        HashSet<string> _Fields;

        /// <summary>
        /// Constructs a converter using the supplied il generator and the type we're converting to.
        /// </summary>
        /// <param name="ilgen">The il generator to use</param>
        /// <param name="type">The type we're converting to</param>
        /// <param name="fields">The fields we should parse (null if we should parse everything, empty if we shouldn't parse at all)</param>
        public ConverterGenerator(ILGenerator ilgen, Type type, HashSet<string> fields)
        {
            if (ilgen == null) throw new ArgumentNullException("ilgen");
            if (type == null) throw new ArgumentNullException("type");
            _ILGen = ilgen;
            _Type = type;
            _Fields = fields;
        }

        bool ShouldParseField(string field)
        {
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

        static List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>> GetFieldLayoutsAndAssignments(Type recordType)
        {
            //get the list
            var attributes = from a in ((Type)recordType).GetCustomAttributes(typeof(StdfFieldLayoutAttribute), true).Cast<StdfFieldLayoutAttribute>()
                             orderby a.FieldIndex
                             select a;
            var list = new List<StdfFieldLayoutAttribute>(attributes);
            //make sure they are consecutive
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].FieldIndex != i) throw new NonconsecutiveFieldIndexException(recordType);
            }
            var withPropInfo = from l in list
                               select new KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>(
                                          l,
                                          (l.AssignTo == null) ? null : ((Type)recordType).GetProperty(l.AssignTo));

            return new List<KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo>>(withPropInfo);
        }

        CodeNode GenerateAssignment<T>(KeyValuePair<StdfFieldLayoutAttribute, PropertyInfo> pair)
        {
            //if this is an array, defer to GenerateArrayAssignment
            if (pair.Key is StdfArrayLayoutAttribute)
            {
                if (typeof(T) == typeof(string))
                {
                    throw new InvalidOperationException(Resources.NoStringArrays);
                }
                return GenerateArrayAssignment<T>(pair);
            }

            //get the length if this is a fixed-length string
            StdfStringLayoutAttribute stringLayout = pair.Key as StdfStringLayoutAttribute;
            var stringLength = -1;
            if (stringLayout != null && stringLayout.Length > 0)
            {
                stringLength = stringLayout.Length;
            }

            //just skip this field if we have an assignment, but shouldn't be parsing it
            if (pair.Value != null && !ShouldParseField(pair.Value.Name))
            {
                if (stringLength > 0)
                {
                    return new SkipRawBytesNode(stringLength);
                }
                else
                {
                    return new SkipTypeNode<T>();
                }
            }

            //determine how we'll read the field
            CodeNode readerNode;
            if (stringLength > 0)
            {
                readerNode = new ReadFixedStringNode(stringLength);
            }
            else
            {
                readerNode = new ReadTypeNode<T>();
            }

            BlockNode assignmentBlock = null;
            //if we have a property to assign to, generate the appropriate assignment statements
            if (pair.Value != null)
            {
                var assignmentNodes = new List<CodeNode>();
                //if this is optional, set us up to skip if the missing flag is set
                StdfOptionalFieldLayoutAttribute optionalLayout = pair.Key as StdfOptionalFieldLayoutAttribute;
                if (optionalLayout != null)
                {
                    assignmentNodes.Add(new SkipAssignmentIfFlagSetNode(optionalLayout.FlagIndex, optionalLayout.FlagMask));
                }
                //if we have a missing value, set us up to skip if the value matches the missing value
                else if (pair.Key.MissingValue != null)
                {
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
            if (pair.Value != null && !ShouldParseField(pair.Value.Name))
            {
                //we can simply return this skip node since it effectively encapsulates the length check as well
                return new SkipTypeNode<T[]>(lengthIndex);
            }
            else
            {
                var readNode = new ReadTypeNode<T[]>(lengthIndex, isNibble: isNibbleArray);
                BlockNode assignmentBlock = null;
                if (pair.Value != null)
                {
                    assignmentBlock = new BlockNode(new AssignFieldToPropertyNode<T[]>(pair.Value));
                }
                //return a FieldAssignmentNode.  Note we're combining the parseConditionNode and the readNode.
                return new FieldAssignmentNode<T[]>(pair.Key.FieldIndex, new BlockNode(parseConditionNode, readNode), assignmentBlock);
            }
        }
    }
}
