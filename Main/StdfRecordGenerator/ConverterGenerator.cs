// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StdfRecordGenerator
{
    /// <summary>
    /// This helper class encapsulates the generation of IL for the converters
    /// </summary>
    class ConverterGenerator
    {
        readonly ClassDeclarationSyntax _RecordClass;
        readonly List<FieldLayoutDefinition> _FieldDefinitions;

        /// <summary>
        /// Constructs a converter using the supplied il generator and the type we're converting to.
        /// </summary>
        /// <param name="ilgen">The il generator to use</param>
        /// <param name="type">The type we're converting to</param>
        /// <param name="fields">The fields we should parse (null if we should parse everything, empty if we shouldn't parse at all)</param>
        public ConverterGenerator(ClassDeclarationSyntax recordClass, List<FieldLayoutDefinition> fieldDefinitions)
        {
            _RecordClass = recordClass;
            _FieldDefinitions = (from def in fieldDefinitions orderby def.FieldIndex select def).ToList();
            for (int i = 0; i < _FieldDefinitions.Count; i++)
            {
                if (_FieldDefinitions[i].FieldIndex != i) throw new InvalidOperationException($"Non-consecutive field indexes on {recordClass.Identifier}");
            }
        }

        /// <summary>
        /// Does the work of generating the appropriate code.
        /// </summary>
        internal void GenerateConverter()
        {
            //generate the assignment nodes
            var assignments = from field in _FieldDefinitions
                                  //don't generate code for dependency properties
                              where field is not DependencyPropertyDefinition
                              //this call through reflection is icky, but marginally better than the hard-coded table
                              //we're just binding to the generic GenerateAssignment method for the field's type
                              select GenerateAssignment(field);

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
            new ConverterEmittingVisitor(_ILGen,_Type,ConverterLog.IsLogging).Visit(block);
        }

        CodeNode GenerateAssignment(FieldLayoutDefinition fieldDefinition)
        {
            var fieldType = fieldDefinition.FieldType ?? throw new InvalidOperationException("The field type for assignment is null");
            //if this is an array, defer to GenerateArrayAssignment
            if (fieldDefinition is ArrayFieldLayoutDefinition)
            {
                // TODO: catch these in GenerateArrayAssignment
                //TODO: how to do these checks?
                if (fieldType == typeof(string))
                {
                    // TODO: Accept string arrays
                    throw new InvalidOperationException(Resources.NoStringArrays);
                }
                if (fieldType == typeof(BitArray))
                {
                    throw new InvalidOperationException(Resources.NoBitArrayArrays);
                }
                return GenerateArrayAssignment(fieldDefinition);
            }

            //get the length if this is a fixed-length string
            var stringLength = -1;
            if (fieldDefinition is StringFieldLayoutDefinition stringLayout && stringLayout.Length > 0)
            {
                //TODO: check explicitly for null as error?
                stringLength = stringLayout.Length ?? -1;
            }

            //determine how we'll read the field
            CodeNode readerNode;
            if (stringLength > 0)
            {
                readerNode = new ReadFixedStringNode(stringLength);
            }
            else
            {
                readerNode = new ReadTypeNode(fieldType);
            }

            BlockNode? assignmentBlock = null;
            //if we have a property to assign to, generate the appropriate assignment statements
            if (pair.Value != null)
            {
                var assignmentNodes = new List<CodeNode>();
                //if this is optional, set us up to skip if the missing flag is set
                if (pair.Key is FlaggedFieldLayoutAttribute optionalLayout)
                {
                    assignmentNodes.Add(new SkipAssignmentIfFlagSetNode(optionalLayout.FlagIndex, optionalLayout.FlagMask));
                }
                //if we have a missing value, set us up to skip if the value matches the missing value
                else if (pair.Key.MissingValue != null && !pair.Key.PersistMissingValue)
                {
                    if (!(fieldType.IsAssignableFrom(pair.Key.MissingValue.GetType())))
                        throw new InvalidOperationException(string.Format("Missing value {0} is not assignable to {1}.", pair.Key.MissingValue, fieldType));
                    assignmentNodes.Add(new SkipAssignmentIfMissingValueNode(pair.Key.MissingValue));
                }
                //set us up to assign to the property
                assignmentNodes.Add(new AssignFieldToPropertyNode(fieldType, pair.Value));
                assignmentBlock = new BlockNode(assignmentNodes);
            }
            return new FieldAssignmentNode(fieldType, pair.Key.FieldIndex, readerNode, assignmentBlock);
        }

        CodeNode GenerateArrayAssignment(KeyValuePair<FieldLayoutAttribute, PropertyInfo> pair)
        {
            var fieldType = pair.Key.FieldType ?? throw new InvalidOperationException("The field type for assignment is null");
            bool isNibbleArray = pair.Key is NibbleArrayFieldLayoutAttribute;
            int lengthIndex = ((ArrayFieldLayoutAttribute)pair.Key).ArrayLengthFieldIndex;

            //we can skip entirely if the length field was zero
            //we'll combine this as part of the "reading" of the field
            var parseConditionNode = new SkipArrayAssignmentIfLengthIsZeroNode(lengthIndex);

            //find out if we should even parse this field
            if (pair.Value != null && !ShouldParseField(pair.Value.Name))
            {
                //we can simply return this skip node since it effectively encapsulates the length check as well
                return new SkipTypeNode(fieldType.MakeArrayType(), lengthIndex);
            }
            else
            {
                var readNode = new ReadTypeNode(fieldType.MakeArrayType(), lengthIndex, isNibble: isNibbleArray);
                BlockNode? assignmentBlock = null;
                if (pair.Value is not null)
                {
                    assignmentBlock = new BlockNode(new AssignFieldToPropertyNode(fieldType.MakeArrayType(), pair.Value));
                }
                //return a FieldAssignmentNode.  Note we're combining the parseConditionNode and the readNode.
                return new FieldAssignmentNode(fieldType.MakeArrayType(), pair.Key.FieldIndex, new BlockNode(parseConditionNode, readNode), assignmentBlock);
            }
        }
    }
}
