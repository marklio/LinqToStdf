// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using LinqToStdf.CompiledQuerySupport;
using LinqToStdf.Indexing;

#nullable enable

namespace LinqToStdf
{
    /// <summary>
    /// This class allows you to generate "compiled queries".  These are queries that have been 
    /// analyzed and optimized by skipping unused records, and even unused fields if possible.
    /// This can provide excellent performance, depending on the situation.  See the remarks
    /// for limitations.  The resulting compiled query takes a path to an STDF file and returns
    /// the output of the query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pre-compiled queries have the following limitations:
    /// <list type="bullet">
    /// <item>They must be expressible as an expression tree</item>
    /// <item>They should not "leak" records parsed from the file itself.
    /// (This is currently not enforced)
    /// Any data returned from a compiled query should be put into new object instances
    /// rather than passing records back directly.  Otherwise, the analyzer cannot detect which
    /// fields you are using from those records and you'll get back empty records.
    /// This does not apply to error records or other injected/synthesized records.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class CompiledQuery
    {

        /// <summary>
        /// Compiles an argument-less query
        /// </summary>
        /// <typeparam name="TResult">The type of the result (in most cases, this can be inferred)</typeparam>
        /// <param name="query">The expression tree representing the query.</param>
        /// <returns>The results of the query</returns>
        public static Func<string, TResult> Compile<TResult>(Expression<Func<StdfFile, TResult>> query)
        {
            return Compile(null, query);
        }

        /// <summary>
        /// Compiles an argument-less query, allowing you to do some initialization on the <see cref="StdfFile"/>
        /// object before the query runs.  This allows you to set options or add filters or seek algorithms.
        /// </summary>
        /// <typeparam name="TResult">The type of the result (in most cases, this can be inferred)</typeparam>
        /// <param name="stdfFileInit">An action that will initialize the <see cref="StdfFile"/>.</param>
        /// <param name="query">The expression tree representing the query.</param>
        /// <returns>The results of the query</returns>
        public static Func<string, TResult> Compile<TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, TResult>> query)
        {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path) =>
            {
                StdfFile stdf;
                if (factory == null)
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), false, rnf) { IndexingStrategy = new NonCachingStrategy() };
                    factory = stdf.ConverterFactory;
                }
                else
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), factory) { IndexingStrategy = new NonCachingStrategy() };
                }
                stdfFileInit?.Invoke(stdf);
                return compiled(stdf);
            };
        }

        public static Func<string, T1, TResult> Compile<T1, TResult>(Expression<Func<StdfFile, T1, TResult>> query)
        {
            return Compile(null, query);
        }

        public static Func<string, T1, TResult> Compile<T1, TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, T1, TResult>> query)
        {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path, t1) =>
            {
                StdfFile stdf;
                if (factory == null)
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), false, rnf) { IndexingStrategy = new NonCachingStrategy() };
                    factory = stdf.ConverterFactory;
                }
                else
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), factory) { IndexingStrategy = new NonCachingStrategy() };
                }
                stdfFileInit?.Invoke(stdf);
                return compiled(stdf, t1);
            };
        }

        public static Func<string, T1, T2, TResult> Compile<T1, T2, TResult>(Expression<Func<StdfFile, T1, T2, TResult>> query)
        {
            return Compile(null, query);
        }

        public static Func<string, T1, T2, TResult> Compile<T1, T2, TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, T1, T2, TResult>> query)
        {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path, t1, t2) =>
            {
                StdfFile stdf;
                if (factory == null)
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), false, rnf) { IndexingStrategy = new NonCachingStrategy() };
                    factory = stdf.ConverterFactory;
                }
                else
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), factory) { IndexingStrategy = new NonCachingStrategy() };
                }
                stdfFileInit?.Invoke(stdf);
                return compiled(stdf, t1, t2);
            };
        }

        public static Func<string, T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Expression<Func<StdfFile, T1, T2, T3, TResult>> query)
        {
            return Compile(null, query);
        }

        public static Func<string, T1, T2, T3, TResult> Compile<T1, T2, T3, TResult>(Action<StdfFile> stdfFileInit, Expression<Func<StdfFile, T1, T2, T3, TResult>> query)
        {
            var rnf = ExpressionInspector.Inspect(query);
            var compiled = query.Compile();
            RecordConverterFactory factory = null;
            return (path, t1, t2, t3) =>
            {
                StdfFile stdf;
                if (factory == null)
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), false, rnf) { IndexingStrategy = new NonCachingStrategy() };
                    factory = stdf.ConverterFactory;
                }
                else
                {
                    stdf = new StdfFile(new StdfFileStreamManager(path), factory) { IndexingStrategy = new NonCachingStrategy() };
                }
                stdfFileInit?.Invoke(stdf);
                return compiled(stdf, t1, t2, t3);
            };
        }
    }
}
