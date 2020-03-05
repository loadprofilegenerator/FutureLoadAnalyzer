using Common.Steps;
using JetBrains.Annotations;

namespace Data.DataModel {
    public class MapColorEntryWithHouseGuid {
        public MapColorEntryWithHouseGuid([NotNull] string houseGuid, [NotNull] RGB desiredColor)
        {
            HouseGuid = houseGuid;
            DesiredColor = desiredColor;
        }

        [NotNull]
        public override string ToString() => HouseGuid + " - " + Text + " : " + DesiredColor;

        public MapColorEntryWithHouseGuid([NotNull] string houseGuid, [NotNull] RGB desiredColor, [NotNull] string text)
        {
            HouseGuid = houseGuid;
            DesiredColor = desiredColor;
            Text = text;
        }

        [NotNull]
        public RGB DesiredColor { get; set; }

        [NotNull]
        public string HouseGuid { get; set; }
        [CanBeNull]
        public string Text { get; set; }
    }
}