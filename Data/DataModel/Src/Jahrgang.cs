using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(Jahrgang))]
    [Table(nameof(Jahrgang))]
    [NPoco.PrimaryKey(nameof(ID))]
    public class Jahrgang {
        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        public int Jahr { get; set; }
        public int Count { get; set; }
    }
}