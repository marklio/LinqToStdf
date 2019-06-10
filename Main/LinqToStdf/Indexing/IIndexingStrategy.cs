// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading.Tasks;

#nullable enable

namespace LinqToStdf.Indexing
{
    public interface IIndexingStrategy
    {
        IAsyncEnumerable<StdfRecord> CacheRecords(IAsyncEnumerable<StdfRecord> records);
        Expression TransformQuery(Expression query);
    }

    public class NonCachingStrategy : IIndexingStrategy
    {
        #region IIndexingStrategy Members

        public IAsyncEnumerable<StdfRecord> CacheRecords(IAsyncEnumerable<StdfRecord> records)
        {
            return records;
        }

        public Expression TransformQuery(Expression query)
        {
            return query;
        }

        #endregion
    }

    public abstract class CachingIndexingStrategy : IIndexingStrategy
    {

        bool _Caching = false;
        bool _Cached = false;

        public abstract ValueTask IndexRecords(IAsyncEnumerable<StdfRecord> records);
        public abstract IAsyncEnumerable<StdfRecord> EnumerateIndexedRecords();
        public abstract Expression TransformQuery(Expression query);

        async IAsyncEnumerable<StdfRecord> IIndexingStrategy.CacheRecords(IAsyncEnumerable<StdfRecord> records)
        {
            if (_Caching)
            {
                throw new InvalidOperationException(Resources.CachingReEntrancy);
            }
            //cache the records
            if (!_Cached)
            {
                _Caching = true;
                await IndexRecords(records);
                _Caching = false;
                _Cached = true;
            }
            //provide the cached records
            //TODO: since these are indexed, this could be synchronous?
            await foreach (var r in EnumerateIndexedRecords())
            {
                yield return r;
            }
        }
    }

    public class SimpleIndexingStrategy : CachingIndexingStrategy
    {
        List<StdfRecord> _Records;

        public override Expression TransformQuery(Expression query)
        {
            return query;
        }

        public async override ValueTask IndexRecords(IAsyncEnumerable<StdfRecord> records)
        {
            _Records = new List<StdfRecord>();
            await foreach (var r in records)
            {
                _Records.Add(r);
            }
        }

        public async override IAsyncEnumerable<StdfRecord> EnumerateIndexedRecords()
        {
            foreach (var r in _Records)
            {
                yield return r;
            }
        }
    }
}
