namespace Data.DataModel.Dst {
    public class GeoCoord {
        public GeoCoord(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }
        public double Y { get; set; }
    }
}