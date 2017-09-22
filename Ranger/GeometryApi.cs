using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ranger
{
    /// <summary>
    /// Communication with Google's Geometry API
    /// </summary>
    public static class GeometryApi
    {
        /// <summary>
        /// Generate dynamic map with a border determined by nodes.
        /// </summary>
        public static void GenerateDynamicMap(IEnumerable<MapAreaInputs> inputs, MapInputs mapInputs, IGeoLocation center, string apiKey, string filePath)
        {
            // create folder if it doesn't exist
            new FileInfo(filePath).Directory.Create();

            var mapTemplate = File.ReadAllText("DynamicMapTemplate.html");
            var polygonTemplate = File.ReadAllText("PolygonTemplate.html");

            var html = new StringBuilder(mapTemplate);

            html.Replace("/*key*/", apiKey);
            html.Replace("/*center*/", $"{center.Latitude}, {center.Longitude}");
            html.Replace("/*zoom*/", mapInputs.Zoom.ToString());
            html.Replace("/*width*/", mapInputs.Width.ToString());
            html.Replace("/*height*/", mapInputs.Height.ToString());

            var polygons = new List<string>();

            var num = 0;

            foreach (var input in inputs)
            {
                var polygon = new StringBuilder(polygonTemplate);
                var nodesStrings = GetNodesStrings(input.Border, 16);

                polygon.Replace("/*num*/", num.ToString());
                polygon.Replace("/*color*/", $"\"{input.Color}\"");
                polygon.Replace("/*strokeOpacity*/", input.StrokeOpacity.ToString("0.00"));
                polygon.Replace("/*strokeWeight*/", input.StrokeWeight.ToString());
                polygon.Replace("/*fillOpacity*/", input.FillOpacity.ToString("0.00"));
                polygon.Replace("/*nodes*/", string.Join($",{Environment.NewLine}", nodesStrings));
                polygons.Add(polygon.ToString());

                num++;
            }

            html.Replace("/*polygons*/", string.Join(Environment.NewLine, polygons));

            File.WriteAllText(filePath, html.ToString());
        }

        /// <summary>
        /// Compute the area (in square km) of the region with a border determined by nodes.
        /// </summary>
        public static double ComputeArea(IEnumerable<IGeoLocation> nodes, string apiKey, string driverFolderPath)
        {
            var template = File.ReadAllText("ComputeAreaTemplate.html");
            var html = new StringBuilder(template);

            html.Replace("/*key*/", apiKey);

            var nodesStrings = GetNodesStrings(nodes, 16);

            html.Replace("/*nodes*/", string.Join($",{Environment.NewLine}", nodesStrings));

            var path = Path.Combine(driverFolderPath, "ComputeArea.html");

            File.WriteAllText(path, html.ToString());

            var driver = new PhantomJSDriver(driverFolderPath);
            var filePath = Path.Combine("file:///", path);
            var url = new Uri(path);

            driver.Navigate().GoToUrl(url);

            var element = driver.FindElement(By.Id("area"));
            var area = double.Parse(element.Text);

            driver.Quit();
            File.Delete(path);

            return area / 1E6;
        }

        private static IEnumerable<string> GetNodesStrings(IEnumerable<IGeoLocation> nodes, int indentation = 0)
        {
            var nodesStrings = new List<string>();
            var prefix = new string(' ', indentation);

            return nodes.Select(x => $"{prefix}new google.maps.LatLng({x.Latitude}, {x.Longitude})");
        }
    }
}
