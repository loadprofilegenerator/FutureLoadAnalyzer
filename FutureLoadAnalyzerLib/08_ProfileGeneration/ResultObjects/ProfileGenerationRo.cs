using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Data.Database;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects {
    public class ProfileGenerationRo {
        [NotNull] private readonly Dictionary<IHouseComponent, HouseComponentRo> _houseComponenetsRosByComponent =
            new Dictionary<IHouseComponent, HouseComponentRo>();

        [NotNull] private readonly Dictionary<House, HouseRo> _houseRosByHouse = new Dictionary<House, HouseRo>();

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        [NotNull]
        public HouseComponentRo this[[NotNull] IHouseComponent key] => _houseComponenetsRosByComponent[key];
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers

#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
        [NotNull]
        public HouseRo this[[NotNull] House key] => _houseRosByHouse[key];
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers

        [NotNull]
        [ItemNotNull]
        private List<HouseRo> Houses { get; } = new List<HouseRo>();

        [NotNull] private readonly Dictionary<string, List<HausAnschlussRo>> _hausanschlussByObjectId = new Dictionary<string, List<HausAnschlussRo>>();

        [NotNull]
        public HausAnschlussRo AddHausanschluss([NotNull] House house, [NotNull] Hausanschluss hausanschluss, [NotNull] string haStatus)
        {
            string housename = house.ComplexName;
            string hausanschlussName = hausanschluss.Adress;
            string objectId = hausanschluss.ObjectID;
            string trafokreis = hausanschluss.Trafokreis;
            string hausanschlussGuid = hausanschluss.Guid;
            HouseRo housero = Houses.Single(x => x.HouseName == housename);
            if (housero.HausAnschlussList.Any(x => x.HausanschlussGuid == hausanschlussGuid)) {
                throw new FlaException("Hausanschluss already exists:" + objectId);
            }

            HausAnschlussRo haro = new HausAnschlussRo(hausanschlussName, objectId, trafokreis, hausanschlussGuid, hausanschluss.Isn.ToString(),
                hausanschluss.Lon,hausanschluss.Lat, haStatus, hausanschluss.Standort);
            housero.HausAnschlussList.Add(haro);
            haro.AssignmentMethod = hausanschluss.MatchingType.ToString();
            haro.AssignmentDistance = hausanschluss.Distance;
            if (!_hausanschlussByObjectId.ContainsKey(objectId)) {
                _hausanschlussByObjectId.Add(objectId, new List<HausAnschlussRo>());
            }
            _hausanschlussByObjectId[objectId].Add(haro);
            return haro;
        }


        public void AddHouse([NotNull] House house)
        {
            string houseName = house.ComplexName;
            if (Houses.Any(x => x.HouseName == houseName)) {
                throw new FlaException("House already exists");
            }

            var houseRo = new HouseRo(houseName, house.WgsGwrCoordsAsJson, house.LocalWgsPointsAsJson, house.StandortIDsAsJson,
                house.ErzeugerIDsAsJson, house.Adress );
            _houseRosByHouse.Add(house, houseRo);
            Houses.Add(houseRo);
        }

        public void AddHouseComponent([NotNull] House house, [NotNull] Hausanschluss hausanschluss, [NotNull] IHouseComponent component)
        {
            string houseName = house.ComplexName;
            string hausanschlussGuid = hausanschluss.Guid;
            string name = component.Name;
            var housecomponenetType = component.HouseComponentType;
            double lowVoltageEnergy = component.LocalnetLowVoltageYearlyTotalElectricityUse;
            double highVoltageEnergy = component.LocalnetHighVoltageYearlyTotalElectricityUse;

            const string processingStatus = "unkown;";
            var houseRo = Houses.Single(x => x.HouseName == houseName);
            var hausanschlussRo = houseRo.HausAnschlussList.Single(x => x.HausanschlussGuid == hausanschlussGuid);
            HouseComponentRo hc = new HouseComponentRo(
                name, housecomponenetType.ToString(), lowVoltageEnergy,
                highVoltageEnergy, processingStatus,
                JsonConvert.SerializeObject(component.OriginalISNs, Formatting.Indented), component.Standort,component.EffectiveEnergyDemand);
            hausanschlussRo.HouseComponents.Add(hc);
            _houseComponenetsRosByComponent.Add(component, hc);
        }

        public void DumpToExcel([NotNull] string dstPath, XlsResultOutputMode mode)
        {
            RowCollection rc = new RowCollection("GeneratedLoadProfiles", "GeneratedLoadProfiles");
            if (mode == XlsResultOutputMode.ByTrafoStationTree) {
                var trafostationen = Houses.SelectMany(x => x.HausAnschlussList).Select(y => y.Trafokreis).Distinct().ToList();
                foreach (var trafostation in trafostationen) {
                    rc.Add(RowBuilder.Start("Trafostation", trafostation));
                    foreach (var houseRo in Houses) {
                        if (houseRo.HausAnschlussList.Any(x => x.Trafokreis == trafostation)) {
                            rc.Add(houseRo.ToRowBuilder());
                            foreach (HausAnschlussRo hausAnschlussRo in houseRo.HausAnschlussList) {
                                if (hausAnschlussRo.Trafokreis == trafostation) {
                                    rc.Add(hausAnschlussRo.ToRowBuilder(houseRo, mode));
                                    foreach (HouseComponentRo component in hausAnschlussRo.HouseComponents) {
                                        rc.Add(component.ToRowBuilder(houseRo, hausAnschlussRo, mode));
                                    }
                                }
                            }
                        }
                    }
                }
            }else if (mode == XlsResultOutputMode.ByTrafoStationHausanschlussTree) {
                var trafostationen = Houses.SelectMany(x => x.HausAnschlussList).Select(y => y.Trafokreis).Distinct().ToList();
                var haros = Houses.SelectMany(x => x.HausAnschlussList).Distinct().ToList();
                haros.Sort((x,y)=> String.Compare(x.ObjektID, y.ObjektID, StringComparison.Ordinal));
                foreach (var trafostation in trafostationen) {
                    rc.Add(RowBuilder.Start("Trafostation", trafostation));
                    var filteredHaros = haros.Where(x => x.Trafokreis == trafostation);
                    foreach (var anschlussRo in filteredHaros) {
                        rc.Add(anschlussRo.ToRowBuilder(null,XlsResultOutputMode.ByTrafoStationHausanschlussTree));
                        var housesForAnschluss = Houses.Where(x => x.HausAnschlussList.Contains(anschlussRo)).ToList();
                        foreach (var houseRo in housesForAnschluss) {
                            rc.Add(houseRo.ToRowBuilder());
                            foreach (HouseComponentRo component in anschlussRo.HouseComponents) {
                                rc.Add(component.ToRowBuilder(houseRo, anschlussRo, mode));
                            }
                        }
                    }
                }
            }
            else {
                foreach (HouseRo house in Houses) {
                    if (mode == XlsResultOutputMode.Tree) {
                        rc.Add(house.ToRowBuilder());
                    }

                    foreach (var anschlussRo in house.HausAnschlussList) {
                        if (mode == XlsResultOutputMode.Tree) {
                            rc.Add(anschlussRo.ToRowBuilder(house, mode));
                        }

                        foreach (var component in anschlussRo.HouseComponents) {
                            rc.Add(component.ToRowBuilder(house, anschlussRo, mode));
                        }
                    }
                }
            }

            XlsxDumper.WriteToXlsx(dstPath, rc);
        }

        [NotNull]
        public HouseRo FindByHousename([NotNull] string houseName)
        {
            foreach (var value in _houseRosByHouse.Values) {
                if (value.HouseName == houseName) {
                    return value;
                }
            }

            throw new FlaException("Could not find house " + houseName);
        }

        [NotNull]
        [ItemNotNull]
        public List<HausAnschlussRo> HausanschlussByObjectId([NotNull] string objectid)
        {
            return _hausanschlussByObjectId[objectid];
        }
    }
}