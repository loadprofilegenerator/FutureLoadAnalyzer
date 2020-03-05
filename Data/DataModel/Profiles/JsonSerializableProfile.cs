using System;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data.DataModel.Profiles {
    public class JsonSerializableProfile {
        [Obsolete("only for json")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public JsonSerializableProfile()
        {
        }

        public JsonSerializableProfile([NotNull] Profile p)
        {
            Name = p.Name;
            Values = p.Values;
            EnergyOrPower = p.EnergyOrPower;
        }


        public JsonSerializableProfile([NotNull] string name,
                                       [NotNull] ReadOnlyCollection<double> values,
                                       EnergyOrPower energyOrPower)
        {
            EnergyOrPower = energyOrPower;
            if (EnergyOrPower == EnergyOrPower.Unknown) {
                throw new Exception("Unknown profile");
            }

            Values = values;
            Name = name;
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public EnergyOrPower EnergyOrPower { get; set; }

        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public ReadOnlyCollection<double> Values { get; set; }

        [NotNull]
        public override string ToString() => Name + " (" + Values.Count + ")";
#pragma warning restore CA1724 // Type names should not match namespaces
    }
}