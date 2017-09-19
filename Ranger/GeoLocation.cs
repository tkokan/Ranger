namespace Ranger
{
    /// <summary>
    /// Holds latitude and longitude of a point on Earth
    /// </summary>
    public class GeoLocation : IGeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public override string ToString()
        {
            return string.Format("({0:0.00000}, {1:0.00000})", Latitude, Longitude);
        }
    }    
}