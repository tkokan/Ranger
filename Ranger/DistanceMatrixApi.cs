using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Ranger
{
    /// <summary>
    /// Communication with Google's DistanceMatrix API
    /// </summary>
    public class DistanceMatrixApi
    {
        private const string AddressFormat = "https://maps.googleapis.com/maps/api/distancematrix/json?origins={0},{1}&destinations={2},{3}&key={4}";

        private readonly string apiKey;

        /// <summary>
        /// Ctor
        /// </summary>
        public DistanceMatrixApi(string apiKeyFilePath)
        {
            apiKey = File.ReadAllText(apiKeyFilePath);
        }

        /// <summary>
        /// Returns the distance between origin and destination.
        /// Returns -1 if the distance cannot be determined.
        /// </summary>
        public int Query(IGeoLocation origin, IGeoLocation destination)
        {
            var address = string.Format(AddressFormat, origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude, apiKey);
            var resultJson = new WebClient().DownloadString(address);
            var result = JsonConvert.DeserializeObject<Result>(resultJson);

            if(result.status == StatusEnum.OVER_QUERY_LIMIT)
            {
                Debug.WriteLine(result.status);
                return -1;
            }

            var element = result.rows[0].elements[0];

            if (element.status == StatusEnum.OK)
            {
                return element.duration.value;
            }
            else if(element.status == StatusEnum.ZERO_RESULTS)
            {
                return int.MaxValue;
            }
            else
            {
                return -1;
            }
        }
    }
}
