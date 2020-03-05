using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class J2_AssignIncomingCommuterData : RunableWithBenchmark {
        public J2_AssignIncomingCommuterData([NotNull] ServiceRepository services)
            : base(nameof(J2_AssignIncomingCommuterData), Stage.Houses, 1001, services, false)
        {
            DevelopmentStatus.Add("//TODO: make better probabilities based on businessize / energy consumption");
        }

        protected override void RunChartMaking()
        {
            var slice = Constants.PresentSlice;
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouse.Fetch<House>();
            //var businesses = dbHouse.Fetch<BusinessEntry>();
            var incoming = dbHouse.Fetch<IncomingCommuterEntry>();
            MakeCommuterSankey();
            MakeCommuterMap();

            void MakeCommuterSankey()
            {
                var ssa = new SingleSankeyArrow("IncomingComuters", 1500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Arbeiter", incoming.Count, 5000, Orientation.Straight));
                var workersFromOutside = incoming.Count(x => x.DistanceInKm > 0);
                var workersFromBurgdorf = incoming.Count(x => Math.Abs(x.DistanceInKm) < 0.000001);

                ssa.AddEntry(new SankeyEntry("Pendler nach Burgdorf", workersFromOutside * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Arbeiter in Burgdorf", workersFromBurgdorf * -1, 2000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }


            void MakeCommuterMap()
            {
                RGBWithSize GetColor(House h)
                {
                    var incomingCount = incoming.Count(x => x.HouseGuid == h.HouseGuid);
                    if (incomingCount > 0) {
                        return new RGBWithSize(Constants.Red, incomingCount + 10);
                    }

                    return new RGBWithSize(Constants.Black, 10);
                }

                var mapPoints = houses.Select(x => x.GetMapPointWithSize(GetColor)).ToList();

                var filename = MakeAndRegisterFullFilename("Employees.svg", Name, "", slice);
                var legendEntries = new List<MapLegendEntry> {
                    new MapLegendEntry("Betrieb mit Angestellten", Constants.Red),
                    new MapLegendEntry("Keine Angestellten", Constants.Black)
                };
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries, MyStage);
            }
        }


        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<IncomingCommuterEntry>(Stage.Houses, Constants.PresentSlice);
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var businesses = dbHouses.Fetch<BusinessEntry>();
            var incomings = dbRaw.Fetch<IncomingCommuter>();
            if (incomings.Count == 0) {
                throw new Exception("No occupants found");
            }

            var entries = new List<IncomingCommuterEntry>();

            foreach (var commuter in incomings) {
                for (var i = 0; i < commuter.Erwerbstätige; i++) {
                    var oce = new IncomingCommuterEntry {
                        CommuterGuid = Guid.NewGuid().ToString()
                    };
                    var method = Services.Rnd.NextDouble();
                    //https://www.bfs.admin.ch/bfs/de/home/statistiken/mobilitaet-verkehr/personenverkehr/pendlermobilitaet.html
                    if (method < 0.52) {
                        oce.CommuntingMethod = CommuntingMethod.Car;
                    }
                    else {
                        oce.CommuntingMethod = CommuntingMethod.PublicTransport;
                    }

                    oce.DistanceInKm = commuter.Entfernung;
                    oce.Wohngemeinde = commuter.Wohngemeinde;
                    oce.WohnKanton = commuter.Wohnkanton;
                    entries.Add(oce);
                }
            }

            dbHouses.BeginTransaction();
            while (entries.Count > 0) {
                var ogce = entries[0];
                entries.RemoveAt(0);
                // make better probabilities based on businessize / energy consumption
                var business = businesses[Services.Rnd.Next(businesses.Count)];
                ogce.BusinessGuid = business.BusinessGuid;
                ogce.HouseGuid = business.HouseGuid;
                ogce.CommuterGuid = Guid.NewGuid().ToString();
                dbHouses.Save(ogce);
            }

            dbHouses.CompleteTransaction();
        }
    }
}