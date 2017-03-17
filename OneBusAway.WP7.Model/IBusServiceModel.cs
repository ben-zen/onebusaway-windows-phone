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
using System;
using System.Net;
using System.Threading.Tasks;
using OneBusAway.ViewModel.BusServiceDataStructures;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model
{
  public interface IBusServiceModel
  {
    double DistanceFromClosestSupportedRegion(Geopoint location);
    bool AreLocationsEquivalent(Geopoint location1, Geopoint location2);
    Task<Tuple<List<Stop>,List<Route>>> CombinedInfoForLocationAsync(Geopoint location, int radiusInMeters);
    Task<Tuple<List<Stop>, List<Route>>> CombinedInfoForLocationAsync(Geopoint location, int radiusInMeters, int maxCount);
    Task<Tuple<List<Stop>, List<Route>>> CombinedInfoForLocationAsync(Geopoint location, int radiusInMeters, int maxCount, bool invalidateCache);
    Task<List<Stop>> StopsForLocationAsync(Geopoint location, int radiusInMeters);
    Task<List<Stop>> StopsForLocationAsync(Geopoint location, int radiusInMeters, int maxCount);
    Task<List<Stop>> StopsForLocationAsync(Geopoint location, int radiusInMeters, int maxCount, bool invalidateCache);
    Task<List<Route>> RoutesForLocationAsync(Geopoint location, int radiusInMeters);
    Task<List<Route>> RoutesForLocationAsync(Geopoint location, int radiusInMeters, int maxCount);
    Task<List<Route>> RoutesForLocationAsync(Geopoint location, int radiusInMeters, int maxCount, bool invalidateCache);
    Task<List<RouteStops>> StopsForRouteAsync(Geopoint location, Route route);
    Task<List<ArrivalAndDeparture>> ArrivalsForStopAsync(Geopoint location, Stop stop);
    Task<List<RouteSchedule>> ScheduleForStopAsync(Geopoint location, Stop stop);
    Task<List<TripDetails>> TripDetailsForArrivalsAsync(Geopoint location, List<ArrivalAndDeparture> arrivals);
    Task<List<Route>> SearchForRoutesAsync(Geopoint location, string query);
    Task<List<Route>> SearchForRoutesAsync(Geopoint location, string query, int radiusInMeters, int maxCount);
    Task<List<Stop>> SearchForStopsAsync(Geopoint location, string query);

    void Initialize();
    void ClearCache();
  }

  public class WebserviceParsingException : Exception
  {
    public string RequestUrl { get; private set; }
    public string ServerResponse { get; private set; }

    public WebserviceParsingException(string requestUrl, string serverResponse, Exception innerException)
        : base("There was an error parsing the server response", innerException)
    {
      this.RequestUrl = requestUrl;
      this.ServerResponse = serverResponse;
    }

    public override string ToString()
    {
      return string.Format(
          "{0}\r\nRequestURL: '{1}'\r\nResponse:\r\n{2}",
          base.ToString(),
          RequestUrl,
          ServerResponse
          );
    }
  }

  public class WebserviceResponseException : Exception
  {
    public string RequestUrl { get; private set; }
    public string ServerResponse { get; private set; }
    public HttpStatusCode ServerStatusCode { get; private set; }

    public WebserviceResponseException(HttpStatusCode serverStatusCode, string requestUrl, string serverResponse, Exception innerException)
        : base("We were able to contact the webservice but the service returned an error", innerException)
    {
      this.RequestUrl = requestUrl;
      this.ServerResponse = serverResponse;
      this.ServerStatusCode = serverStatusCode;
    }

    public override string ToString()
    {
      return string.Format(
          "{0}\r\nHttpErrorCode: '{1}'\r\nRequestURL: '{2}'\r\nResponse:\r\n{3}",
          base.ToString(),
          ServerStatusCode,
          RequestUrl,
          ServerResponse
          );
    }
  }
}
