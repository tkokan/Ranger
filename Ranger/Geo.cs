using static System.Math;

namespace Ranger
{
    /// <summary>
    /// Deals with geo-specific calculations like finding aerial distance between two points.
    /// </summary>
    public static class Geo
    {
        /// <summary>
        /// Calculates aerial distance between the two geo locations.
        /// </summary>
        public static double AerialDistance(IGeoLocation point1, IGeoLocation point2)
        {
            if (point1 == null || point2 == null)
            {
                return 0.0;
            }

            var R = 6371E3; // meters

            var lat1 = DegreesToRadians(point1.Latitude);
            var lon1 = DegreesToRadians(point1.Longitude);
            var lat2 = DegreesToRadians(point2.Latitude);
            var lon2 = DegreesToRadians(point2.Longitude);

            var dlat = lat2 - lat1;
            var dlon = lon2 - lon1;

            var a = Sin(dlat / 2) * Sin(dlat / 2) + Cos(lat1) * Cos(lat2) * Sin(dlon / 2) * Sin(dlon / 2);
            var c = 2 * Atan2(Sqrt(a), Sqrt(1 - a));

            return c * R;
        }

        /// <summary>
        /// Converts angle in degrees to radians
        /// </summary>
        private static double DegreesToRadians(double deg)
        {
            return deg * PI / 180.0;
        }

        /// <summary>
        /// Converts angle in radians to degrees
        /// </summary>
        private static double RadiansToDegrees(double rad)
        {
            return rad * 180.0 / PI;
        }
    }
}