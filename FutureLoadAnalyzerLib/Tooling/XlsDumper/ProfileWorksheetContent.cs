using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Common;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling.Database;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.XlsDumper {

    public class RowWorksheetContent : IRowCollectionProvider {
        public RowWorksheetContent([NotNull] RowCollection rc)
        {
            if (string.IsNullOrWhiteSpace(rc.SheetName)) {
                throw new FlaException("Sheetname was null");
            }

            SheetName = rc.SheetName;
            RowCollection = rc;
        }

        [NotNull]
        public string SheetName { get; }
        [NotNull]
        public RowCollection RowCollection { get; }
    }


    public class ProfileWorksheetContent: IValueProvider {
        public ProfileWorksheetContent([NotNull] string sheetName, [NotNull] string yaxisname, [NotNull] [ItemNotNull] List<Profile> profiles)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) {
                throw new FlaException("Sheetname was null");
            }

            YAxisName = yaxisname;
            SheetName = sheetName;
            Profiles = new List<Profile>();
            foreach (var profile in profiles) {
                if(profile.EnergyOrPower == EnergyOrPower.Energy) {
                    Profiles.Add(profile.ConvertFromEnergyToPower());
                }
                else {
                    Profiles.Add(profile);
                }
            }
        }
        public ProfileWorksheetContent([NotNull] string sheetName, [NotNull] string yaxisname,  [NotNull] string specialProfileName, [NotNull] [ItemNotNull] List<Profile> profiles)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) {
                throw new FlaException("Sheetname was null");
            }
            YAxisName = yaxisname;
            SheetName = sheetName;
            Profiles = new List<Profile>();
            Profile specialProfile = null;
            foreach (var profile in profiles) {
                if (profile.Name == specialProfileName) {
                    specialProfile = profile;
                    continue;
                }
                if (profile.EnergyOrPower == EnergyOrPower.Energy) {
                    Profiles.Add(profile.ConvertFromEnergyToPower());
                }
                else {
                    Profiles.Add(profile);
                }

            }

            if (specialProfile == null) {
                throw new FlaException("Could not find special profile " + specialProfileName);
            }
            if (specialProfile.EnergyOrPower == EnergyOrPower.Energy) {
                Profiles.Add(specialProfile.ConvertFromEnergyToPower());
            }
            else {
                Profiles.Add(specialProfile);
            }
            SpecialLineColumnIndex = Profiles.Count -1;
        }

        public ProfileWorksheetContent([NotNull] string sheetName, [NotNull] string yaxisname,double chartHeight, [NotNull] [ItemNotNull] params Profile[] profiles)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) {
                throw new FlaException("Sheetname was null");
            }
            YAxisName = yaxisname;
            ChartHeight = chartHeight;
            SheetName = sheetName;
            Profiles = new List<Profile>();
            foreach (var profile in profiles) {
                if (profile.EnergyOrPower == EnergyOrPower.Energy) {
                    Profiles.Add(profile.ConvertFromEnergyToPower());
                }
                else {
                    Profiles.Add(profile);
                }
            }
        }

        [NotNull]
        [ItemNotNull]
        public List<Profile> Profiles { get; }

        [NotNull]
        public string SheetName { get; }

        [NotNull]
        public ReadOnlyCollection<T> GetValues<T>(int column) => Profiles[column].Values as ReadOnlyCollection<T> ?? throw new InvalidOperationException();


        [NotNull]
        public List<string> GetColumnNames() => Profiles.Select(x=> x.Name).ToList();

        [NotNull]
        public Type ReturnType => typeof(double);

        public int SpecialLineColumnIndex { get; set; } = -1;
        [NotNull]
        public string YAxisName { get; }

        [CanBeNull]
        public string GetUnit  => Profiles.First().DisplayUnit.ToString();
        [CanBeNull]
        public double? ChartHeight { get; }

        public int GetColumnCount() => Profiles.Count;

    }
}