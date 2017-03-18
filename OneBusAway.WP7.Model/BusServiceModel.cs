﻿/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using System.IO;
using OneBusAway.Model.BusServiceDataStructures;
using System.Diagnostics;
using System.Threading.Tasks;
using OneBusAway.Model.LocationServiceDataStructures;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model
{
    public class BusServiceModel : IBusServiceModel
    {
        private OneBusAwayWebservice webservice;

        #region Constructor/Singleton

        // TODO we really need to get rid of this singleton and move to a better dependency injection model at some point.

        public static BusServiceModel Singleton = new BusServiceModel();

        private BusServiceModel()
        {
        }

        #endregion

        /// <summary>
        /// Scan the list of stops to find all associated routes.
        /// </summary>
        /// <param name="stops"></param>
        /// <param name="location">Center location used to find closestStop on each route.</param>
        /// <returns></returns>
        private List<Route> GetRoutesFromStops(List<Stop> stops, Geopoint location)
        {
            IDictionary<string, Route> routesMap = new Dictionary<string, Route>();
            stops.Sort(new StopDistanceComparer(location));

            foreach (Stop stop in stops)
            {
                foreach (Route route in stop.routes)
                {
                    if (!routesMap.ContainsKey(route.id))
                    {
                        // the stops are sorted in distance order.
                        // so if we haven't already seen this route, then this is the closest stop.
                        route.closestStop = stop;
                        routesMap.Add(route.id, route);
                    }
                }
            }
            return routesMap.Values.ToList<Route>();
        }

        #region Public Methods

        public void Initialize()
        {
            webservice = new OneBusAwayWebservice();
        }

        public double DistanceFromClosestSupportedRegion(Geopoint location)
        {
            return OneBusAwayWebservice.ClosestRegion(location).DistanceFrom(location.Position.Latitude, location.Position.Longitude);
        }

        public bool AreLocationsEquivalent(Geopoint location1, Geopoint location2)
        {
            return OneBusAwayWebservice.GetRoundedLocation(location1) == OneBusAwayWebservice.GetRoundedLocation(location2);
        }

        public Task<Tuple<List<Stop>, List<Route>>> CombinedInfoForLocationAsync(Geopoint location, int radiusInMeters)
        {
            return CombinedInfoForLocationAsync(location, radiusInMeters, -1);
        }

        public Task<Tuple<List<Stop>, List<Route>>> CombinedInfoForLocationAsync(Geopoint location, int radiusInMeters, int maxCount)
        {
            return CombinedInfoForLocationAsync(location, radiusInMeters, maxCount, false);
        }

        public async Task<Tuple<List<Stop>, List<Route>>> CombinedInfoForLocationAsync(Geopoint location, int radiusInMeters, int maxCount, bool invalidateCache)
        {
            var stops = await webservice.StopsForLocationAsync(location,
                null,
                radiusInMeters,
                maxCount,
                invalidateCache);

        }

        public async Task<List<Stop>> StopsForLocationAsync(Geopoint location, int radiusInMeters, int maxCount = -1, bool invalidateCache = false)
        {
            return await webservice.StopsForLocationAsync(location,
                                                          null,
                                                          radiusInMeters,
                                                          maxCount,
                                                          invalidateCache);
        }

        public async Task<List<Route>> RoutesForLocationAsync(Geopoint location, int radiusInMeters, int maxCount = -1, bool invalidateCache = false)
        {
            return await webservice.RoutesForLocationAsync(location,
                                                           null,
                                                           radiusInMeters,
                                                           maxCount);
        }

        public async Task<List<RouteStops>> StopsForRouteAsync(Geopoint location, Route route)
        {
            return webservice.StopsForRouteAsync(location,
                        route);
        }

        public async Task<List<ArrivalAndDeparture>> ArrivalsForStopAsync(Geopoint location, Stop stop)
        {
            return webservice.ArrivalsForStopAsync(location,
                          stop);
        }

        public async Task<List<RouteSchedule>> ScheduleForStopAsync(Geopoint location, Stop stop)
        {
            return webservice.ScheduleForStopAsync(location,
                          stop);
        }

        public async Task<List<TripDetails>> TripDetailsForArrivalsAsync(Geopoint location, List<ArrivalAndDeparture> arrivals)
        {
            int count = 0;
            List<TripDetails> tripDetails = new List<TripDetails>(arrivals.Count);
            Exception overallError = null;

            if (arrivals.Count == 0)
            {
                if (TripDetailsForArrival_Completed != null)
                {
                    TripDetailsForArrival_Completed(
                        this,
                        new ViewModel.EventArgs.TripDetailsForArrivalEventArgs(arrivals, tripDetails, overallError)
                        );
                }
            }
            else
            {
                arrivals.ForEach(arrival =>
                    {
                        webservice.TripDetailsForArrival(
                            location,
                            arrival,
                            delegate (TripDetails tripDetail, Exception error)
                            {
                                if (error != null)
                                {
                                    overallError = error;
                                }
                                else
                                {
                                    tripDetails.Add(tripDetail);
                                }

                                // Is this code thread-safe?
                                count++;
                                if (count == arrivals.Count && TripDetailsForArrival_Completed != null)
                                {
                                    TripDetailsForArrival_Completed(this, new ViewModel.EventArgs.TripDetailsForArrivalEventArgs(arrivals, tripDetails, error));
                                }
                            }
                        );
                    }
                );
            }
        }

        public Task<List<Route>> SearchForRoutesAsync(Geopoint location, string query)
        {
            return SearchForRoutesAsync(location, query, 1000000, -1);
        }

        public Task<List<Route>> SearchForRoutesAsync(Geopoint location, string query, int radiusInMeters, int maxCount)
        {
            return webservice.RoutesForLocation(location,
                        query,
                        radiusInMeters,
                        maxCount);
        }

        public async Task<List<Stop>> SearchForStopsAsync(Geopoint location, string query)
        {
            return await SearchForStopsAsync(location, query, 1000000, -1);
        }

        public async Task<List<Stop>> SearchForStopsAsync(Geopoint location, string query, int radiusInMeters, int maxCount)
        {
            return await webservice.StopsForLocationAsync(location,
                               query,
                               radiusInMeters,
                               maxCount,
                               false);
        }

        public async Task<string> LocationForAddress(string query, Geopoint searchNearLocation)
        {
            string bingMapAPIURL = "http://dev.virtualearth.net/REST/v1/Locations";
            string requestUrl = string.Format(
                "{0}?query={1}&key={2}&o=xml&userLocation={3}",
                bingMapAPIURL,
                query.Replace('&', ' '),
                "AtAv-npPzjiTyL6ij1J5cgR7Cxmt6h8e3fHlsTSlfWshc8GQ1jfQB1PnB1VfvBGz",
                string.Format("{0},{1}", searchNearLocation.Position.Latitude, searchNearLocation.Position.Longitude)
            );

            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(requestUrl);
            return response;
        }

        public delegate void LocationForAddress_Callback(List<LocationForQuery> locations, Exception error);
        private class GetLocationForAddressCompleted
        {
            private LocationForAddress_Callback callback;
            private string requestUrl;

            public GetLocationForAddressCompleted(string requestUrl, LocationForAddress_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
            }

            public void LocationForAddress_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<LocationForQuery> locations = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        XNamespace ns = "http://schemas.microsoft.com/search/local/ws/rest/v1";

                        locations = (from location in xmlDoc.Descendants(ns + "Location")
                                     select new LocationForQuery
                                     {
                                         location = new Geopoint(
                                             Convert.ToDouble(location.Element(ns + "Point").Element(ns + "Latitude").Value),
                                             Convert.ToDouble(location.Element(ns + "Point").Element(ns + "Longitude").Value)
                                             ),
                                         name = location.Element(ns + "Name").Value,
                                         confidence = (Confidence)Enum.Parse(
                                              typeof(Confidence),
                                              location.Element(ns + "Confidence").Value,
                                              true
                                              ),
                                         boundingBox = new LocationRect(
                                              Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "NorthLatitude").Value),
                                              Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "WestLongitude").Value),
                                              Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "SouthLatitude").Value),
                                              Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "EastLongitude").Value)
                                              )
                                     }).ToList();

                    }
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(locations, error);
            }
        }

        #endregion

        public void ClearCache()
        {
            if (webservice != null)
            {
                webservice.ClearCache();
            }
        }

        public void SaveCache()
        {
            if (webservice != null)
            {
                webservice.SaveCache();
            }
        }
    }
}
