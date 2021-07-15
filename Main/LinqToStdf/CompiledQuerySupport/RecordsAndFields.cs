// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.CompiledQuerySupport {

    /// <summary>
    /// This class holds a list of records and fields to indicate which
    /// ones should be parsed for a compiled query.
    /// </summary>
    class RecordsAndFields {
        /// <summary>
        /// The internal datastructure Type is a record type, and the hashset is a set of property names
        /// </summary>
        readonly Dictionary<Type, HashSet<string>> _Fields = new Dictionary<Type, HashSet<string>>();

        /// <summary>
        /// adds a field for a type
        /// </summary>
        public void AddField(Type type, string field) {
            GetFieldsForAdding(type).Add(field);
        }

        /// <summary>
        /// Adds a set of fields for a type
        /// </summary>
        public void AddFields(Type type, IEnumerable<string> fields) {
            GetFieldsForAdding(type).UnionWith(fields);
        }

        public bool TypeHasFields(Type type)
        {
            return _Fields.ContainsKey(type);
        }

        public HashSet<string>? GetFieldsForType(Type type)
        {
            if (_Fields.TryGetValue(type, out var fields))
            {
                return new HashSet<string>(fields);
            }
            return null;
        }

        /// <summary>
        /// Gets the fields for a type
        /// </summary>
        HashSet<string> GetFieldsForAdding(Type type) {
            if (!_Fields.TryGetValue(type, out var fields)) {
                fields = new HashSet<string>();
                _Fields[type] = fields;
            }
            return fields;
        }

        /// <summary>
        /// Gets all the types
        /// </summary>
        public IEnumerable<Type> Types {
            get { return _Fields.Keys; }
        }
    }
}
