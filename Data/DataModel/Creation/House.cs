using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Common;
using Common.Database;
using Common.Logging;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Creation {
#pragma warning disable CA1040 // Avoid empty interfaces

    [TableName(nameof(House))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(House))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class House : IGuidProvider {
        public enum CoordsToUse {
            GWR,
            Localnet
        }

        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public House()
        {
        }

        public House([JetBrains.Annotations.NotNull] string complexName,
                     [JetBrains.Annotations.NotNull] string complexGuid,
                     [JetBrains.Annotations.NotNull] string guid)
        {
            ComplexName = complexName;
            ComplexGuid = complexGuid;
            Guid = guid;
        }

        [CanBeNull]
        public string Adress { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<AppartmentEntry> Appartments { get; set; } = new List<AppartmentEntry>();

        [CanBeNull]
        public string AppartmentsAsJson {
            get => JsonConvert.SerializeObject(Appartments, Formatting.Indented);
            set => Appartments = JsonConvert.DeserializeObject<List<AppartmentEntry>>(value);
        }

        public double Area { get; set; }

        public double AverageBuildingAge { get; set; }

        [CanBeNull]
        public string Comments { get; set; }

        [JetBrains.Annotations.NotNull]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public string ComplexGuid { get; set; }

        public int ComplexID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ComplexName { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> EGIDs { get; set; } = new List<int>();

        [CanBeNull]
        public string EGIDsAsJson {
            get => JsonConvert.SerializeObject(EGIDs, Formatting.Indented);
            set => EGIDs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public double EnergieBezugsFläche {
            get { return Appartments.Sum(x => x.EnergieBezugsFläche); }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> ErzeugerIDs { get; set; } = new List<string>();

        [CanBeNull]
        public string ErzeugerIDsAsJson {
            get => JsonConvert.SerializeObject(ErzeugerIDs, Formatting.Indented);
            set => ErzeugerIDs = JsonConvert.DeserializeObject<List<string>>(value);
        }

        [CanBeNull]
        public string GebäudeIDsAsJson {
            get => JsonConvert.SerializeObject(GebäudeObjectIDs, Formatting.Indented);
            set => GebäudeObjectIDs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        /// <summary>
        ///     Localnet gebäude ids
        /// </summary>
        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> GebäudeObjectIDs { get; set; } = new List<int>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<Hausanschluss> Hausanschluss { get; set; } = new List<Hausanschluss>();

        [CanBeNull]
        public string HausanschlussAsJson {
            get => JsonConvert.SerializeObject(Hausanschluss, Formatting.Indented);
            set => Hausanschluss = JsonConvert.DeserializeObject<List<Hausanschluss>>(value);
        }

        [JetBrains.Annotations.NotNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        public string HouseGuid => Guid;

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<WgsPoint> LocalWgsPoints { get; set; } = new List<WgsPoint>();

        [CanBeNull]
        public string LocalWgsPointsAsJson {
            get => JsonConvert.SerializeObject(LocalWgsPoints, Formatting.Indented);
            set => LocalWgsPoints = JsonConvert.DeserializeObject<List<WgsPoint>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> MonthlyEnergyUseIDs { get; set; } = new List<int>();

        [CanBeNull]
        public string MonthlyEnergyUseIDsAsJson {
            get => JsonConvert.SerializeObject(MonthlyEnergyUseIDs, Formatting.Indented);
            set => MonthlyEnergyUseIDs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        public int OfficialNumberOfHouseholds {
            get { return Appartments.Count(x => x.IsApartment); }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> Standortes { get; set; } = new List<string>();

        [CanBeNull]
        public string StandortIDsAsJson {
            get => JsonConvert.SerializeObject(Standortes, Formatting.Indented);
            set => Standortes = JsonConvert.DeserializeObject<List<string>>(value);
        }

        /// <summary>
        ///     just for ease of use show the very first trafokreis as needed
        /// </summary>
        [CanBeNull]
        [NPoco.Ignore]
        [SQLite.Ignore]
        public string TrafoKreis {
            get { return Hausanschluss.Select(x => x.Trafokreis).FirstOrDefault(); }
        }

        /// <summary>
        ///     GWR WGS coords
        /// </summary>
        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<WgsPoint> WgsGwrCoords { get; set; } = new List<WgsPoint>();

        [CanBeNull]
        public string WgsGwrCoordsAsJson {
            get => JsonConvert.SerializeObject(WgsGwrCoords, Formatting.Indented);
            set => WgsGwrCoords = JsonConvert.DeserializeObject<List<WgsPoint>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<IHouseComponent> CollectHouseComponents([JetBrains.Annotations.NotNull] HouseComponentRepository hcr)
        {
            List<IHouseComponent> components = hcr.HouseComponentsByHouseGuid[Guid];
            return components;
        }

        [JetBrains.Annotations.NotNull]
        public HouseSummedLocalnetEnergyUse CollectTotalEnergyConsumptionFromLocalnet([JetBrains.Annotations.NotNull] [ItemNotNull]
                                                                                      List<HouseSummedLocalnetEnergyUse> energySum)
        {
            return energySum.Single(x => x.HouseGuid == Guid);
        }

        [CanBeNull]
        public Hausanschluss GetHausanschlussByIsn([JetBrains.Annotations.NotNull] List<int> isnIDs,
                                                   [CanBeNull] string standort,
                                                   [JetBrains.Annotations.NotNull] [ItemNotNull]
                                                   List<Hausanschluss> allHausanschlusses,
                                                   [JetBrains.Annotations.NotNull] ILogger logger,
                                                   bool kleinanschlussOk = true)
        {
            if (Hausanschluss[0].ObjectID.Contains("LEUCHTE")) {
                throw new FlaException("Trying to assign a house to a leuchte");
            }

            if (standort == "Gifasverteiler, Kirchbergstrasse 23A, Erdgeschoss, 3400 Burgdorf") {
                Console.WriteLine("hi");
            }

            if (isnIDs.Count == 0) {
                Hausanschluss anschluss = null;
                if (!kleinanschlussOk) {
                    anschluss = Hausanschluss.FirstOrDefault(x => !x.ObjectID.ToLower(CultureInfo.InvariantCulture).Contains("kleinanschluss"));
                }

                if (anschluss == null) {
                    anschluss = Hausanschluss[0];
                }

                return anschluss;
            }

            if (!Standortes.Contains(standort) && Standortes.Count > 0) {
                throw new FlaException("Trying to match standort that is not part of the house: " + standort + "\nStandorts are: " +
                                       JsonConvert.SerializeObject(Standortes, Formatting.Indented));
            }

            if (standort?.Contains("Oberburgstrasse 54D") == true) {
                Console.WriteLine("hi");
            }

            var has = new List<Hausanschluss>();
            foreach (var isnID in isnIDs) {
                var ha = Hausanschluss.Where(x => x.Isn == isnID).ToList();
                has.AddRange(ha);
            }

            if (Hausanschluss.Count == 0) {
                throw new FlaException("The house " + ComplexName + " had no hausanschluss ");
            }

            if (has.Count == 0 && Hausanschluss.All(x => x.MatchingType != HouseMatchingType.Proximity)) {
                List<Hausanschluss> correctHausanschluss = new List<Hausanschluss>();
                foreach (var isnID in isnIDs) {
                    correctHausanschluss.AddRange(allHausanschlusses.Where(x => x.Isn == isnID));
                }

                if (correctHausanschluss.Count > 0) {
                    logger.ErrorM("Not a single hausanschluss for the isn found in the house " + ComplexName + " and the isns " +
                                  JsonConvert.SerializeObject(isnIDs) + " on a directly matched house. Correct HAs would be " +
                                  JsonConvert.SerializeObject(correctHausanschluss),
                        Stage.Houses,
                        "GetHausanschluss");
                }
            }

            if (has.Count > 0) {
                if (has.Count == 1) {
                    return has[0];
                }

                var correctstandort = has.FirstOrDefault(x => x.Standort == standort);
                if (correctstandort != null) {
                    return correctstandort;
                }

                var hawithoutstandort = has.Where(x => string.IsNullOrWhiteSpace(x.Standort)).ToList();
                if (hawithoutstandort.Count > 0) {
                    return hawithoutstandort[0];
                }

                return has[0];
            }

            var hasWithCorrectStandort = Hausanschluss.Where(x => x.Standort == standort).ToList();
            if (hasWithCorrectStandort.Count > 2) {
                throw new FlaException("more than one ha with correct standort");
            }

            if (hasWithCorrectStandort.Count == 1) {
                return hasWithCorrectStandort[0];
            }

            return Hausanschluss[0];
        }

        [CanBeNull]
        public MapColorEntryWithHouseGuid GetMapColorForHouse([JetBrains.Annotations.NotNull] Func<House, RGB> func)
        {
            if (func == null) {
                throw new Exception("Func was null");
            }

            var rgb = func(this);
            var mc = new MapColorEntryWithHouseGuid(Guid, rgb);
            return mc;
        }

        [CanBeNull]
        public MapColorEntryWithHouseGuid GetMapColorForHouse([JetBrains.Annotations.NotNull] Func<House, RGBWithLabel> func)
        {
            if (func == null) {
                throw new Exception("Func was null");
            }

            var rgb = func(this);
            var mc = new MapColorEntryWithHouseGuid(Guid, rgb.GetRGB(), rgb.Label);
            return mc;
        }

        [CanBeNull]
        public MapColorEntryWithHouseGuid GetMapColorForHouseWithName([JetBrains.Annotations.NotNull] Func<House, RGB> func)
        {
            if (func == null) {
                throw new Exception("Func was null");
            }

            var rgb = func(this);
            var mc = new MapColorEntryWithHouseGuid(Guid, rgb, ComplexName);
            return mc;
        }

        [CanBeNull]
        public MapPoint GetMapPoint([JetBrains.Annotations.NotNull] Func<House, RGB> func)
        {
            if (WgsGwrCoords.Count == 0) {
                return null;
            }

            if (func == null) {
                throw new Exception("Func was null");
            }

            var rgb = func(this);
            var g = WgsGwrCoords[0];
            var mp = new MapPoint(g.Lon, g.Lat, 10, rgb.R, rgb.G, rgb.B);
            return mp;
        }

        [CanBeNull]
        public MapPoint GetMapPointWithSize([JetBrains.Annotations.NotNull] Func<House, RGBWithSize> func, CoordsToUse coords = CoordsToUse.GWR)
        {
            if (func == null) {
                throw new Exception("Func was null");
            }

            var rgb = func(this);
            WgsPoint g;
            switch (coords) {
                case CoordsToUse.GWR:
                    if (WgsGwrCoords.Count == 0) {
                        return null;
                    }

                    g = WgsGwrCoords[0];
                    break;
                case CoordsToUse.Localnet:
                    if (LocalWgsPoints.Count == 0) {
                        return null;
                    }

                    g = LocalWgsPoints[0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(coords), coords, null);
            }

            var mp = new MapPoint(g.Lat, g.Lon, rgb.Size, rgb.R, rgb.G, rgb.B);
            return mp;
        }

        [JetBrains.Annotations.NotNull]
        public List<int> GetTrafoKreise()
        {
            return Hausanschluss.Select(x => x.Isn).ToList();
        }
    }
}