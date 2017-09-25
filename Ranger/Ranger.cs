using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Ranger.Properties.Settings;

namespace Ranger
{
    public class Ranger
    {
        private readonly string apiKey;

        public Ranger()
        {
            var apiKeyPath = Path.Combine(Default.RangerFolder, "apiKey.txt");
            apiKey = File.ReadAllText(apiKeyPath);
        }

        /// <summary>
        /// Creates a dynamic Google map.
        /// </summary>
        public void CreateDynamicMap(MapAreaInputs mapAreaInput, MapInputs mapInputs)
        {
            // build a filename with origin name, range, grid size and smooth percentage
            var fileName = string.Format(
                "{0}-Rng{1}-Unt{2}-Pct{3}.html",
                mapAreaInput.OriginName.Replace(" ", ""),
                mapAreaInput.RangeMins.ToString("000"),
                mapAreaInput.UnitDistance,
                mapAreaInput.SmoothPct.ToString("000"));

            CreateDynamicMap(new List<MapAreaInputs>() { mapAreaInput }, mapInputs, fileName);
        }

        public double CalculateArea(ComputeAreaInputs rangeOptions)
        {
            var range = new RangeGrid(rangeOptions.OriginName, rangeOptions.RangeMins, rangeOptions.UnitDistance);
            range.Process();
            var border = range.GetBorder(rangeOptions.SmoothPct);

            var area = GeometryApi.ComputeArea(border, apiKey);

            using (var dbContext = new RangerDataContext())
            {
                var existingRegion = dbContext
                    .Regions
                    .SingleOrDefault(x => x.OriginId == range.Home.Id && x.RangeMins == rangeOptions.RangeMins && x.UnitDistance == rangeOptions.UnitDistance);

                if (existingRegion == null)
                {
                    var newRegion = new Region()
                    {
                        OriginId = range.Home.Id,
                        RangeMins = rangeOptions.RangeMins,
                        UnitDistance = rangeOptions.UnitDistance,
                        Area = area,
                        BorderNodes = border.Length
                    };

                    dbContext.Regions.InsertOnSubmit(newRegion);
                }
                else
                {
                    existingRegion.Area = area;
                    existingRegion.BorderNodes = border.Length;
                }

                dbContext.SubmitChanges();
            }

            return area;
        }

        public void CreateDynamicMap(IEnumerable<MapAreaInputs> mapAreaInputs, MapInputs mapInputs, string fileName)
        {
            var borders = new List<GeoLocation[]>();

            var minLat = double.MaxValue;
            var maxLat = double.MinValue;
            var minLon = double.MaxValue;
            var maxLon = double.MinValue;

            foreach (var mapAreaInput in mapAreaInputs)
            {
                var range = new RangeGrid(mapAreaInput.OriginName, mapAreaInput.RangeMins, mapAreaInput.UnitDistance);
                range.Process();

                // set border
                mapAreaInput.Border = range.GetBorder(mapAreaInput.SmoothPct);

                if (range.Home.Latitude > maxLat)
                {
                    maxLat = range.Home.Latitude;
                }

                if (range.Home.Latitude < minLat)
                {
                    minLat = range.Home.Latitude;
                }

                if (range.Home.Longitude > maxLon)
                {
                    maxLon = range.Home.Longitude;
                }

                if (range.Home.Longitude < minLon)
                {
                    minLon = range.Home.Longitude;
                }
            }

            var center = new GeoLocation()
            {
                Latitude = (minLat + maxLat) / 2.0,
                Longitude = (minLon + maxLon) / 2.0
            };

            var filePath = Path.Combine(Default.RangerFolder, "Maps", fileName);

            // generate map
            GeometryApi.GenerateDynamicMap(mapAreaInputs, mapInputs, center, apiKey, filePath);

        }
    }
}
