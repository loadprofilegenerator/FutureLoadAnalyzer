namespace Data.DataModel.Creation {
    public class AppartmentHeatingDemand {
        public AppartmentHeatingDemand([JetBrains.Annotations.NotNull] string houseExpansionGuid,
                                       double energiebezugsfläche,
                                       double heatDemand,
                                       int year)
        {
            HouseExpansionGuid = houseExpansionGuid;
            Energiebezugsfläche = energiebezugsfläche;
            HeatDemand = heatDemand;
            Year = year;
        }

        public double Energiebezugsfläche { get; set; }
        public double HeatDemand { get; set; }

        [JetBrains.Annotations.NotNull]
        public string HouseExpansionGuid { get; set; }

        public int Year { get; }
    }
}