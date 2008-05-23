// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;

//we only need this for Silverlight
#if SILVERLIGHT
namespace System.Collections.Generic {
    public class HashSet<T> : IEnumerable<T> {

        static readonly object _Dummy = new object();
        Dictionary<T, object> _Set = new Dictionary<T, object>();

        public HashSet() { }

        public HashSet(IEnumerable<T> items) {
            foreach (var item in items) {
                _Set.Add(item, _Dummy);
            }
        }

        public void UnionWith(IEnumerable<T> fields) {
            throw new NotImplementedException();
        }

        public bool Add(T item) {
            if (_Set.ContainsKey(item)) return false;
            _Set.Add(item, _Dummy);
            return true;
        }

        public bool Contains(T item) {
            return _Set.ContainsKey(item);
        }

        public int Count {
            get { return _Set.Count; }
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator() {
            return _Set.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

    }
}
#endif