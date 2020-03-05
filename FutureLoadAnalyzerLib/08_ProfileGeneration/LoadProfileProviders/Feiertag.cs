using System;
using Common;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class Feiertag {
        public Feiertag([NotNull] string name, DateTime date)
        {
            Name = name;
            Date = date;
            _year = date.Year;
            _month = date.Month;
            _day = date.Day;
        }

        [NotNull]
        public string Name { get; }
        public DateTime Date { get; }
        private readonly int _year;
        private readonly int _month;
        private readonly int _day;
        public bool IsSameDate(DateTime dt)
        {
            if (_year != dt.Year) {
                throw new FlaException("Trying to compare the wrong year");
            }

            if (_month == dt.Month && _day == dt.Day) {
                return true;
            }

            return false;
        }
    }
}