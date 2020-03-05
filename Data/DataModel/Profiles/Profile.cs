using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Common;
using Common.Database;
using Data.Database;
using Data.DataModel.Export;
using JetBrains.Annotations;
using MathNet.Numerics.Random;
using MessagePack;
using Newtonsoft.Json;

namespace Data.DataModel.Profiles {
    public enum EnergyOrPower {
        Unknown,
        Energy,
        Power,
        Temperatures
    }

    public enum DisplayUnit {
        Stk,
        GWh,
        Percentage,
        Mw
    }

    [Serializable]
    [SuppressMessage("ReSharper", "RedundantNameQualifier")]
#pragma warning disable CA1724 // Type names should not match namespaces
    [MessagePackObject]
    public class Profile : BasicSaveable<Profile> {
        [NotNull]
        public Profile Add([NotNull] [ItemNotNull] List<Profile> profilesToMerge, [NotNull] string mergedprofiles)
        {
            var newVals = Values.ToList();
            foreach (var other in profilesToMerge) {
                for (int i = 0; i < other.Values.Count; i++) {
                    newVals[i] += other.Values[i];
                }
            }

            return new Profile(mergedprofiles, newVals.AsReadOnly(), EnergyOrPower);
        }

        [NotNull]
        public List<double> Get5BoxPlotValues()
        {
            List<double> result = new List<double>();
            var sortedVals = Values.OrderBy(x => x).ToList();
            if (sortedVals.Count == 0) {
                throw new FlaException("no values");
            }

            int count = sortedVals.Count;
            result.Add(sortedVals[0]);
            result.Add(sortedVals[(int)(count / 4.0)]);
            result.Add(sortedVals[(int)(count / 2.0)]);
            result.Add(sortedVals[(int)(count / 4.0 * 3)]);
            result.Add(sortedVals[count - 1]);
            return result;
        }

        public double GetAverageDailyDurations()
        {
            if (Values.Count != 35040) {
                throw new FlaException("Invalid value count");
            }

            int idx = 0;
            List<int> dayCounts = new List<int>();
            for (int i = 0; i < Values.Count; i += 96) {
                int daycount = 0;
                for (int day = 0; day < 96; day++) {
                    if (Math.Abs(Values[idx]) > 0.00000000001) {
                        daycount++;
                    }

                    idx++;
                }

                dayCounts.Add(daycount);
            }

            return dayCounts.Average();
        }

        public bool IsValidSingleYearProfile()
        {
            if (Values.Count == 35040) {
                return true;
            }

            return false;
        }

        [NotNull]
        public Profile LimitNegativeToPercentageOfMax(double d)
        {
            double newMaxVal = Values.Min() * d;
            List<double> newVals = new List<double>();
            foreach (var val in Values) {
                if (val < newMaxVal) {
                    newVals.Add(newMaxVal);
                }
                else {
                    newVals.Add(val);
                }
            }

            return new Profile(Name, newVals.AsReadOnly(), EnergyOrPower);
        }

        [NotNull]
        public Profile LimitPositiveToPercentageOfMax(double d)
        {
            double newMaxVal = Values.Max() * d;
            List<double> newVals = new List<double>();
            foreach (var val in Values) {
                if (val > newMaxVal) {
                    newVals.Add(newMaxVal);
                }
                else {
                    newVals.Add(val);
                }
            }

            return new Profile(Name, newVals.AsReadOnly(), EnergyOrPower);
        }

        public double MaxPower()
        {
            if (EnergyOrPower != EnergyOrPower.Energy) {
                throw new NotImplementedException();
            }

            if (Values.Count < 35000 || Values.Count > 36000) {
                throw new NotImplementedException();
            }

            return Values.Max() * 4;
        }

        public double MinPower()
        {
            if (EnergyOrPower != EnergyOrPower.Energy) {
                throw new NotImplementedException();
            }

            if (Values.Count < 35000 || Values.Count > 36000) {
                throw new NotImplementedException();
            }

            return Values.Min() * 4;
        }

