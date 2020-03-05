using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    // ReSharper disable once InconsistentNaming
    public class SLPProvider {
        private readonly DateTime _autumEnd;
        private readonly DateTime _autumStart;
        [NotNull] [ItemNotNull] private readonly List<Feiertag> _feiertags;

        [NotNull] private readonly Dictionary<string, Profile> _predefinedProfiles = new Dictionary<string, Profile>();

        [NotNull] private readonly Dictionary<string, List<VDEWProfileValue>> _profiles;
        private readonly DateTime _summerbegin;
        private readonly DateTime _winter1Enddate;
        private readonly int _year;

        public SLPProvider(int year,
                           [NotNull] [ItemNotNull] List<VDEWProfileValue> vdewProfileValues,
                           [NotNull] [ItemNotNull] List<FeiertagImport> feiertageRaw)
        {
            _year = year;
            _winter1Enddate = new DateTime(_year, 3, 21);
            _summerbegin = new DateTime(year, 05, 15);
            _autumStart = new DateTime(year, 9, 15);
            _autumEnd = new DateTime(year, 10, 31);
            var profileNames = vdewProfileValues.Select(x => x.ProfileName).Distinct().ToList();
            _profiles = new Dictionary<string, List<VDEWProfileValue>>();
            foreach (var profileName in profileNames) {
                var values = vdewProfileValues.Where(x => x.ProfileName == profileName).ToList();
                _profiles.Add(profileName, values);
            }

            _feiertags = new List<Feiertag>();
            var filteredfeiertage = feiertageRaw.Where(x => x.Date.Year == year).ToList();
            foreach (var import in filteredfeiertage) {
                _feiertags.Add(new Feiertag(import.Name, import.Date));
            }
        }

        public Season GetSeason(DateTime dt)
        {
            if (dt < _winter1Enddate) {
                return Season.Winter;
            }

            if (dt >= _summerbegin && dt < _autumStart) {
                return Season.Sommer;
            }

            if (dt > _autumEnd) {
                return Season.Winter;
            }

            return Season.Uebergang;
        }

        [NotNull]
        public Profile Run([NotNull] string profileName, double targetValue)
        {
            if (Constants.MakeDummyProfilesOnly) {
                var tmpvals = new List<double> {
                    targetValue / 2,
                    targetValue / 2
                };
                var p1 = new Profile(profileName, tmpvals.AsReadOnly(), EnergyOrPower.Energy);
                return p1;
            }

            if (_predefinedProfiles.ContainsKey(profileName)) {
                return _predefinedProfiles[profileName].ScaleToTargetSum(targetValue, "SLP " + profileName, out var _);
            }

            var fvals = _profiles[profileName];
            var valDict = new Dictionary<string, VDEWProfileValue>();
            foreach (var fval in fvals) {
                valDict.Add(MakeKey(fval.Minutes, fval.Season, fval.TagTyp), fval);
            }

            var dt = new DateTime(_year, 1, 1);
            var dstvalues = new List<double>();
            for (var i = 0; i < 365; i++) {
                TagTyp tagTyp;
                var dayOfWeek = dt.DayOfWeek;
                if (IsFeiertag(dt)) {
                    tagTyp = TagTyp.Sonntag;
                }
                else if (dayOfWeek == DayOfWeek.Sunday) {
                    tagTyp = TagTyp.Sonntag;
                }
                else if (dayOfWeek == DayOfWeek.Saturday) {
                    tagTyp = TagTyp.Samstag;
                }
                else {
                    tagTyp = TagTyp.Werktag;
                }

                var dayNumber = i + 1;
                var dynamisierungsfaktor = -3.92 * Math.Pow(10, -10) * Math.Pow(dayNumber, 4) + 3.2 * Math.Pow(10, -7) * Math.Pow(dayNumber, 3) -
                                           7.02 * Math.Pow(10, -5) * Math.Pow(dayNumber, 2) + 2.1 * Math.Pow(10, -3) * dayNumber + 1.24;
                var season = GetSeason(dt);
                var day = dt;
                for (var period = 0; period < 96; period++) {
                    var val = GetValue(dt, season, tagTyp, day, valDict);
                    dstvalues.Add(val.Value * dynamisierungsfaktor);
                    dt = dt.AddMinutes(15);
                }
            }

            var p = new Profile("SLP " + profileName, dstvalues.AsReadOnly(), EnergyOrPower.Energy);
            _predefinedProfiles.Add(profileName, p);
            p = p.ScaleToTargetSum(targetValue, "SLP " + profileName, out var _);
            return p;
        }

        [NotNull]
        private static VDEWProfileValue GetValue(DateTime time,
                                                 Season season,
                                                 TagTyp tagTyp,
                                                 DateTime day,
                                                 [NotNull] Dictionary<string, VDEWProfileValue> valDict)
        {
            var ts = time - day;
            var minutes = (int)ts.TotalMinutes;
            var val = valDict[MakeKey(minutes, season, tagTyp)];
            return val;
        }

        private bool IsFeiertag(DateTime dt)
        {
            return _feiertags.Any(x => x.IsSameDate(dt));
        }

        [NotNull]
        private static string MakeKey(int minutes, Season season, TagTyp tagTyp) => season + "$" + tagTyp + "$" + minutes;
    }
}