using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer.OSM {
    public class MapColorEntryWithOsmGuid {
        public MapColorEntryWithOsmGuid([NotNull] string osmGuid, [NotNull] RGB desiredColor)
        {
            OsmGuid = osmGuid;
            DesiredColor = desiredColor;
        }

        public MapColorEntryWithOsmGuid([NotNull] string osmGuid, [NotNull] RGB desiredColor, [CanBeNull] string text)
        {
            OsmGuid = osmGuid;
            DesiredColor = desiredColor;
            Text = text;
        }

        [NotNull]
        public RGB DesiredColor { get; set; }

        [NotNull]
        public string OsmGuid { get; set; }
        [CanBeNull]
        public string Text { get; set; }
        public override string ToString() => OsmGuid + " - " + Text + " : " + DesiredColor;
    }
}