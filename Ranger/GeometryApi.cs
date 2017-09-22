using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Remote;
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
    public class GeometryApi
    {
        private readonly string apiKey;

        /// <summary>
        /// Ctor
        /// </summary>
        public GeometryApi(string apiKeyFilePath)
        {
            apiKey = File.ReadAllText(apiKeyFilePath);
        }

        /// <summary>
        /// Generate dynamic map with a border determined by nodes.
        /// </summary>
        public void GenerateDynamicMap(IEnumerable<IGeoLocation> nodes, IGeoLocation center, string filePath)
        {
            // create folder if it doesn't exist
            new FileInfo(filePath).Directory.Create();

            var template = File.ReadAllText("DynamicMapTemplate.html");
            var html = new StringBuilder(template);

            html.Replace("/*key*/", apiKey);
            html.Replace("/*center*/", $"{center.Latitude}, {center.Longitude}");

            var nodesStrings = GetNodesStrings(nodes, 16);

            html.Replace("/*nodes*/", string.Join("," + Environment.NewLine, nodesStrings));

            File.WriteAllText(filePath, html.ToString());
        }

        /// <summary>
        /// Compute the area (in square km) of the region with a border determined by nodes.
        /// </summary>
        public double ComputeArea(IEnumerable<IGeoLocation> nodes, string driverFolderPath)
        {
            var template = File.ReadAllText("ComputeAreaTemplate.html");
            var html = new StringBuilder(template);

            html.Replace("/*key*/", apiKey);

            var nodesStrings = GetNodesStrings(nodes, 16);

            html.Replace("/*nodes*/", string.Join("," + Environment.NewLine, nodesStrings));

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

        private IEnumerable<string> GetNodesStrings(IEnumerable<IGeoLocation> nodes, int indentation = 0)
        {
            var nodesStrings = new List<string>();
            var prefix = new string(' ', indentation);

            return nodes.Select(x => $"{prefix}new google.maps.LatLng({x.Latitude}, {x.Longitude})");
        }
    }
}
