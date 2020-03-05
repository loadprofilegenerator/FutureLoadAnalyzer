using System;
using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer {
    public static class ColorGenerator {
        public static int GetElement(int index)
        {
            var value = index - 1;
            var v = 0;
            for (var i = 0; i < 8; i++) {
                v |= value & 1;
                v <<= 1;
                value >>= 1;
            }

            v >>= 1;
            return v & 0xFF;
        }

        [NotNull]
        public static int[] GetPattern(int index)
        {
            var n = (int)Math.Pow(index, 1.0 / 3.0);
            index -= n * n * n;
            var p = new int[3];
            for (var i = 0; i < p.Length; i++) {
                p[i] = n;
            }

            if (index == 0) {
                return p;
            }

            index--;
            var v = index % 3;
            index /= 3;
            if (index < n) {
                p[v] = index % n;
                return p;
            }

            index -= n;
            p[v] = index / n;
            p[++v % 3] = index % n;
            return p;
        }
        /*
        public Color getColor(int i)
        {
            return new Color(getRGB(i));
        }*/

        [NotNull]
        public static RGB GetRGB(int index)
        {
            var p = GetPattern(index);
            return new RGB(GetElement(p[0]), GetElement(p[1]), GetElement(p[2]));
        }
    }
}