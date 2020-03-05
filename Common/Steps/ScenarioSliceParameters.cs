using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Steps {
    public class ScenarioSliceParameters : IEquatable<ScenarioSliceParameters> {
        public ScenarioSliceParameters([NotNull] Scenario dstScenario, int dstYear, [CanBeNull] ScenarioSliceParameters previousSlice)
        {
            DstScenario = dstScenario;
            DstYear = dstYear;
            PreviousSlice = previousSlice;
            if (DstScenario == PreviousSlice?.DstScenario && DstYear == previousSlice?.DstYear) {
                throw new FlaException("You can't run a slice processor with the same source and destination year and scenario");
            }
        }

        //mobility
        [ScenarioComment("Energiebedarf der neu installierten Klimaanlagen in kWh/m2", ScenarioCategory.Klimatisierung)]

        public double AirConditioningEnergyIntensityInKWhPerSquareMeter { get; set; }

        [ScenarioComment("Haus Energie Renovierungsfaktor (0.2 = von 100 MWh Verbrauch bleiben noch 20 MWh) [%]", ScenarioCategory.Wärmebedarf)]
        public double AverageHouseRenovationFactor { get; set; }

        [ScenarioComment("Prozent Autobesitzer (0.44 = 440/1000 Personen haben ein Auto) [%]", ScenarioCategory.Mobilität)]
        public double CarOwnershipPercentage { get; set; }

        [ScenarioComment("Wie viele Boiler auf Wärmepumpen umgestellt werden", ScenarioCategory.Wärmebedarf)]
        public double DHWSystemConversionNumber { get; set; }

        [ScenarioComment("Legt das aktuelle Szenario fest", ScenarioCategory.System)]
        [JsonConverter(typeof(StringEnumConverter))]
        [NotNull]
        public Scenario DstScenario { get; set; }

        [ScenarioComment("Legt das aktuelle Jahr fest", ScenarioCategory.System)]

        //PV
        public int DstYear { get; set; }

        //heating
        [ScenarioComment("Wie viele Heizungen von Gas auf Wärmepumpen umgestellt werden, in % des 2017 Energieverbrauchs",
            ScenarioCategory.Wärmebedarf)]
        public double Energy2017PercentageFromGasToHeatpump { get; set; }

        //heating
        [ScenarioComment("Wie viele Heizungen von Öl auf Wärmepumpen umgestellt werden, in % des 2017 Energieverbrauchs",
            ScenarioCategory.Wärmebedarf)]
        public double Energy2017PercentageFromOilToHeatpump { get; set; }

        [ScenarioComment("Wie viele Heizungen von Other auf Wärmepumpen umgestellt werden, in % des 2017 Energieverbrauchs",
            ScenarioCategory.Wärmebedarf)]
        public double Energy2017PercentageFromOtherToHeatpump { get; set; }

        [ScenarioComment("Reduktion Stromverbrauch Gebäudeinfrastruktur (Prozent, über 5 Jahre, 0-1, 0.99 = 1% Reduktion)",
            ScenarioCategory.Wärmebedarf)]
        public double EnergyReductionFactorBuildingInfrastructure { get; set; }

        [ScenarioComment("Reduktion Energieverbrauch Businesses (Prozent, über 5 Jahre, 0-1, 0.99 = 1% Reduktion)", ScenarioCategory.Energie)]
        public double EnergyReductionFactorBusiness { get; set; }

        [ScenarioComment("Reduktion Energieverbrauch Haushalte (Prozent, über 5 Jahre, 0-1, 0.99 = 1% Reduktion)", ScenarioCategory.Energie)]
        public double EnergyReductionFactorHouseholds { get; set; }

        [ScenarioComment("Anzahl der neu gebohrenen Kinder", ScenarioCategory.Bevölkerung)]
        public double NumberOfChildren { get; set; }

        [ScenarioComment("Anzahl der Tode (Wahrscheinlichkeit abhängig vom Alter)", ScenarioCategory.Bevölkerung)]
        public double NumberOfDeaths { get; set; }

        [ScenarioComment("Anteil an Flächen die Klimatisiert sind", ScenarioCategory.Klimatisierung)]
        public double PercentageOfAreaWithAirConditioning { get; set; }

        //system
        [CanBeNull]
        public ScenarioSliceParameters PreviousSlice { get; set; }

        //system
        [NotNull]
        public ScenarioSliceParameters PreviousSliceNotNull {
            get {
                if (PreviousSlice == null) {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                    throw new FlaException("PreviousScenario was not set on scenario " + DstScenario + " - " + DstYear);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
                }

                return PreviousSlice;
            }
        }

        [ScenarioComment("PV Abregelung auf x Prozent (0.7 = Abregelung auf 70% der maximalen Leistung)", ScenarioCategory.PV)]
        public double PVCurtailToXPercent { get; set; } = 1;

        [ScenarioComment("In diesem Zeitabschnitt zusätzlich zu installierende PV-Leistung in GWh", ScenarioCategory.PV)]
        public double PvPowerToInstallInGwh { get; set; }

        [ScenarioComment("Prozent der Gebäudesanierungen [%, 0-1]", ScenarioCategory.Wärmebedarf)]
        public double RenovationRatePercentage { get; set; }

        //[ScenarioComment("Smart", ScenarioCategory.Bevölkerung)]
        public bool SmartGridEnabled { get; set; } = false;

        [ScenarioComment("Ziel-Bevölkerung nach der Migration", ScenarioCategory.Bevölkerung)]
        public double TargetPopulationAfterMigration { get; set; }

        [ScenarioComment("Prozent der Elektroautos [%]", ScenarioCategory.Mobilität)]
        public double TotalPercentageOfElectricCars { get; set; }

        public bool Equals([CanBeNull] ScenarioSliceParameters other) =>
            DstScenario == other?.DstScenario && DstYear == other.DstYear;

        [NotNull]
        public ScenarioSliceParameters CopyThisSlice()
        {
            var sourceProps = typeof(ScenarioSliceParameters).GetProperties().Where(x => x.CanRead).ToList();
            ScenarioSliceParameters newSlice = new ScenarioSliceParameters(DstScenario, DstYear, PreviousSlice);

            foreach (var property in sourceProps) {
                if (property.CanWrite) {
                    // check if the property can be set or no.
                    property.SetValue(newSlice, property.GetValue(this, null), null);
                }
            }

            return newSlice;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != GetType()) {
                return false;
            }

            return Equals((ScenarioSliceParameters)obj);
        }

        [NotNull]
        public string GetFileName() => DstScenario + "." + DstYear + ".";

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked {
                return (DstScenario.GetHashCode() * 397) ^ DstYear;
            }
        }

        [NotNull]
        public override string ToString() => DstScenario + " - " + DstYear;
    }
}