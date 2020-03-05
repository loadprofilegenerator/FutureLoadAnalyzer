using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.Database {
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class TableClass {
        [NotNull] private List<KeyValuePair<string, Type>> _fieldInfo = new List<KeyValuePair<string, Type>>();
        [NotNull] private string _className;

        [NotNull]
        private static Dictionary<Type, string> DataMapper {
            get {
                // Add the rest of your CLR Types to SQL Types mapping here
                var dataMapper = new Dictionary<Type, string> {
                    {
                        typeof(int), "INTEGER"
                    }, {
                        typeof(string), "TEXXT"
                    }, {
                        typeof(bool), "BIT"
                    }, {
                        typeof(DateTime), "DATETIME"
                    }, {
                        typeof(float), "DOUBLE"
                    }, {
                        typeof(decimal), "DECIMAL(18,0)"
                    }, {
                        typeof(Guid), "GUID"
                    }
                };

                return dataMapper;
            }
        }

        [NotNull]
        public List<KeyValuePair<string, Type>> Fields {
            get => _fieldInfo;
            set => _fieldInfo = value;
        }

        [NotNull]
        public string ClassName {
            get => _className;
            set => _className = value;
        }

        public TableClass([NotNull] Type t)
        {
            _className = t.Name;

            foreach (var p in t.GetProperties()) {
                var field = new KeyValuePair<string, Type>(p.Name, p.PropertyType);

                Fields.Add(field);
            }
        }

        [NotNull]
        public string CreateTableScript()
        {
            var script = new StringBuilder();

            script.AppendLine("CREATE TABLE " + ClassName);
            script.AppendLine("(");
            script.AppendLine("\t ID BIGINT,");
            for (var i = 0; i < Fields.Count; i++) {
                var field = Fields[i];

                if (DataMapper.ContainsKey(field.Value)) {
                    script.Append("\t " + field.Key + " " + DataMapper[field.Value]);
                }
                else {
                    // Complex Type?
                    script.Append("\t " + field.Key + " BIGINT");
                }

                if (i != Fields.Count - 1) {
                    script.Append(",");
                }

                script.Append(Environment.NewLine);
            }

            script.AppendLine(")");

            return script.ToString();
        }
    }
}