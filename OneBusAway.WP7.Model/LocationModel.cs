/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using dev.virtualearth.net.webservices.v1.geocode;
using dev.virtualearth.net.webservices.v1.common;
using OneBusAway.Model.EventArgs;
using OneBusAway.Model.LocationServiceDataStructures;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model
{
    internal class VirtualEarthRequest
    {
        private const string virtualEarthAPIEndpoint = "https://dev.virtualearth.net/REST/v1/";
        public Geopoint UserLocation { get; set; }
        public string SearchString { get; set; }
        public string ApplicationCredential { get; set; }

        /// <summary>
        /// Constructs a VirtualEarthRequest object, which builds an unstructured request.
        /// </summary>
        /// <param name="location">The caller's location, used to improve results.</param>
        /// <param name="search">The unescaped request string, which will be percent encoded for an unstructured search.</param>
        /// <param name="credential">The application's credential for accessing the Bing APIs.</param>

        public Uri GetUri()
        {
            var requestUrl = virtualEarthAPIEndpoint + " Locations/" + WebUtility.UrlEncode(SearchString) + "?" // Use the structured query since it saves a bit of space.
                + "ul=" + string.Format("{0},{1}", UserLocation.Position.Latitude, UserLocation.Position.Longitude) + "&" // Provide the user's location to scope results
                + "o=xml&" // Format the response as XML
                + "key=" + ApplicationCredential;
            return new Uri(requestUrl);
        }
    }

    public class LocationModel : ILocationModel
    {
        #region Private Variables

        private string bingMapsApiKey = "AtAv-npPzjiTyL6ij1J5cgR7Cxmt6h8e3fHlsTSlfWshc8GQ1jfQB1PnB1VfvBGz";

        #endregion

        #region Constructor/Singleton

        public static LocationModel Singleton = new LocationModel();

        #endregion

        #region Public Members

        public async Task<Geopoint> GetLocationForAddressAsync(string addressString, Geopoint searchNearLocation)
        {
            // Build the request URI.
            var request = new VirtualEarthRequest { UserLocation = searchNearLocation,
                SearchString = addressString,
                ApplicationCredential = bingMapsApiKey };

            var client = new HttpClient();
            var response = await client.GetStringAsync(request.GetUri());
            var xmlResponse = XDocument.Parse(response);
            // Inside this response I'm looking for a GeocodePoint whose Usage is Display.
            // Ultimately I'm looking for 
            var displayPoints = from point in xmlResponse.Descendants("GeocodePoint")
                                where (point.Descendants("Usage").First().Value) == "Display"
                                select new Geopoint(new BasicGeoposition { Latitude = double.Parse(point.Descendants("Latitude").First().Value),
                                                                           Longitude = double.Parse(point.Descendants("Longitude").First().Value)});
            return displayPoints.First();
        }

        #endregion
    }
}
