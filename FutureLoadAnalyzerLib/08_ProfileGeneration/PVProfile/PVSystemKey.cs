using System;
using System.Collections.Generic;
using Common;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public struct PVSystemKey : IEquatable<PVSystemKey> {
        public int Year { get;  }
        public override string ToString() => GetKey();

        public PVSystemKey(int azimut, int tilt, int year)
        {
            azimut += 180;
            if (azimut == 360) {
                azimut = 0;
            }
            if (azimut > 360) {
                throw new FlaException("Azimut of " + azimut + " is not ok");
            }
            if (tilt > 90) {
                throw new FlaException("Tilt > 90 is not ok");
            }

            if (azimut < 0) {
                throw new FlaException("Azimut < 0 is not ok. It was " + azimut);
            }
            Azimut = (azimut / 5) * 5;
            Tilt = (tilt / 5) * 5;
            Year = year;
        }

        public bool Equals(PVSystemKey other) => Year == other.Year && Azimut == other.Azimut && Tilt == other.Tilt;

        private sealed class YearAzimutTiltEqualityComparer : IEqualityComparer<PVSystemKey> {
            public bool Equals(PVSystemKey x, PVSystemKey y)
            {
                return x.Year == y.Year && x.Azimut == y.Azimut && x.Tilt == y.Tilt;
            }

            public int GetHashCode(PVSystemKey obj)
            {
                unchecked {
                    var hashCode = obj.Year;
                    hashCode = (hashCode * 397) ^ obj.Azimut;
                    hashCode = (hashCode * 397) ^ obj.Tilt;
                    return hashCode;
                }
            }
        }

        [NotNull]
        public static IEqualityComparer<PVSystemKey> YearAzimutTiltComparer { get; } = new YearAzimutTiltEqualityComparer();

        public override bool Equals(object obj) => obj is PVSystemKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked {
                var hashCode = Year;
                hashCode = (hashCode * 397) ^ Azimut;
                hashCode = (hashCode * 397) ^ Tilt;
                return hashCode;
            }
        }

        public static bool operator ==(PVSystemKey pk1, PVSystemKey pk2)
        {
            return pk1.Equals(pk2);
        }

        public static bool operator !=(PVSystemKey pk1, PVSystemKey pk2)
        {
            return !pk1.Equals(pk2);
        }

        public int Azimut { get; }
        public int Tilt { get; }

        [NotNull]
        public string GetLine()
        {
            return Azimut + ";" + Tilt + ";" + Year;
        }

        [NotNull]
        public string GetKey()
        {
            return Azimut + "###" + Tilt + "###" + Year;
        }
    }
}