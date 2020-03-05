using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using JetBrains.Annotations;
using MathNet.Numerics.Statistics;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class J1_AssignOutgoingCommuterData : RunableWithBenchmark {
        public J1_AssignOutgoingCommuterData([NotNull] ServiceRepository services)
            : base(nameof(J1_AssignOutgoingCommuterData), Stage.Houses, 1000, services, false)
        {
            DevelopmentStatus.Add("//TODO: make sure to only assign commutingMethod Car to people with cars!!!)");
            DevelopmentStatus.Add("//https://www.bfs.admin.ch/bfs/de/home/statistiken/mobilitaet-verkehr/personenverkehr/pendlermobilitaet.html");
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            var occupants = dbHouse.Fetch<Occupant>();
            var outgoingCommuters = dbHouse.Fetch<OutgoingCommuterEntry>();
            var carDistances = dbHouse.Fetch<CarDistanceEntry>();
            MakeCarDistanceHistogram();
            MakeCommuterSankey();
            MakeCommuterMap();
            MakeCommuterDistanceHistogram();
            MakeCommuterDistanceHistogramNonBurgdorf();

            void MakeCommuterSankey()
            {
                var ssa = new SingleSankeyArrow("OutgoingCommuters", 1500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Einwohner", occupants.Count, 5000, Orientation.Straight));
                var workersOutside = outgoingCommuters.Count(x => x.DistanceInKm > 0);
                var workersBurgdorf = outgoingCommuters.Count(x => Math.Abs(x.DistanceInKm) < 0.000001);
                var unemployed = occupants.Count - workersOutside - workersBurgdorf;
                ssa.AddEntry(new SankeyEntry("Ohne Anstellung", unemployed * -1, 2000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Pendler aus Burgdorf", workersOutside * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Arbeiter in Burgdorf", workersBurgdorf * -1, 2000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeCarDistanceHistogram()
            {
                var commuterDistances = carDistances.Select(x => x.TotalDistance).ToList();
                var hg = new Histogram(commuterDistances, 15);
                var bse = new List<BarSeriesEntry>();
                var barSeries = BarSeriesEntry.MakeBarSeriesEntry(hg, out var colNames, "F1");
                bse.Add(barSeries);
                var filename = MakeAndRegisterFullFilename("CarDistanceHistogram.png", Name, "", slice);
                Services.PlotMaker.MakeBarChart(filename, "Anzahl Autos mit dieser Entfernung", bse, colNames);
            }



            void MakeCommuterDistanceHistogram()
            {
                var commuterDistances = outgoingCommuters.Select(x => x.DistanceInKm).ToList();
                var hg = new Histogram(commuterDistances, 15);
                var bse = new List<BarSeriesEntry>();
                var barSeries = BarSeriesEntry.MakeBarSeriesEntry(hg, out var colNames, "F1");
                bse.Add(barSeries);
                var filename = MakeAndRegisterFullFilename("OutgoingCommutersDistanceHistogram.png", Name, "", slice);
                Services.PlotMaker.MakeBarChart(filename, "Anzahl Pendler mit dieser Entfernung", bse, colNames);
            }


            void MakeCommuterDistanceHistogramNonBurgdorf()
            {
                var commuterDistances = outgoingCommuters.Where(x => x.DistanceInKm > 0).Select(x => x.DistanceInKm).ToList();
                var hg = new Histogram(commuterDistances, 15);
                var bse = new List<BarSeriesEntry>();
                var barSeries = BarSeriesEntry.MakeBarSeriesEntry(hg, out var colNames, "F1");
                bse.Add(barSeries);
                var filename = MakeAndRegisterFullFilename("OutgoingCommutersDistanceHistogramNoBurgdorf.png", Name, "", slice);
                Services.PlotMaker.MakeBarChart(filename, "Anzahl Pendler mit dieser Entfernung", bse, colNames);
            }

            void MakeCommuterMap()
            {
                RGBWithSize GetColor(House h)
                {
                    var outgoing = outgoingCommuters.Count(x => x.HouseGuid == h.HouseGuid);
                    if (outgoing > 0) {
                        return new RGBWithSize(Constants.Red, outgoing + 10);
                    }

                    return new RGBWithSize(Constants.Black, 10);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("OutgoingCommutersMap.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Arbeiter", Constants.Red),
                    new MapLegendEntry("Keine Pendler", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<OutgoingCommuterEntry>(Stage.Houses, Constants.PresentSlice);
            SqlConnection.RecreateTable<CarDistanceEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var occupants = dbHouses.Fetch<Occupant>();
            var outGoings = dbRaw.Fetch<OutgoingCommuter>();
            if (occupants.Count == 0) {
                throw new Exception("No occupants found");
            }

            var entries = new List<OutgoingCommuterEntry>();

            foreach (var commuter in outGoings) {
                for (var i = 0; i < commuter.Erwerbstätige; i++) {
                    var oce = new OutgoingCommuterEntry {
                        CommuterGuid = Guid.NewGuid().ToString()
                    };
                    var method = Services.Rnd.NextDouble();

                    if (method < 0.52) {
                        oce.CommuntingMethod = CommuntingMethod.Car;
                    }
                    else {
                        oce.CommuntingMethod = CommuntingMethod.PublicTransport;
                    }

                    oce.DistanceInKm = commuter.Entfernung;
                    oce.WorkCity = commuter.Arbeitsgemeinde;
                    oce.WorkKanton = commuter.Arbeitskanton;
                    entries.Add(oce);
                }
            }

            dbHouses.BeginTransaction();
            //HashSet<string> usedHouseholdIDs = new HashSet<string>();
            //oce.CommuniterDistanceInKm = commuter.Entfernung + 27.39; //freizeit km nach mikromobilitätszensus, 12.05 auto-km/tag/person, 0.44 autos pro person = 27.39 km/tag
            var potentialOccupants = occupants.Where(x => x.Age > 18 && x.Age < 65).ToList();
            var cars = dbHouses.Fetch<Car>();
            var householdGuidsWithCar = cars.Select(x => x.HouseholdGuid).ToList();
            var occupantsWithCar = potentialOccupants.Where(x => householdGuidsWithCar.Contains(x.HouseholdGuid)).ToList();
            var occupantsWithoutCar = potentialOccupants.Where(x => !occupantsWithCar.Contains(x)).ToList();
            //List<CarDistanceEntry> cdes = new List<CarDistanceEntry>();
            List<Car> availableCars = cars.ToList();
            while (entries.Count > 0) {
                OutgoingCommuterEntry ogce = entries[0];
                entries.RemoveAt(0);
                Occupant oc = null;

                if (ogce.CommuntingMethod == CommuntingMethod.Car) {
                    Car myCar = null;
                    while (myCar == null) {
                        oc = occupantsWithCar[Services.Rnd.Next(occupantsWithCar.Count)];
                        occupantsWithCar.Remove(oc);
                        myCar = availableCars.FirstOrDefault(x => x.HouseholdGuid == oc.HouseholdGuid);
                    }
                    availableCars.Remove(myCar);
                    CarDistanceEntry cd = new CarDistanceEntry(oc.HouseGuid, oc.HouseholdGuid, myCar.CarGuid, ogce.DistanceInKm, 27.4);
                    //cdes.Add(cd);
                    dbHouses.Save(cd);
                }
                else {
                    if (occupantsWithoutCar.Count > 0) {
                        oc = occupantsWithoutCar[Services.Rnd.Next(occupantsWithoutCar.Count)];
                        occupantsWithoutCar.Remove(oc);
                    }
                    else
                    {
                        oc = occupantsWithCar[Services.Rnd.Next(occupantsWithCar.Count)];
                        occupantsWithCar.Remove(oc);
                    }
                }
                ogce.HouseholdGuid = oc.HouseholdGuid;
                ogce.HouseGuid = oc.HouseGuid;
                ogce.CommuterGuid = Guid.NewGuid().ToString();
                dbHouses.Save(ogce);
            }

            foreach (Car car in availableCars) {
                CarDistanceEntry cd = new CarDistanceEntry(car.HouseGuid, car.HouseholdGuid, car.CarGuid, 0, 27.4);
                dbHouses.Save(cd);
            }
            dbHouses.CompleteTransaction();
        }
    }

    public class CarDistanceEntry {
        public CarDistanceEntry([NotNull] string houseGuid, [NotNull] string householdGuid, [NotNull] string carGuid, double commutingDistance, double freizeitDistance)
        {
            HouseGuid = houseGuid;
            HouseholdGuid = householdGuid;
            CarGuid = carGuid;
            CommutingDistance = commutingDistance;
            FreizeitDistance = freizeitDistance;
            TotalDistance = commutingDistance + freizeitDistance;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        [Obsolete("only for json")]
        public CarDistanceEntry()
        {
        }
        public int ID { get; set; }
        [NotNull]
        public string HouseGuid { get; set; }
        [NotNull]
        public string HouseholdGuid { get; set; }
        [NotNull]
        public string CarGuid { get; set; }
        public double CommutingDistance { get; set; }
        public double FreizeitDistance { get; set; }
        public double TotalDistance { get; set; }
    }
}