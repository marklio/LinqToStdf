using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.CompiledQuerySupport {

    /// <summary>
    /// This is Jomo Fisher's Chain of responsibility solution
    /// (http://blogs.msdn.com/jomo_fisher/archive/2007/05/07/visitor-revisitted-linq-function-composablity-and-chain-of-responsibility.aspx)
    /// </summary>
    static class FuncOp {

        private class Binding<T, TResult> {

            public Func<T, TResult> Bound { get; private set; }
            public Func<Func<T, TResult>, Func<T, TResult>, T, TResult> Unbound { get; private set; }
            public Binding<T, TResult> LastBinding { get; private set; }
            public Binding(Func<T, TResult> bound,
                Func<Func<T, TResult>, Func<T, TResult>, T, TResult> unbound,
                Binding<T, TResult> lastBinding
                ) {
                Bound = bound;
                Unbound = unbound;
                LastBinding = lastBinding;
            }

            public TResult Invoke(T t) { return Bound(t); }
        }

        private class RefOf<T> {
            public T Ref { get; set; }
        }

        public static Func<T, TResult> Create<T, TResult>(Func<Func<T, TResult>, T, TResult> function) {
            var selfRef = new RefOf<Func<T, TResult>>();
            var unbound = new Func<Func<T, TResult>, Func<T, TResult>, T, TResult>((s, l, t) => function(s, t));
            selfRef.Ref = Chain(selfRef, unbound, null);
            return selfRef.Ref;
        }

        private static Func<T, TResult> Chain<T, TResult>(
            RefOf<Func<T, TResult>> selfRef,
            Func<Func<T, TResult>,
            Func<T, TResult>, T, TResult> unbound,
            Binding<T, TResult> lastBinding) {

            Func<T, TResult> last = null;
            Func<T, TResult> bound = (t) => unbound(selfRef.Ref, last, t);
            if (lastBinding != null) last = Chain(selfRef, lastBinding.Unbound, lastBinding.LastBinding);
            var binding = new Binding<T, TResult>(bound, unbound, lastBinding);

            return new Func<T, TResult>(binding.Invoke);
        }

        public static Func<T, TResult> Chain<T, TResult>(
            this Func<T, TResult> lastFunction,
            Func<Func<T, TResult>,
            Func<T, TResult>, T, TResult> selfFunction) {

            var selfRef = new RefOf<Func<T, TResult>>();
            var lastBinding = (Binding<T, TResult>)lastFunction.Target;
            selfRef.Ref = Chain<T, TResult>(selfRef, selfFunction, lastBinding);

            return selfRef.Ref;
        }
    }
}