        [Key(6)]
        public DisplayUnit DisplayUnit { get; set; }

        protected override void SetAdditionalFieldsForRow([NotNull] RowBuilder rb)
        {
            rb.Add("Name", Name).Add("ProfileType", EnergyOrPower).Add("ValueCount", Values.Count).Add("EnergySum", Values.Sum());
        }

        protected override void SetFieldListToSaveOtherThanMessagePack([NotNull] Action<string, SqliteDataType> addField)
        {
            addField("Name", SqliteDataType.Text);
            addField("ProfileType", SqliteDataType.Integer);
            addField("ValueCount", SqliteDataType.Integer);
            addField("EnergySum", SqliteDataType.Double);
        }

        public enum ProfileResolution {
            Hourly,
            QuarterHour
        }

        [Obsolete("only for json")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public Profile()
        {
        }

        public Profile([NotNull] JsonSerializableProfile jsp)
        {
            Name = jsp.Name;
            Values = jsp.Values;
            EnergyOrPower = jsp.EnergyOrPower;
        }

        public Profile([JetBrains.Annotations.NotNull] string name,
                       [JetBrains.Annotations.NotNull] ReadOnlyCollection<double> values,
                       EnergyOrPower energyOrPower)
        {
            EnergyOrPower = energyOrPower;
            if (EnergyOrPower == EnergyOrPower.Unknown) {
                throw new Exception("Unknown profile");
            }

            Values = values;
            Name = name;
        }

        public Profile([NotNull] Profile other)
        {
            EnergyOrPower = other.EnergyOrPower;
            if (other.EnergyOrPower == EnergyOrPower.Unknown) {
                throw new Exception("Unknown profile");
            }

            Values = other.Values;
            Name = other.Name;
            SourceFileName = other.SourceFileName;
            SourceFileLength = other.SourceFileLength;
            SourceFileDate = other.SourceFileDate;
        }

        public Profile([NotNull] Profile other, [JetBrains.Annotations.NotNull] ReadOnlyCollection<double> newValues)
        {
            EnergyOrPower = other.EnergyOrPower;
            if (other.EnergyOrPower == EnergyOrPower.Unknown) {
                throw new Exception("Unknown profile");
            }

            Values = newValues;
            Name = other.Name;
            SourceFileName = other.SourceFileName;
            SourceFileLength = other.SourceFileLength;
            SourceFileDate = other.SourceFileDate;
        }

        [Key(0)]
        public EnergyOrPower EnergyOrPower { get; set; }

        [JetBrains.Annotations.NotNull]
        [Key(1)]
        public string Name { get; set; }

        [JetBrains.Annotations.NotNull]
        [JsonIgnore]
        [MessagePack.IgnoreMember]
        public ReadOnlyCollection<double> Values { get; private set; }

        [JsonIgnore]
        [Key(2)]
        [Obsolete("Only for messagepack!")]
        [NotNull]
        public List<double> ValuesAsList {
            get => Values.ToList();
            set => Values = value.AsReadOnly();
        }

        [CanBeNull]
        [Key(3)]
        public string SourceFileName { get; set; }

        [Key(4)]
        public long SourceFileLength { get; set; }

        [Key(5)]
        public DateTime SourceFileDate { get; set; }

        [NotNull]
        public Profile AdjustValueCountForLeapYear()
        {
            if (Values.Count == 35040) {
                return this;
            }

            ReadOnlyCollection<double> values;
            if (Values.Count == 35136) {
                var list = Values.ToList();
                list.RemoveRange(35040, 35136 - 35040);
                values = list.AsReadOnly();
            }
            else {
                throw new FlaException("Invalid value count?");
            }

            return new Profile(this, values);
        }

        [JetBrains.Annotations.NotNull]
        public Profile Add([JetBrains.Annotations.NotNull] Profile other, [JetBrains.Annotations.NotNull] string name)
        {
            ValidateCompatibiltiy(other);
            var vals = new List<double>();
            for (var i = 0; i < Values.Count; i++) {
                vals.Add(Values[i] + other.Values[i]);
            }

            var p = new Profile(other, vals.AsReadOnly());
            p.Name = name;
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public Profile Subtract([JetBrains.Annotations.NotNull] Profile other, [JetBrains.Annotations.NotNull] string name)
        {
            ValidateCompatibiltiy(other);
            var vals = new List<double>();
            for (var i = 0; i < Values.Count; i++) {
                vals.Add(Values[i] - other.Values[i]);
            }

            var p = new Profile(other, vals.AsReadOnly());
            p.Name = name;
            return p;
        }

        [NotNull]
        public Profile Add([NotNull] ReadOnlyCollection<double> other)
        {
            if (other.Count != Values.Count) {
                throw new FlaException("invalid value count");
            }

            var vals = new List<double>();
            for (var i = 0; i < Values.Count; i++) {
                vals.Add(Values[i] + other[i]);
            }

            var p = new Profile(Name, vals.AsReadOnly(), EnergyOrPower);
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public Profile ConvertFromEnergyToPower()
        {
            if (EnergyOrPower != EnergyOrPower.Energy) {
                throw new Exception("wrong source type profile");
            }

            if (Values.Count > 35000 && Values.Count < 36000) {
                //15 min values
                List<double> vals = new List<double>();
                for (int i = 0; i < Values.Count; i++) {
                    vals.Add(Values[i] * 4);
                }

                Profile p = new Profile(this, vals.AsReadOnly()) {EnergyOrPower = EnergyOrPower.Energy};
                return p;
            }

            throw new Exception("Unknown conversion faktor");
        }

        [JetBrains.Annotations.NotNull]
        public Profile ConvertFromPowerToEnergy()
        {
            if (EnergyOrPower != EnergyOrPower.Power) {
                throw new Exception("wrong source type profile for profile " + Name);
            }

            if (Values.Count > 35000 && Values.Count < 36000) {
                //15 min values
                List<double> vals = new List<double>();
                for (int i = 0; i < Values.Count; i++) {
                    vals.Add(Values[i] / 4);
                }

                Profile p = new Profile(this, vals.AsReadOnly()) {EnergyOrPower = EnergyOrPower.Energy};
                return p;
            }

            throw new Exception("Unknown conversion faktor");
        }

        public static int CountProfiles([JetBrains.Annotations.NotNull] MyDb db, SaveableEntryTableType saveableEntryTableType)
        {
            var query = "select count(*) from " + nameof(Profile) + "_" + saveableEntryTableType;

            using (var con = new SQLiteConnection(db.GetConnectionstring())) {
                var cmd = new SQLiteCommand(con) {
                    CommandText = query
                };
                con.Open();
                object o = cmd.ExecuteScalar();
                if (o == null) {
                    throw new FlaException("Result was null");
                }

                int rows = (int)(long)o;
                con.Close();
                return rows;
            }
        }

        public double EnergyDuringDay()
        {
            if (Values.Count < 35000) {
                throw new FlaException("Not the right number of values: " + Values.Count);
            }

            if (Values.Count > 36000) {
                throw new FlaException("Not the right number of values: " + Values.Count);
            }

            double sum = 0;
            for (int i = 0; i < Values.Count; i++) {
                int mod = i % 96; //96 timesteps in 15 min
                if (mod >= 28 && mod <= 84) {
                    sum += Values[i];
                }
            }

            return sum;
        }

        public double EnergyDuringNight()
        {
            if (Values.Count < 35000) {
                throw new FlaException("Not the right number of values: " + Values.Count);
            }

            if (Values.Count > 36000) {
                throw new FlaException("Not the right number of values: " + Values.Count);
            }

            double sum = 0;
            for (int i = 0; i < Values.Count; i++) {
                int mod = i % 96; //96 timesteps in 15 min
                if (mod < 28) {
                    sum += Values[i];
                }

                if (mod > 84) {
                    sum += Values[i];
                }
            }

            return sum;
        }

        [CanBeNull] private double? _cachedEnergySum;

        public double EnergySum()
        {
            if (_cachedEnergySum != null) {
                return _cachedEnergySum.Value;
            }

            if (EnergyOrPower == EnergyOrPower.Power) {
                if (Values.Count > 30000 && Values.Count < 40000) {
                    _cachedEnergySum = Values.Sum() / 4;
                    return _cachedEnergySum.Value;
                }

                if (Values.Count > 8000 && Values.Count < 9000) {
                    _cachedEnergySum = Values.Sum();
                    return _cachedEnergySum.Value;
                }

                if (Values.Count == 2 && Constants.MakeDummyProfilesOnly) {
                    _cachedEnergySum = Values.Sum();
                    return _cachedEnergySum.Value;
                }
            }
            else {
                _cachedEnergySum = Values.Sum();
                return _cachedEnergySum.Value;
            }

            throw new Exception("Unknown conversion factor");
        }

        [JetBrains.Annotations.NotNull]
        public BarSeriesEntry GetBarSeries()
        {
            var bse = new BarSeriesEntry(Name);
            bse.Values.AddRange(Values);
            return bse;
        }

        [JetBrains.Annotations.NotNull]
        public string GetCSVLine()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Values.Count; i++) {
                sb.Append(Values[i].ToString(CultureInfo.InvariantCulture)).Append(";");
            }

            var str = sb.ToString();
            if (str.EndsWith(";", StringComparison.Ordinal)) {
                str = str.Substring(0, str.Length - 1);
            }

            return str;
        }


        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<LineSeriesEntry> GetLineSeriesEntriesList()
        {
            var l = new List<LineSeriesEntry> {GetLineSeriesEntry()};
            return l;
        }

        [JetBrains.Annotations.NotNull]
        public LineSeriesEntry GetLineSeriesEntry()
        {
            var lse = new LineSeriesEntry(Name);
            for (var i = 0; i < Values.Count; i++) {
                lse.Values.Add(new Point(i, Values[i]));
            }

            return lse;
        }

        [JetBrains.Annotations.NotNull]
        public Profile GetOnlyNegative([JetBrains.Annotations.NotNull] string name)
        {
            var vals = new List<double>();
            foreach (var d in Values) {
                if (d < 0) {
                    vals.Add(d);
                }
                else {
                    vals.Add(0);
                }
            }

            var pNew = new Profile(this, vals.AsReadOnly());
            pNew.Name = name;
            return pNew;
        }

        [JetBrains.Annotations.NotNull]
        public Profile GetOnlyPositive([JetBrains.Annotations.NotNull] string name)
        {
            var vals = new List<double>();
            foreach (var d in Values) {
                if (d > 0) {
                    vals.Add(d);
                }
                else {
                    vals.Add(0);
                }
            }

            var pNew = new Profile(this, vals.AsReadOnly()) {Name = name};
            return pNew;
        }

        [JetBrains.Annotations.NotNull]
        public static Profile MakeRandomProfile([NotNull] Random rnd, [JetBrains.Annotations.NotNull] string name, ProfileResolution resolution)
        {
            var values = new List<double>();
            int count;
            switch (resolution) {
                case ProfileResolution.Hourly:
                    count = 8760;
                    break;
                case ProfileResolution.QuarterHour:
                    count = 8760 * 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }

            values.AddRange(rnd.NextDoubles(count) ?? throw new FlaException("rnd broken"));

            var p = new Profile(name, values.AsReadOnly(), EnergyOrPower.Energy);
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public static Profile MakeRandomProfile([NotNull] Random rnd,
                                                [JetBrains.Annotations.NotNull] string name,
                                                ProfileResolution resolution,
                                                double minimum,
                                                double maximum)
        {
            var values = new List<double>();
            int count;
            switch (resolution) {
                case ProfileResolution.Hourly:
                    count = 8760;
                    break;
                case ProfileResolution.QuarterHour:
                    count = 8760 * 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
            }

            values.AddRange(rnd.NextDoubles(count) ?? throw new FlaException("rnd broken"));
            double range = maximum - minimum;
            for (int i = 0; i < values.Count; i++) {
                values[i] = values[i] * range + minimum;
            }

            var p = new Profile(name, values.AsReadOnly(), EnergyOrPower.Energy);
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public static Profile MakeConstantProfile(double yearlySum, [JetBrains.Annotations.NotNull] string name, ProfileResolution resolution)
        {
            var values = new List<double>();
            if (Constants.MakeDummyProfilesOnly) {
                values.Add(yearlySum / 2);
                values.Add(yearlySum / 2);
                var p = new Profile(name, values.AsReadOnly(), EnergyOrPower.Energy);
                return p;
            }

            {
                const int yearlyHours = 8760;
                int count;
                switch (resolution) {
                    case ProfileResolution.Hourly:
                        count = yearlyHours;
                        break;
                    case ProfileResolution.QuarterHour:
                        count = yearlyHours * 4;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
                }

                var val = yearlySum / count;
                for (var i = 0; i < count; i++) {
                    values.Add(val);
                }

                var p = new Profile(name, values.AsReadOnly(), EnergyOrPower.Energy);
                return p;
            }
        }

        [JetBrains.Annotations.NotNull]
        public Profile MakeCoolingDegreeHours([JetBrains.Annotations.NotNull] string targetName, double coolingBorder, double yearlyTotal)
        {
            var values = new List<double>();
            //jahressumme berechnen
            double yearlyCoolingHourSum = 0;
            foreach (var hourlyTemp in Values) {
                if (hourlyTemp > coolingBorder) {
                    yearlyCoolingHourSum += hourlyTemp - 20;
                }
            }

            foreach (var hourlyTemp in Values) {
                double hourlyValue = 0;
                if (hourlyTemp > coolingBorder) {
                    hourlyValue = (hourlyTemp - 20) / yearlyCoolingHourSum * yearlyTotal;
                }

                values.Add(hourlyValue);
            }

            var targetProfile = new Profile(this, values.AsReadOnly()) {Name = targetName};

            return targetProfile;
        }


        [JetBrains.Annotations.NotNull]
        public Profile MakeDailyAverages()
        {
            int factor;
            if (Values.Count > 8000 && Values.Count < 9000) {
                factor = 24;
            }
            else if (Values.Count > 35000 && Values.Count < 36000) {
                factor = 24 * 4;
            }
            else {
                throw new Exception("Error, unknown factor");
            }

            var vals = new List<double>();
            for (var i = 0; i < Values.Count; i += factor) {
                double sum = 0;
                for (var j = 0; j < factor; j++) {
                    sum += Values[i + j];
                }

                vals.Add(sum / factor);
            }

            var p = new Profile(this, vals.AsReadOnly()) {Name = Name + "-daily-averages"};
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public Profile MakeDegreeDayPowerProfile([JetBrains.Annotations.NotNull] string targetName, double heizgrenze, double yearlyTotal)
        {
            var dailyProfile = MakeDailyAverages();
            var values = new List<double>();
            //jahressumme berechnen
            double yearlyHeizGradSum = 0;
            for (var i = 0; i < dailyProfile.Values.Count; i++) {
                var dailyTemp = dailyProfile.Values[i];
                if (dailyTemp < heizgrenze) {
                    yearlyHeizGradSum += 20 - dailyTemp;
                }
            }

            for (var i = 0; i < dailyProfile.Values.Count; i++) {
                var dailyTemp = dailyProfile.Values[i];
                double hourlyValue = 0;
                if (dailyTemp < heizgrenze) {
                    var heizgrad = (20 - dailyTemp) / yearlyHeizGradSum * yearlyTotal;
                    hourlyValue = heizgrad / 24;
                }

                for (var j = 0; j < 24; j++) {
                    values.Add(hourlyValue);
                }
            }

            var targetProfile = new Profile(this, values.AsReadOnly()) {Name = targetName};
            return targetProfile;
        }

        [JetBrains.Annotations.NotNull]
        public Profile MakeHourlyAverages()
        {
            int factor;
            if (Values.Count > 8000 && Values.Count < 9000) {
                factor = 1;
            }
            else if (Values.Count > 35000 && Values.Count < 36000) {
                factor = 4;
            }
            else if (Values.Count == 2 && Constants.MakeDummyProfilesOnly) {
                factor = 1;
            }
            else {
                throw new Exception("Error, unknown factor");
            }

            var vals = new List<double>();
            for (var i = 0; i < Values.Count; i += factor) {
                double sum = 0;
                for (var j = 0; j < factor; j++) {
                    sum += Values[i + j];
                }

                vals.Add(sum / factor);
            }

            var p = new Profile(this, vals.AsReadOnly()) {Name = Name + "-hourly-averages"};
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public Profile Minus([JetBrains.Annotations.NotNull] Profile otherProfile, [JetBrains.Annotations.NotNull] string name)
        {
            ValidateCompatibiltiy(otherProfile);
            var vals = new List<double>();
            for (var i = 0; i < Values.Count; i++) {
                vals.Add(Values[i] - otherProfile.Values[i]);
            }

            var pNew = new Profile(this, vals.AsReadOnly()) {Name = name};
            return pNew;
        }

        [JetBrains.Annotations.NotNull]
        public Profile MultiplyWith(double factor, [JetBrains.Annotations.NotNull] string name)
        {
            var vals = new List<double>();
            foreach (var d in Values) {
                vals.Add(d * factor);
            }

            var pNew = new Profile(this, vals.AsReadOnly()) {Name = name};
            return pNew;
        }

        [JetBrains.Annotations.NotNull]
        public Profile ReduceTimeResolutionFrom1MinuteTo15Minutes()
        {
            List<double> newValues = new List<double>();
            for (int i = 0; i < Values.Count; i += 15) {
                double tmpSum = 0;
                int c = 0;
                for (int j = 0; j < 15 && i + j < Values.Count; j++) {
                    tmpSum += Values[i + j];
                    c++;
                }

                tmpSum /= c;
                newValues.Add(tmpSum);
            }

            var p = new Profile(this, newValues.AsReadOnly()) {Name = Name};
            return p;
        }

        [JetBrains.Annotations.NotNull]
        public Profile ScaleToTargetSum(double targetSum, [JetBrains.Annotations.NotNull] string name, out double usedFactor)
        {
            var pvsum = Values.Sum();
            var factor = targetSum / pvsum;
            if (double.IsNaN(factor)) {
                factor = 0;
            }

            if (double.IsInfinity(factor)) {
                factor = 0;
            }

            usedFactor = factor;
            return MultiplyWith(factor, name);
        }


        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private void ValidateCompatibiltiy([JetBrains.Annotations.NotNull] Profile other)
        {
            if (other.EnergyOrPower != EnergyOrPower) {
                throw new Exception("Trying to merge Energy with Power");
            }

            if (other.Values.Count != Values.Count) {
                throw new Exception("Unequal value count: " + Name + ": " + Values.Count + " vs. " + other.Name + ": " + other.Values.Count);
            }
        }

        [NotNull]
        public override string ToString() => Name + " (" + Values.Count + ")";
#pragma warning restore CA1724 // Type names should not match namespaces
    }
}