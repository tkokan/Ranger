using System;
using System.Collections.Generic;
using System.Linq;

namespace Ranger
{
    class Program
    {
        static void Main(string[] args)
        {
            var ranger = new Ranger();

            var fillOpacity = 0.4;

            var mapAreaInputs = new List<MapAreaInputs>()
            {
                new MapAreaInputs()
                {
                    OriginName = "London, UK",
                    Color = "#872D7D",
                    FillOpacity = fillOpacity
                },

                new MapAreaInputs()
                {
                    OriginName = "Paris, France",
                    Color = "#FF8F00",
                    FillOpacity = fillOpacity
                },

                new MapAreaInputs()
                {
                    OriginName = "Rome, Italy",
                    Color = "#C22326",
                    FillOpacity = fillOpacity
                },

                new MapAreaInputs()
                {
                    OriginName = "Berlin, Germany",
                    Color = "#3C7DC4",
                    FillOpacity = fillOpacity
                },

                new MapAreaInputs()
                {
                    OriginName = "Madrid, Spain",
                    Color = "#5A8F29",
                    FillOpacity = fillOpacity
                }
            };

            var mapInputs = new MapInputs()
            {
                Zoom = 5,
                Width = 800,
                Height = 700
            };

            ranger.CreateDynamicMap(mapAreaInputs, mapInputs, "FiveCities.html");

            var area = ranger.CalculateArea(mapAreaInputs.First());

            Console.WriteLine("Area = {0:0.00} sq km", area);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
