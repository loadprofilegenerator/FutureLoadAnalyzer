using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;
using JetBrains.Annotations;

namespace Data.Database {
    public class RowBuilder {
        [NotNull]
        [ItemNotNull]
        public List<RowValue> RowValues { get; } = new List<RowValue>();

        [NotNull]
        public RowBuilder Add([NotNull] string name, [CanBeNull] object content)
        {
            if (RowValues.Any(x => x.Name == name)) {
                throw new FlaException("Key was already added for this row: " + name);
            }

            RowValues.Add(new RowValue(name, content));
            return this;
        }

        [NotNull]
        public RowBuilder AddToPossiblyExisting([NotNull] string name, double content)
        {
            var other = RowValues.FirstOrDefault(x => x.Name == name);
            if (other != null) {
                if(other.Value == null) {
                    throw new FlaException("was null");
                }
                other.Value =(double) other.Value + content;
            }
            else {
                RowValues.Add(new RowValue(name, content));
            }

            return this;
        }

        [NotNull]
        public static RowBuilder GetAllProperties([NotNull] object o)
        {
            var rb = new RowBuilder();
            var properties = o.GetType().GetProperties();
            foreach (PropertyInfo property in properties) {
                var shouldIgnoreAttribute = Attribute.IsDefined(property, typeof(RowBuilderIgnoreAttribute));
                if (shouldIgnoreAttribute) {
                    continue;
                }

                object val = property.GetValue(o);
                if (o is List<string> mylist) {
                    val = string.Join(",", mylist);
                }

                rb.Add(property.Name, val);
            }

            return rb;
        }

        [NotNull]
        public Row GetRow() => new Row(RowValues);

        public void Merge([NotNull] RowBuilder toRowBuilder)
        {
            foreach (var line in toRowBuilder.RowValues) {
                RowValues.Add(new RowValue(line.Name, line.Value));
            }
        }

        [NotNull]
        public static RowBuilder Start([NotNull] string name, [CanBeNull] object content)
        {
            var rb = new RowBuilder();
            return rb.Add(name, content);
        }
    }
}