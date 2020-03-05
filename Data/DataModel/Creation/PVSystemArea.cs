using System;

namespace Data.DataModel.Creation {
    public class PVSystemArea {
        [Obsolete("Only for json")]
        public PVSystemArea()
        {
        }

        public PVSystemArea(double azimut, double tilt, double energy)
        {
            Azimut = azimut;
            Tilt = tilt;
            Energy = energy;
        }

        public double Azimut { get; set; }
        public double Tilt { get; set; }
        public double Energy { get; set; }
    }
}