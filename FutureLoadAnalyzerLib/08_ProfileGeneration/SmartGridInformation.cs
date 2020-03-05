using SQLite;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    public class SmartGridInformation {
        [AutoIncrement]
        [PrimaryKey]
        public int ID { get; set; }

        public int NumberOfProsumers { get; set; }
        public int NumberOfReductionFactors { get; set; }
        public double SummedReductionFactor { get; set; }
        public double TotalStorageSize { get; set; }
    }
}