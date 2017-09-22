using static Ranger.Properties.Settings;

namespace Ranger
{
    public class MapAreaInputs : ComputeAreaInputs
    {
        public string Color { get; set; }
        public double StrokeOpacity { get; set; }
        public int StrokeWeight { get; set; }
        public double FillOpacity { get; set; }

        public MapAreaInputs() : base()
        {
            Color = Default.ColorDefault;
            StrokeOpacity = Default.StrokeOpacityDefault;
            StrokeWeight = Default.StrokeWeightDefault;
            FillOpacity = Default.FillOpacityDefault;
        }
    }
}
