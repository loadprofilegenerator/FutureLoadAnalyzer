using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer
{
#pragma warning disable CA1724 // Type names should not match namespaces
    public static class Extensions
#pragma warning restore CA1724 // Type names should not match namespaces
    {
        [NotNull]
        public static Mapsui.Styles.Color GetMapSuiColor([NotNull] this RGB rgb) => Mapsui.Styles.Color.FromArgb(255, rgb.R, rgb.G, rgb.B);
        //public Mapsui.Styles.Color GetMapSuiColor() => Mapsui.Styles.Color.FromArgb(255, R, G, B);

    }
}
