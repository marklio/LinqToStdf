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
    /// This is the code that inspects precompile queries and determines
    /// which records and fields should be parsed.
    /// It's based on code from Jomo Fisher's blog for visiting and copying
    /// expression trees.
    /// (http://blogs.msdn.com/jomo_fisher/archive/2007/05/23/dealing-with-linq-s-immutable-expression-trees.aspx)
    /// </summary>
    static class ExpressionInspector {

        class InspectingVisitor : ExpressionVisitor
        {
            RecordsAndFields _RecordsAndFields = null;
            public RecordsAndFields InspectExpression(LambdaExpression node)
            {
                _RecordsAndFields = new RecordsAndFields();
                //first see if the node leaks any records:
                EnsureTypeWontLeakRecords(node.ReturnType);
                Visit(node);
                return _RecordsAndFields;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var type = node.Member.DeclaringType;
                if (typeof(StdfRecord).IsAssignableFrom(type))
                {
                    _RecordsAndFields.AddType(type);
                    _RecordsAndFields.AddField(type, node.Member.Name);
                }
                return base.VisitMember(node);
            }

            HashSet<Type> _CheckedTypes = new HashSet<Type>();
            void EnsureTypeWontLeakRecords(Type type)
            {
                if (type.IsPrimitive) return;
                //see if we've checked this type
                if (!_CheckedTypes.Add(type)) return;
                //see if it's a record
                if (typeof(StdfRecord).IsAssignableFrom(type)) throw new InvalidOperationException(string.Format("The compiled query can return {0} in its object graph or inheritance chain.  A compiled query can't return StdfRecords.  Just return the data you want in a new or anonymous type.", type));
                //check any generics
                if (type.IsGenericType)
                {
                    foreach (var genericType in type.GetGenericArguments())
                    {
                        EnsureTypeWontLeakRecords(genericType);
                    }
                }
                EnsureTypesWontLeakRecords(type.GetInterfaces());
                if (type.HasElementType)
                {
                    EnsureTypeWontLeakRecords(type.GetElementType());
                }
                //check publics
                EnsureTypesWontLeakRecords(from f in type.GetFields() select f.FieldType);
                EnsureTypesWontLeakRecords(from p in type.GetProperties() select p.PropertyType);
                EnsureTypesWontLeakRecords(from m in type.GetMethods() select m.ReturnType);

            }
            void EnsureTypesWontLeakRecords(IEnumerable<Type> types) {
                foreach (var type in types)
                {
                    EnsureTypeWontLeakRecords(type);
                }
            }
        }

        static public RecordsAndFields Inspect(LambdaExpression exp)
        {
            return new InspectingVisitor().InspectExpression(exp);

        }
    }
}
