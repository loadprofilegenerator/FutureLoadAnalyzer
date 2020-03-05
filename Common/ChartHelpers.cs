using JetBrains.Annotations;

namespace Common {
    public static class ChartHelpers {
        [NotNull]
        public static string GetFriendlyEnergTypeName([NotNull] string profileName)
        {
            switch (profileName) {
                case "Electricity": return "Strom";
                case "Other": return "Sonstiges / Fernwärme";
                case "Oil": return "Öl";
                case "Gas": return "Gas";
                default: throw new FlaException("Unknown name: \"" + profileName + "\"");
            }
        }

        [NotNull]
        public static string GetFriendlyScenarioName([NotNull] string profileName)
        {
            switch (profileName) {
                case "Pom":
                    return "POM";
                case "Nep": return "NEP";
                case "Utopia": return "Utopia";
                case "Dystopia": return "Dystopia";
                case "UtopiaCurtailTo40":return "Utopia (PV abgeregelt auf 40%)";
                case "UtopiaCurtailTo50": return "Utopia (PV abgeregelt auf 50%)";
                case "UtopiaCurtailTo70":  return "Utopia (PV abgeregelt auf 70%)";
                case "UtopiaNoEfficiencyLowRenovation": return "Utopia (Keine Effizienz, wenig Renovierungen)";
                case "Present": return "Gegenwart";
                default: throw new FlaException("Unknown name: \"" + profileName + "\"");
            }
        }

        [NotNull]
        public static string GetFriendlyProviderName([NotNull] string profileName)
        {
            switch (profileName) {
                case "CachingLPGProfileLoader LPG Electricity (cached) Load": return "Haushalte";
                case "CachingLPGProfileLoader LPG Car Charging Electricity (cached) Load": return "Elektromobilität";
                case "HeatingProvider Load": return "Wärmepumpen";
                case "DhwProvider Load": return "Warmwasser";
                case "HouseInfrastructureProvider Load": return "Gebäudeinfrastruktur";
                case "HouseholdLoadProfileProvider - SLP H0 Substitute Profile Load": return "Haushalte";
                case "BusinessProfileProvider Load": return "Gewerbe";
                case "CoolingProvider Load": return "Klimatisierung";
                case "LastgangMessungProvider Load": return "Lastgangmessungen";
                case "ElectricCarProvider - Flat Substitute Profile Load": return "Elektromobilität";
                case "WasserkraftProvider Load": return "Wasserkraft";
                case "WasserkraftProvider Generation": return "Wasserkraft";
                case "PVProfileProvider Generation": return "Photovoltaik";
                case "CachingLPGProfileLoader LPG Load": return "Haushalte";
                default: throw new FlaException("Unknown name: \"" + profileName + "\"");
            }
        }
    }
}