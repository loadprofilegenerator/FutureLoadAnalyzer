/*using System;
using JetBrains.Annotations;

namespace Visualizer.Sankey {
    public class ExportEntry {
        // ReSharper disable once NotNullMemberIsNotInitialized
        [Obsolete("for json only")]
        public ExportEntry()
        {
        }

        public ExportEntry([NotNull] string houseGuid, int familySize, double yearlyElectricityUse, [NotNull] string gebäudeObjektIDs)
        {
            HouseGuid = houseGuid;
            FamilySize = familySize;
            YearlyElectricityUse = yearlyElectricityUse;
            GebäudeObjektIDs = gebäudeObjektIDs;
        }

        [NotNull]
        public string HouseGuid { get; set; }

        public int FamilySize { get; set; }
        public double YearlyElectricityUse { get; set; }

        [NotNull]
        public string GebäudeObjektIDs { get; set; }
    }
}*/