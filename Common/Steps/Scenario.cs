using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Common.Steps {
    public sealed class Scenario : IEquatable<Scenario> {

        [NotNull]
        public string FriendlyName => ChartHelpers.GetFriendlyScenarioName(Name);

        public Scenario([NotNull] string name) => Name = name;

        [NotNull]
        public string Name { get; }

        [NotNull]
        public string ShortName {
            get {
                if (Name.Length < 25) {
                    return Name;
                }

                return Name.Substring(0, 25);
            }
        }

        public bool Equals([CanBeNull] Scenario other)
        {
            if (other == null) {
                return false;
            }

            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != GetType()) {
                return false;
            }

            return Equals((Scenario)obj);
        }

        [NotNull]
        public static Scenario FromEnum(ScenarioEnum name) => new Scenario(name.ToString());

        [NotNull]
        public static Scenario FromString([NotNull] string name) => new Scenario(name);

        public override int GetHashCode() => Name.GetHashCode();

        public static bool operator ==([CanBeNull] Scenario pk1, [CanBeNull] Scenario pk2)
        {
            if (Equals(pk1, null) && !Equals(pk2, null)) {
                return false;
            }

            if (Equals(pk1, null)) {
                //pk2 == null!
                return true;
            }

            return pk1.Equals(pk2);
        }

        public static bool operator ==([CanBeNull] Scenario pk1, ScenarioEnum pk2)
        {
            if (pk1?.Name == null) {
                return false;
            }

            return Equals(pk1.Name, pk2.ToString());
        }

        public static bool operator !=([CanBeNull] Scenario pk1, [CanBeNull] Scenario pk2)
        {
            if (Equals(pk1, null) && !Equals(pk2, null)) {
                return true;
            }

            if (Equals(pk1, null)) {
                //pk2 == null!
                return false;
            }

            return !pk1.Equals(pk2);
        }

        public static bool operator !=([CanBeNull] Scenario pk1, ScenarioEnum pk2)
        {
            if (pk1?.Name == null) {
                return false;
            }

            return !Equals(pk1.Name, pk2.ToString());
        }

        [NotNull]
        public static Scenario Present() => FromEnum(ScenarioEnum.Present);

        [NotNull]
        public override string ToString() => Name;
    }

#pragma warning disable S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum ScenarioEnum {
#pragma warning restore S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
        Present,
        Pom,
        Nep,
        Utopia,
        Dystopia
    }
}