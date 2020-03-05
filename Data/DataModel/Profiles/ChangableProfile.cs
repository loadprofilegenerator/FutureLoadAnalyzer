using System.Linq;
using Common;
using JetBrains.Annotations;

namespace Data.DataModel.Profiles {
    public class ChangableProfile {
        [NotNull] private readonly double[] _values;

        public ChangableProfile([NotNull] double[] values, [NotNull] string name, EnergyOrPower energyOrPower)
        {
            _values = values;
            Name = name;
            EnergyOrPower = energyOrPower;
        }

        public EnergyOrPower EnergyOrPower { get; set; }

        [NotNull]
        public string Name { get; set; }

        public void Add([NotNull] Profile p)
        {
            if (p.Values.Count != _values.Length) {
                throw new FlaException("incompatible counts");
            }

            var vals = p.Values.ToArray();
            for (int i = 0; i < _values.Length; i++) {
                _values[i] += vals[i];
            }
        }

        public void Add([NotNull] ChangableProfile p)
        {
            if (p._values.Length != _values.Length) {
                throw new FlaException("incompatible counts");
            }

            var vals = p._values;
            for (int i = 0; i < _values.Length; i++) {
                _values[i] += vals[i];
            }
        }

        [NotNull]
        public ChangableProfile DeepCopy() => new ChangableProfile(_values.ToArray(), Name, EnergyOrPower);

        [NotNull]
        public static ChangableProfile FromProfile([NotNull] Profile p) => new ChangableProfile(p.Values.ToArray(), p.Name, p.EnergyOrPower);

        public void Subtract([NotNull] Profile p)
        {
            if (p.Values.Count != _values.Length) {
                throw new FlaException("incompatible counts");
            }

            var vals = p.Values.ToArray();
            for (int i = 0; i < _values.Length; i++) {
                _values[i] -= vals[i];
            }
        }

        [NotNull]
        public Profile ToProfile()
        {
            Profile p = new Profile(Name, _values.ToList().AsReadOnly(), EnergyOrPower);
            return p;
        }
    }
}