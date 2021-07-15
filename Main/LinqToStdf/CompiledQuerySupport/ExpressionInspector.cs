// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToStdf.CompiledQuerySupport {

    /// <summary>
    /// This is the class that inspects precompile queries and determines
    /// which records and fields should be parsed.
    /// </summary>
    static class ExpressionInspector {

        /// <summary>
        /// This visitor processes a query (in the form of a LambdaExpression) and
        /// ensure it won't leak concrete record types and tracks the records and
        /// fields used in the query so that it can optimize the converters.
        /// </summary>
        class InspectingVisitor : ExpressionVisitor
        {
            /// <summary>
            /// The records and fields used in the query
            /// </summary>
            RecordsAndFields? _RecordsAndFields = null;

            /// <summary>
            /// Inspects a query, ensuring it won't leak records and calculating the
            /// records and fields it uses.
            /// </summary>
            public RecordsAndFields InspectExpression(LambdaExpression node)
            {
                _RecordsAndFields = new RecordsAndFields();
                //first see if the node leaks any records:
                EnsureTypeWontLeakRecords(node.ReturnType);
                //visit the tree
                Visit(node);
                return _RecordsAndFields;
            }

            /// <summary>
            /// This gets called for each member access.
            /// </summary>
            protected override Expression VisitMember(MemberExpression node)
            {
                if (_RecordsAndFields is null) throw new InvalidOperationException("Must be called in the context of an expression inspection");
                //Get the type that declares the member
                Type type = node.Member.DeclaringType ?? throw new InvalidOperationException("Can't get DeclaringType of member");
                //if it is an StdfRecord, track the field
                if (typeof(StdfRecord).IsAssignableFrom(type))
                {
                    _RecordsAndFields.AddField(type, node.Member.Name);
                }
                return base.VisitMember(node);
            }

            /// <summary>
            /// This is the set of types we've checked to reduce duplication
            /// and prevent following circular references
            /// </summary>
            readonly HashSet<Type> _CheckedTypes = new HashSet<Type>();

            /// <summary>
            /// Throws if the type, its interfaces, or any generic parameters leak stdf records
            /// </summary>
            void EnsureTypeWontLeakRecords(Type type)
            {
                //TODO: think about whether we need to go up the base type chain to check for interfaces.
                //This depends on a) whether we care that much, and b) whether GetFields/etc. return aggregated data.

                //if it is a primitive, we don't care about it
                if (type.IsPrimitive) return;
                //see if we've checked this type.  Return if we have.
                if (!_CheckedTypes.Add(type)) return;
                //see if it's a record
                if (typeof(StdfRecord).IsAssignableFrom(type)) throw new InvalidOperationException(string.Format("The compiled query can return {0} in its object graph or inheritance chain.  A compiled query can't return StdfRecords.  Just return the data you want in a new or anonymous type.", type));
                //check any generic arguments
                if (type.IsGenericType)
                {
                    foreach (var genericType in type.GetGenericArguments())
                    {
                        EnsureTypeWontLeakRecords(genericType);
                    }
                }
                //check any interfaces
                EnsureTypesWontLeakRecords(type.GetInterfaces());
                //check any element type (for arrays, pointers, etc.)
                if (type.HasElementType)
                {
                    EnsureTypeWontLeakRecords(type.GetElementType() ?? throw new InvalidOperationException("Could not get ElementType"));
                }
                //check public fields/properties/methods
                EnsureTypesWontLeakRecords(from f in type.GetFields() select f.FieldType);
                EnsureTypesWontLeakRecords(from p in type.GetProperties() select p.PropertyType);
                EnsureTypesWontLeakRecords(from m in type.GetMethods() select m.ReturnType);

            }

            /// <summary>
            /// helper that will ensure collections don't leak
            /// </summary>
            void EnsureTypesWontLeakRecords(IEnumerable<Type> types) {
                foreach (var type in types)
                {
                    EnsureTypeWontLeakRecords(type);
                }
            }
        }

        /// <summary>
        /// Wraps the inspection of a lambda expression for use in CompiledQuery
        /// </summary>
        static public RecordsAndFields Inspect(LambdaExpression exp)
        {
            return new InspectingVisitor().InspectExpression(exp);

        }
    }
}
