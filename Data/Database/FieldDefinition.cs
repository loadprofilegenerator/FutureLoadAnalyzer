
namespace Data.Database {
    public class FieldDefinition {
        public FieldDefinition([JetBrains.Annotations.NotNull] string name, [JetBrains.Annotations.NotNull] string type)
        {
            Name = name;
            Type = type;
        }

        public FieldDefinition([JetBrains.Annotations.NotNull] string name, SqliteDataType mytype)
        {
            Name = name;
            Type = mytype.ToString();
        }

        [JetBrains.Annotations.NotNull]
        public string Name { get; }

        [JetBrains.Annotations.NotNull]
        public string Type { get; }
    }
}