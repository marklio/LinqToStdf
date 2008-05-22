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

        /// <summary>
        /// Returns a delegate capable of inspecting an expression tree
        /// and returning a <see cref="RecordsAndFields"/> object populated
        /// with the records and fields needed to execute the query.
        /// </summary>
        static public Func<Expression, RecordsAndFields> Inspect = FuncOp.Create<Expression, RecordsAndFields>(
            (self, expr) => {
                switch (expr.NodeType) {
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.Convert:
                    case ExpressionType.TypeAs:
                    case ExpressionType.TypeIs: {
                            var tbe = (TypeBinaryExpression)expr;
                            var rnf = self(tbe.Expression);
                            rnf.AddType(tbe.TypeOperand);
                            return rnf;
                        }
                    case ExpressionType.Conditional:
                        var ce = (ConditionalExpression)expr;
                        return self(ce.Test) + self(ce.IfTrue) + self(ce.IfFalse);
                    case ExpressionType.MemberAccess: {
                            var ma = (MemberExpression)expr;
                            var rnf = self(ma.Expression);
                            if (ma.Member.MemberType == System.Reflection.MemberTypes.Property
                                && typeof(StdfRecord).IsAssignableFrom(ma.Member.DeclaringType)) {
                                rnf.AddField(ma.Member.DeclaringType, ma.Member.Name);
                            }
                            return rnf;
                        }
                    case ExpressionType.Call: {
                            var mce = (MethodCallExpression)expr;
                            var rnf = self.VisitExpressionList(mce.Arguments);
                            if (mce.Object != null) rnf += self(mce.Object);
                            return rnf;
                        }
                    case ExpressionType.Lambda:
                        var le = (LambdaExpression)expr;
                        return self(le.Body);
                    case ExpressionType.New:
                        var ne = (NewExpression)expr;
                        return self.VisitExpressionList(ne.Arguments);
                    case ExpressionType.NewArrayInit:
                    case ExpressionType.NewArrayBounds:
                        var na = (NewArrayExpression)expr;
                        return self.VisitExpressionList(na.Expressions);
                    case ExpressionType.Invoke:
                        var inv = (InvocationExpression)expr;
                        return self(inv.Expression) + self.VisitExpressionList(inv.Arguments);
                    case ExpressionType.MemberInit:
                        var mi = (MemberInitExpression)expr;
                        return self(mi.NewExpression) + self.VisitBindingList(mi.Bindings);
                    case ExpressionType.ListInit:
                        var li = (ListInitExpression)expr;
                        return self(li.NewExpression) + self.VisitElementInitializerList(li.Initializers);
                }

                if (expr.IsBinary()) {
                    var b = (BinaryExpression)expr;
                    return self(b.Left) + self(b.Right);
                }
                else if (expr.IsUnary()) {
                    var u = (UnaryExpression)expr;
                    return self(u.Operand);
                }
                return new RecordsAndFields();
            }
        );

        public static bool IsBinary(this Expression expr) {
            return expr is BinaryExpression;
        }

        public static bool IsUnary(this Expression expr) {
            return expr is UnaryExpression;
        }

        public static RecordsAndFields VisitBinding(this Func<Expression, RecordsAndFields> self, MemberBinding b) {
            switch (b.BindingType) {
                case MemberBindingType.Assignment:
                    return self.VisitMemberAssignment((MemberAssignment)b);
                case MemberBindingType.MemberBinding:
                    return self.VisitMemberMemberBinding((MemberMemberBinding)b);
            }
            return self.VisitMemberListBinding((MemberListBinding)b);

        }

        public static RecordsAndFields VisitMemberAssignment(this Func<Expression, RecordsAndFields> self, MemberAssignment assignment) {
            return self(assignment.Expression);
        }

        public static RecordsAndFields VisitMemberMemberBinding(this Func<Expression, RecordsAndFields> self, MemberMemberBinding binding) {
            return self.VisitBindingList(binding.Bindings);
        }

        public static RecordsAndFields VisitMemberListBinding(this Func<Expression, RecordsAndFields> self, MemberListBinding binding) {
            return self.VisitElementInitializerList(binding.Initializers);
        }

        public static RecordsAndFields VisitElementInitializer(this Func<Expression, RecordsAndFields> self, ElementInit initializer) {
             return self.VisitExpressionList(initializer.Arguments);
        }

        public static RecordsAndFields VisitExpressionList(this Func<Expression, RecordsAndFields> self, ReadOnlyCollection<Expression> original) {
            return VisitList(original, e => self(e));
        }

        public static RecordsAndFields VisitBindingList(this Func<Expression, RecordsAndFields> self, ReadOnlyCollection<MemberBinding> original) {

            return VisitList(original, e => self.VisitBinding(e));

        }

        public static RecordsAndFields VisitElementInitializerList(this Func<Expression, RecordsAndFields> self, ReadOnlyCollection<ElementInit> original) {

            return VisitList(original, e => self.VisitElementInitializer(e));

        }

        private static RecordsAndFields VisitList<T>(ReadOnlyCollection<T> original, Func<T, RecordsAndFields> op) {
            RecordsAndFields @new = new RecordsAndFields();
            for (int i = 0, n = original.Count; i < n; i++) {
                @new += op(original[i]);
            }
            return @new;
        }
    }
}
