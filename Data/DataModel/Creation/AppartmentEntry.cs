namespace Data.DataModel.Creation {
    public class AppartmentEntry {
        public AppartmentEntry([JetBrains.Annotations.NotNull] string guid, double energieBezugsFläche, bool isApartment, int year)
        {
            Guid = guid;
            EnergieBezugsFläche = energieBezugsFläche;
            IsApartment = isApartment;
            Year = year;
        }

        public double EnergieBezugsFläche { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        public bool IsApartment { get; set; }
        public int Year { get; set; }
    }
}