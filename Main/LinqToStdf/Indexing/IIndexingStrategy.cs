using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace LinqToStdf.Indexing {
    public interface IIndexingStrategy {
        IEnumerable<StdfRecord> CacheRecords(IEnumerable<StdfRecord> records);
        Expression TransformQuery(Expression query);
    }

    public class NonCachingStrategy : IIndexingStrategy {
        #region IIndexingStrategy Members

        public IEnumerable<StdfRecord> CacheRecords(IEnumerable<StdfRecord> records) {
            return records;
        }

        public Expression TransformQuery(Expression query) {
            return query;
        }

        #endregion
    }

    public abstract class CachingIndexingStrategy : IIndexingStrategy {

        bool _Caching = false;
        bool _Cached = false;

        public abstract void IndexRecords(IEnumerable<StdfRecord> records);
        public abstract IEnumerable<StdfRecord> EnumerateIndexedRecords();
        public abstract Expression TransformQuery(Expression query);

        IEnumerable<StdfRecord> IIndexingStrategy.CacheRecords(IEnumerable<StdfRecord> records) {
            if (_Caching) {
                throw new InvalidOperationException(Resources.CachingReEntrancy);
            }
            //cache the records
            if (!_Cached) {
                _Caching = true;
                IndexRecords(records);
                _Caching = false;
                _Cached = true;
            }
            //provide the cached records
            return EnumerateIndexedRecords();
        }
    }

    public class SimpleIndexingStrategy : CachingIndexingStrategy {
        List<StdfRecord> _Records;

        public override Expression TransformQuery(Expression query) {
            return query;
        }

        public override void IndexRecords(IEnumerable<StdfRecord> records) {
            _Records = new List<StdfRecord>();
            foreach (var r in records) {
                _Records.Add(r);
            }
        }

        public override IEnumerable<StdfRecord> EnumerateIndexedRecords() {
            foreach (var r in _Records) {
                yield return r;
            }
        }
    }
}
