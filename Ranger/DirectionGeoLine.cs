namespace Ranger
{
    /// <summary>
    /// Parametric equation of a geo line:
    /// x = x0 + alpha * t
    /// y = y0 + beta * t
    /// </summary>
    public class DirectionGeoLine
    {
        private readonly IGeoLocation point;
        private readonly double multLat;
        private readonly double multLong;

        /// <summary>
        /// Ctor
        /// </summary>
        public DirectionGeoLine(IGeoLocation point, DirectionEnum direction)
        {
            this.point = point;
            multLat = direction.MultY();
            multLong = direction.MultX();
        }

        /// <summary>
        /// Returns the point on the line that is defined by the given parameter t.
        /// </summary>
        public IGeoLocation PointOnLine(double t)
        {
            return new GeoLocation()
            {
                Latitude = point.Latitude + multLat * t,
                Longitude = point.Longitude + multLong * t
            };
        }
    }
}
