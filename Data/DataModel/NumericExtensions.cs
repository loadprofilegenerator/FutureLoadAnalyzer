using System;

namespace Data.DataModel {
    public static class NumericExtensions {
        public static double ToRadians(this double val) => Math.PI / 180 * val;
    }
}