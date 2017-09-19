using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ranger
{
    /// <summary>
    /// Communication with Google's Geometry API
    /// </summary>
    public class GeometryApi
    {
        private readonly string apiKey;
        private readonly string template;

        /// <summary>
        /// Ctor
        /// </summary>
        public GeometryApi(string apiKeyFilePath)
        {
            apiKey = File.ReadAllText(apiKeyFilePath);
            template = File.ReadAllText("DynamicMapTemplate.html");
        }

        /// <summary>
        /// Generate dynamic map with a border determined with nodes.
        /// </summary>
        public void GenerateDynamicMap(IEnumerable<IGeoLocation> nodes, IGeoLocation center, string filePath)
        {
            // create folder if it doesn't exist
            new FileInfo(filePath).Directory.Create();

            var html = new StringBuilder(template);

            html.Replace("{key}", apiKey);
            html.Replace("{center}", $"{center.Latitude}, {center.Longitude}");

            var nodesStrings = new List<string>();

            foreach(var node in nodes)
            {
                nodesStrings.Add($"new google.maps.LatLng({node.Latitude}, {node.Longitude})");
            }

            html.Replace("{nodes}", string.Join("," + Environment.NewLine, nodesStrings));

            File.WriteAllText(filePath, html.ToString());
        }
    }
}
