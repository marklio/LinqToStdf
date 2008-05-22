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
        Dictionary<Type, HashSet<string>> _Fields = new Dictionary<Type, HashSet<string>>();

        public void AddType(Type type) {
            GetFieldsForType(type);
        }

        public void AddField(Type type, string fields) {
            GetFieldsForType(type).Add(fields);
        }

        public void AddFields(Type type, IEnumerable<string> fields) {
            GetFieldsForType(type).UnionWith(fields);
        }

        public HashSet<string> GetFieldsForType(Type type) {
            HashSet<string> fields;
            if (!_Fields.TryGetValue(type, out fields)) {
                fields = new HashSet<string>();
                _Fields[type] = fields;
            }
            return fields;
        }


        public IEnumerable<Type> Types {
            get { return _Fields.Keys; }
        }

        public static RecordsAndFields operator +(RecordsAndFields first, RecordsAndFields second) {
            var allTypes = new HashSet<Type>(first.Types);
            allTypes.UnionWith(second.Types);
            var rnf = new RecordsAndFields();
            foreach (var type in allTypes) {
                rnf.AddType(type);
                rnf.AddFields(type, first.GetFieldsForType(type));
                rnf.AddFields(type, second.GetFieldsForType(type));
            }
            return rnf;
        }
    }
}
