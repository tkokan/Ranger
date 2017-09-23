using static Ranger.Properties.Settings;

namespace Ranger
{
    public class ComputeAreaInputs
    {
        public string OriginName { get; set; }
        public int RangeMins { get; set; }
        public int UnitDistance { get; set; }
        public int SmoothPct { get; set; }
        public GeoLocation[] Border { get; set; }

        public ComputeAreaInputs()
        {
            OriginName = string.Empty;
            RangeMins = Default.RangeMinsDefault;
            UnitDistance = Default.UnitDistanceDefault;
            SmoothPct = Default.SmoothPctDefault;
        }
    }
}