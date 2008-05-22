using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using LinqToStdf.CompiledQuerySupport;

namespace LinqToStdf {
    public static class CompiledQuery {
        public static Func<string, TResult> Compile<TResult>(Expression<Func<StdfFile, TResult>> query) {
            return Compile(null, query);
        }

        public static Func<string, TResult> Compile<TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, TResult>> query) {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path) => {
                StdfFile stdf;
                if (factory == null) {
                    stdf = new StdfFile(path, false, rnf) { EnableCaching = false };
                    factory = stdf.ConverterFactory;
                }
                else {
                    stdf = new StdfFile(path, factory) { EnableCaching = false };
                }
                if (stdfFileInit != null) stdfFileInit(stdf);
                return compiled(stdf);
            };
        }

        public static Func<string, T1, TResult> Compile<T1, TResult>(Expression<Func<StdfFile, T1, TResult>> query) {
            return Compile(null, query);
        }

        public static Func<string, T1, TResult> Compile<T1, TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, T1, TResult>> query) {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path, t1) => {
                StdfFile stdf;
                if (factory == null) {
                    stdf = new StdfFile(path, false, rnf) { EnableCaching = false };
                    factory = stdf.ConverterFactory;
                }
                else {
                    stdf = new StdfFile(path, factory) { EnableCaching = false };
                }
                if (stdfFileInit != null) stdfFileInit(stdf);
                return compiled(stdf, t1);
            };
        }

        public static Func<string, T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<StdfFile, T1, T2, TResult>> query) {
            return Compile(null, query);
        }

        public static Func<string, T1, T2, TResult> Compile<T1, T2, TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, T1, T2, TResult>> query) {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path, t1, t2) => {
                StdfFile stdf;
                if (factory == null) {
                    stdf = new StdfFile(path, false, rnf) { EnableCaching = false };
                    factory = stdf.ConverterFactory;
                }
                else {
                    stdf = new StdfFile(path, factory) { EnableCaching = false };
                }
                if (stdfFileInit != null) stdfFileInit(stdf);
                return compiled(stdf, t1, t2);
            };
        }

        public static Func<string, T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Expression<Func<StdfFile, T1, T2, T3, TResult>> query) {
            return Compile(null, query);
        }

        public static Func<string, T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, T1, T2, T3, TResult>> query) {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path, t1, t2, t3) => {
                StdfFile stdf;
                if (factory == null) {
                    stdf = new StdfFile(path, false, rnf) { EnableCaching = false };
                    factory = stdf.ConverterFactory;
                }
                else {
                    stdf = new StdfFile(path, factory) { EnableCaching = false };
                }
                if (stdfFileInit != null) stdfFileInit(stdf);
                return compiled(stdf, t1, t2, t3);
            };
        }
    }
}
