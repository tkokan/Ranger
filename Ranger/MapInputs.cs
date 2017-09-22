using static Ranger.Properties.Settings;

namespace Ranger
{
    public class MapInputs
    {
        public int Zoom { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public MapInputs()
        {
            Zoom = Default.MapZoomDefault;
            Width = Default.MapWidthDefault;
            Height = Default.MapHeightDefault;
        }
    }
}