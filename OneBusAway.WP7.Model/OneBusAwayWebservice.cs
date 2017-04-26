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
using OneBusAway.Model.BusServiceDataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Storage;

namespace OneBusAway.Model
{
  internal class OneBusAwayWebservice
  {

    #region Private Variables

    private const string REGIONS_XML_FILE = "Regions.xml";
    private static readonly object regionsLock = new object();
    private static List<Region> discoveredRegions;

    /// <summary>
    /// This is the URL of the regions web service.
    /// </summary>
    private const string REGIONS_SERVICE_URI = "http://regions.onebusaway.org/regions.xml";

    private const string KEY = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
    private const int APIVERSION = 2;

    // This decides which decimal place we round
    // Ex: roundingLevel = 2, -122.123 -> -122.12
    private const int roundingLevel = 2;
    // This decides what fraction of a whole number we round to
    // Ex: multiplier = 2, we round to the nearest 0.5
    // Ex: multipler = 3, we round to the nearest 0.33
    private const int multiplier = 3;

    private HttpClient client;

    #endregion

    #region Constructor

    public OneBusAwayWebservice()
    {
      client = new HttpClient();
    }

    #endregion

    #region OneBusAway service calls

    /// <summary>
    /// Base class for callbacks on service call completion.
    /// </summary>
    private abstract class ACallCompleted
    {
      protected string requestUrl;

      public ACallCompleted(string requestUrl)
      {
        this.requestUrl = requestUrl;
      }

      /// <summary>
      /// Callback entry point for calls based on HttpWebRequest
      /// </summary>
      /// <param name="asyncResult"></param>
      public void HttpWebRequest_Completed(IAsyncResult asyncResult)
      {
        try
        {
          HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
          HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);

          if (response.StatusCode != HttpStatusCode.OK)
          {
            // Reading the server response will probably fail since the request was unsuccessful
            // so just return null as the server response. Also, from my personal testing this
            // code is unreachable: if the server returns a 404 request.EndGetResponse() will
            // throw an exception.
            throw new WebserviceResponseException(response.StatusCode, request.RequestUri.ToString(), null, null);
          }
          else
          {
            XDocument xmlResponse = CheckResponseCode(new StreamReader(response.GetResponseStream()), request.RequestUri.ToString());
            ParseResults(xmlResponse, null);
          }
        }
        catch (Exception e)
        {
          ParseResults(null, e);
        }
      }

      private static XDocument CheckResponseCode(TextReader xmlResponse, string requestUrl)
      {
        XDocument xmlDoc = null;
        HttpStatusCode code = HttpStatusCode.Unused;

        try
        {
          xmlDoc = XDocument.Load(xmlResponse);
          code = (HttpStatusCode)int.Parse(xmlDoc.Element("response").Element("code").Value);
        }
        catch (Exception e)
        {
          // Any exception thrown in this code means either A) the server response wasn't XML so XDocument.Load() failed or
          // B) the code element doesn't exist so the server response is invalid. The known cause for these things to
          // fail (besides a server malfunction) is the phone being connected to a WIFI access point which requires
          // a login page, so we get the hotspot login page back instead of our web request.
          Debug.Assert(false);

          throw new WebserviceResponseException(HttpStatusCode.Unused, requestUrl, xmlResponse.ReadToEnd(), e);
        }

        if (code != HttpStatusCode.OK)
        {
          Debug.Assert(false);

          throw new WebserviceResponseException(code, requestUrl, xmlDoc.ToString(), null);
        }

        return xmlDoc;
      }

      /// <summary>
      /// Subclasses should implement this by pulling data out of the specified reader, parsing it, and invoking the desired callback.
      /// </summary>
      /// <param name="result"></param>
      /// <param name="error"></param>
      public abstract void ParseResults(XDocument result, Exception error);
    }

    public async Task<List<Route>> RoutesForLocationAsync(Geopoint location, string query, int radiusInMeters, int maxCount)
    {
      var region = await ClosestRegionAsync(location);
      string requestUrl = string.Format(
          "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&Version={6}",
          region.RegionUrl,
          "routes-for-location",
          KEY,
          location.Position.Latitude.ToString(NumberFormatInfo.InvariantInfo),
          location.Position.Longitude.ToString(NumberFormatInfo.InvariantInfo),
          radiusInMeters,
          APIVERSION
          );

      if (string.IsNullOrEmpty(query) == false)
      {
        requestUrl += string.Format("&query={0}", query);
      }

      if (maxCount > 0)
      {
        requestUrl += string.Format("&maxCount={0}", maxCount);
      }

      var response = await client.GetStringAsync(requestUrl);
      var routes = new List<Route>();
      try
      {
        var xmlResponse = XDocument.Parse(response);
        routes.AddRange(from route in xmlResponse.Descendants("route")
                        select ParseRoute(route, xmlResponse.Descendants("agency"), region));
      }
      catch (Exception ex)
      {
        throw new WebserviceParsingException(requestUrl, response, ex);
      }
      return routes;
    }

    public async Task<List<Stop>> StopsForLocationAsync(Geopoint location, string query, int radiusInMeters, int maxCount, bool invalidateCache)
    {
      Geopoint roundedLocation = GetRoundedLocation(location);

      // ditto for the search radius -- nearest 50 meters for caching
      int roundedRadius = (int)(Math.Round(radiusInMeters / 50.0) * 50);
      var region = await ClosestRegionAsync(location);
      string requestString = string.Format(
          "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&Version={6}",
          region.RegionUrl,
          "stops-for-location",
          KEY,
          roundedLocation.Position.Latitude.ToString(NumberFormatInfo.InvariantInfo),
          roundedLocation.Position.Longitude.ToString(NumberFormatInfo.InvariantInfo),
          roundedRadius,
          APIVERSION
          );

      if (!string.IsNullOrEmpty(query))
      {
        requestString += string.Format("&query={0}", query);
      }

      if (maxCount > 0)
      {
        requestString += string.Format("&maxCount={0}", maxCount);
      }

      Uri requestUri = new Uri(requestString);

      var response = await client.GetStringAsync(requestString);

      var xResponse = XDocument.Parse(response);
      IEnumerable<XElement> descendants = xResponse.Descendants("data");
      bool limitExceeded = false;
      if (descendants.Count() != 0)
      {
        limitExceeded = bool.Parse(SafeGetValue(descendants.First().Element("limitExceeded")));
      }

      var stops = new List<Stop>();
      try
      {

        IDictionary<string, Route> routesMap = ParseAllRoutes(xResponse, region);

        stops.AddRange(from stop in xResponse.Descendants("stop")
                       select ParseStop(
                       stop,
                       (from routeId in stop.Element("routeIds").Descendants("string")
                        select routesMap[SafeGetValue(routeId)]).ToList<Route>()));

      }
      catch (Exception ex)
      {
        throw ex;
      }
      return stops;
    }

    public async Task<List<RouteStops>> StopsForRouteAsync(Geopoint location, Route route)
    {
      var region = await ClosestRegionAsync(location);
      string requestUrl = string.Format(
          "{0}/{1}/{2}.xml?key={3}&Version={4}",
          region.RegionUrl,
          "stops-for-route",
          route.Id,
          KEY,
          APIVERSION
          );
      var response = await client.GetStringAsync(requestUrl);
      var routeStops = new List<RouteStops>();
      var xResponse = XDocument.Parse(response);
      try
      {
        var routesMap = ParseAllRoutes(xResponse, region);

        // parse all the stops, using previously parsed Route objects
        var stops =
            (from stop in xResponse.Descendants("stop")
             select ParseStop(stop,
                 (from routeId in stop.Element("routeIds").Descendants("string")
                  select routesMap[SafeGetValue(routeId)]
                      ).ToList<Route>()
             )).ToList<Stop>();

        var stopsMap = new Dictionary<string, Stop>();
        foreach (Stop s in stops)
        {
          stopsMap.Add(s.Id, s);
        }

        // and put it all together
        routeStops.AddRange
            (from stopGroup in xResponse.Descendants("stopGroup")
             where SafeGetValue(stopGroup.Element("name").Element("type")) == "destination"
             select new RouteStops
             {
               Name = SafeGetValue(stopGroup.Descendants("names").First().Element("string")),
               EncodedPolylines = (from poly in stopGroup.Descendants("encodedPolyline")
                                   select new PolyLine
                                   {
                                     PointsString = SafeGetValue(poly.Element("points")),
                                     Length = SafeGetValue(poly.Element("length"))
                                   }).ToList<PolyLine>(),
               Stops =
                     (from stopId in stopGroup.Descendants("stopIds").First().Descendants("string")
                      select stopsMap[SafeGetValue(stopId)]).ToList<Stop>(),

               Route = routesMap[route.Id]

             });
      }
      catch (WebserviceResponseException ex)
      {
        throw ex;
      }
      catch (Exception ex)
      {
        throw new WebserviceParsingException(requestUrl, xResponse.ToString(), ex);
      }
      return routeStops;
    }

    public async Task<List<ArrivalAndDeparture>> GetArrivalsForStopAsync(Geopoint location, Stop stop)
    {
      string requestUrl = string.Format(
          "{0}/{1}/{2}.xml?minutesAfter={3}&key={4}&Version={5}",
          await WebServiceUrlForLocationAsync(location),
          "arrivals-and-departures-for-stop",
          stop.Id,
          60,
          KEY,
          APIVERSION
          );

      var response = await client.GetStringAsync(requestUrl);
      var arrivals = new List<ArrivalAndDeparture>();
      try
      {
        var xmlResponse = XDocument.Parse(response);
        arrivals.AddRange(from arrival in xmlResponse.Descendants("arrivalAndDeparture")
                          select ParseArrivalAndDeparture(arrival));
      }
      catch (Exception ex)
      {
        throw new WebserviceParsingException(requestUrl, response, ex);
      }
      return arrivals;
    }

    public async Task<List<RouteSchedule>> GetScheduleForStopAsync(Geopoint location, Stop stop)
    {
      var region = await ClosestRegionAsync(location);
      string requestUrl = string.Format(
          "{0}/{1}/{2}.xml?key={3}&Version={4}",
          region.RegionUrl,
          "schedule-for-stop",
          stop.Id,
          KEY,
          APIVERSION
          );

      var response = await client.GetStringAsync(requestUrl);
      var schedules = new List<RouteSchedule>();
      try
      {
        var xmlResponse = XDocument.Parse(response);
        var routes = ParseAllRoutes(xmlResponse, region);
        schedules.AddRange(from schedule in xmlResponse.Descendants("stopRouteSchedule")
                           select new RouteSchedule
                           {
                             Route = routes[schedule.Element("routeId").Value],
                             Directions = (from direction in schedule.Descendants("stopRouteDirectionSchedule")
                                           select new DirectionSchedule
                                           {
                                             TripHeadsign = direction.Element("tripHeadsign").Value,
                                             Trips = (from trip in direction.Descendants("scheduleStopTime")
                                                      select ParseScheduleStopTime(trip)).ToList()
                                           }).ToList()
                           });
      }
      catch (Exception ex)
      {
        throw new WebserviceParsingException(requestUrl, response, ex);
      }
      return schedules;
    }

    public async Task<TripDetails> TripDetailsForArrivalAsync(Geopoint location, ArrivalAndDeparture arrival)
    {
      string requestUrl = string.Format(
          "{0}/{1}/{2}.xml?key={3}&includeSchedule={4}",
          await WebServiceUrlForLocationAsync(location),
          "trip-details",
          arrival.TripId,
          KEY,
          "false"
          );

      var response = await client.GetStringAsync(requestUrl);
      var xmlResponse = XDocument.Parse(response);
      var detail = (from trip in xmlResponse.Descendants("entry")
                    select ParseTripDetails(trip)).First();
      return detail;
    }

    private class TripDetailsForArrivalCompleted
    {
      public void ParseResults(string requestUrl, XDocument xmlDoc, Exception error)
      {
        TripDetails tripDetail = new TripDetails();

        if (xmlDoc == null || error != null)
        {

        }
        else
        {
          try
          {

            tripDetail =
                (from trip in xmlDoc.Descendants("entry")
                 select ParseTripDetails(trip)).First();
          }
          catch (WebserviceResponseException ex)
          {
            error = ex;
          }
          catch (Exception ex)
          {
            error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
          }
        }

        Debug.Assert(error == null);
      }
    }

    #endregion

    #region Structure parsing code

    private static TripDetails ParseTripDetails(XElement trip)
    {
      TripDetails tripDetails = new TripDetails();

      tripDetails.TripId = SafeGetValue(trip.Element("tripId"));

      XElement statusElement;
      if (trip.Element("tripStatus") != null)
      {
        // ArrivalsForStop returns the status element as 'tripStatus'
        statusElement = trip.Element("tripStatus");
      }
      else if (trip.Element("status") != null)
      {
        // The TripDetails query returns 'status'
        statusElement = trip.Element("status");
      }
      else
      {
        // No status available, stop parsing here
        return tripDetails;
      }

      // TODO: Log a warning for when the serviceDate is invalid. This might be a OBA bug, but I don't
      // have the debugging info to prove it
      string serviceDate = SafeGetValue(statusElement.Element("serviceDate"));
      if (string.IsNullOrEmpty(serviceDate) == false)
      {
        long serviceDateLong;
        bool success = long.TryParse(serviceDate, out serviceDateLong);
        if (success)
        {
          tripDetails.ServiceDate = UnixTimeToDateTime(serviceDateLong);
          if (string.IsNullOrEmpty(SafeGetValue(statusElement.Element("predicted"))) == false
              && bool.Parse(SafeGetValue(statusElement.Element("predicted"))) == true)
          {
            tripDetails.ScheduleDeviationInSec = int.Parse(SafeGetValue(statusElement.Element("scheduleDeviation")));
            tripDetails.ClosestStopId = SafeGetValue(statusElement.Element("closestStop"));
            tripDetails.ClosestStopTimeOffset = int.Parse(SafeGetValue(statusElement.Element("closestStopTimeOffset")));

            if (statusElement.Element("position") != null)
            {
              tripDetails.Location = new Geopoint(new BasicGeoposition
              {
                Latitude = double.Parse(SafeGetValue(statusElement.Element("position").Element("lat")), NumberFormatInfo.InvariantInfo),
                Longitude = double.Parse(SafeGetValue(statusElement.Element("position").Element("lon")), NumberFormatInfo.InvariantInfo)
              });
            }
          }
        }
      }

      return tripDetails;
    }

    private static ArrivalAndDeparture ParseArrivalAndDeparture(XElement arrival)
    {
      return new ArrivalAndDeparture
      {
        RouteId = SafeGetValue(arrival.Element("routeId")),
        TripId = SafeGetValue(arrival.Element("tripId")),
        StopId = SafeGetValue(arrival.Element("stopId")),
        RouteShortName = SafeGetValue(arrival.Element("routeShortName")),
        TripHeadsign = SafeGetValue(arrival.Element("tripHeadsign")),
        PredictedArrivalTime = arrival.Element("predictedArrivalTime").Value == "0" ?
          null : (DateTime?)UnixTimeToDateTime(long.Parse(arrival.Element("predictedArrivalTime").Value)),
        ScheduledArrivalTime = UnixTimeToDateTime(long.Parse(arrival.Element("scheduledArrivalTime").Value)),
        PredictedDepartureTime = arrival.Element("predictedDepartureTime").Value == "0" ?
          null : (DateTime?)UnixTimeToDateTime(long.Parse(arrival.Element("predictedDepartureTime").Value)),
        ScheduledDepartureTime = UnixTimeToDateTime(long.Parse(arrival.Element("scheduledDepartureTime").Value)),
        Status = SafeGetValue(arrival.Element("status")),
        TripDetails = ParseTripDetails(arrival)
      };
    }



    private static Route ParseRoute(XElement route, IEnumerable<XElement> agencies, Region region)
    {
      var agency = Agency.GetAgencyForXML((from xmlAgency in agencies
                                           where route.Element("agencyId").Value == xmlAgency.Element("id").Value
                                           select xmlAgency).First(), region);
      return Route.GetRouteForXML(route, agency);
    }

    /// <summary>
    /// Parses all the routes in the document.
    /// </summary>
    /// <param name="xmlDoc"></param>
    /// <returns>A map of route id to route object</returns>
    private static IDictionary<string, Route> ParseAllRoutes(XDocument xmlDoc, Region region)
    {
      IList<Route> routes =
          (from route in xmlDoc.Descendants("route")
           select ParseRoute(route, xmlDoc.Descendants("agency"), region)).ToList<Route>();
      IDictionary<string, Route> routesMap = new Dictionary<string, Route>();
      foreach (Route r in routes)
      {
        routesMap.Add(r.Id, r);
      }
      return routesMap;
    }

    private static ScheduleStopTime ParseScheduleStopTime(XElement trip)
    {
      return new ScheduleStopTime()
      {
        ArrivalTime = UnixTimeToDateTime(long.Parse(SafeGetValue(trip.Element("arrivalTime"), "0"))),
        DepartureTime = UnixTimeToDateTime(long.Parse(SafeGetValue(trip.Element("departureTime"), "0"))),
        ServiceId = SafeGetValue(trip.Element("serviceId")),
        TripId = SafeGetValue(trip.Element("tripId"))
      };
    }

    private static StopClassification ParseStopClassification(string classification)
    {
      var value = StopClassification.Stop;
      if (classification != null && classification != String.Empty)
      {
        switch (classification)
        {
          case "0":
            value = StopClassification.Stop;
            break;
          case "1":
            value = StopClassification.Station;
            break;
        }
      }
      return value;
    }

    private static WheelchairAccessibility ParseWheelchairAccessibility(string accessibility)
    {
      var value = WheelchairAccessibility.Unknown;
      if (accessibility != null && accessibility != String.Empty)
      {
        switch (accessibility.ToLowerInvariant())
        {
          case "accessible":
            value = WheelchairAccessibility.Available;
            break;
          case "not_accessible":
            value = WheelchairAccessibility.Unavailable;
            break;
        }
      }
      return value;
    }

    private static Stop ParseStop(XElement stop, List<Route> routes)
    {
      return new Stop
      {
        Id = SafeGetValue(stop.Element("id")),
        Direction = SafeGetValue(stop.Element("direction")),
        Location = new Geopoint(new BasicGeoposition
        {
          Latitude = double.Parse(SafeGetValue(stop.Element("lat")), NumberFormatInfo.InvariantInfo),
          Longitude = double.Parse(SafeGetValue(stop.Element("lon")), NumberFormatInfo.InvariantInfo)
        }),
        Name = SafeGetValue(stop.Element("name")),
        Routes = routes,
        ParentStationId = SafeGetValue(stop.Element("parentStationId")),
        Code = SafeGetValue(stop.Element("code")),
        Accessibility = ParseWheelchairAccessibility(SafeGetValue(stop.Element("wheelchairBoarding"))),
        Type = ParseStopClassification(SafeGetValue(stop.Element("locationType")))
      };
    }

    private static string SafeGetValue(XElement element)
    {
      return SafeGetValue(element, string.Empty);
    }

    private static string SafeGetValue(XElement element, string debuggingString)
    {
      if (element != null)
      {
        return element.Value;
      }
      else
      {
        return string.Empty;
      }
    }

    #endregion

    #region Internal/Private Methods

    private static DateTime UnixTimeToDateTime(long unixTime)
    {
      return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime);
    }

    internal void ClearCache()
    {
      // stopsCache.Clear();
      // directionCache.Clear();
    }

    internal void SaveCache()
    {
      // stopsCache.Save();
      // directionCache.Save();
    }

    /// <summary>
    /// An array of regions supported by OneBusAway.org.
    /// </summary>
    internal static async Task<List<Region>> GetRegionsAsync()
    {
      if (discoveredRegions == null)
      {
        XDocument regionsDoc = null;
        try
        {
          // First try and read the regions.xml file from the local cache:
          var appStorage = ApplicationData.Current.LocalCacheFolder;
          var cachedRegionsItem = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(REGIONS_XML_FILE);
          if (cachedRegionsItem != null)
          {
            if (cachedRegionsItem.DateCreated.CompareTo(DateTimeOffset.Now.AddDays(-7)) >= 0)
            {
              using (var streamReader = new StreamReader(await (cachedRegionsItem as StorageFile).OpenStreamForReadAsync()))
              {
                string xml = streamReader.ReadToEnd();
                regionsDoc = XDocument.Parse(xml);
              }
            }
          }

          // If we've failed to find a cache file (either because it wasn't there, or because it was too old,
          // request the regions file from 
          if (regionsDoc == null)
          {
            var client = new HttpClient();
            var response = await client.GetStringAsync(REGIONS_SERVICE_URI);
            regionsDoc = XDocument.Parse(response);

            // Save the regions.xml file to isolated storage:
            var cachedRegions = await appStorage.CreateFileAsync(REGIONS_XML_FILE, CreationCollisionOption.ReplaceExisting);
            using (var streamWriter = new StreamWriter(await cachedRegions.OpenStreamForWriteAsync()))
            {
              regionsDoc.Save(streamWriter);
            }
          }
        }
        catch
        {
        }

        // If we make it here, use the backup regions.xml file:
        if (regionsDoc == null)
        {
          var cachedRegions = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///OneBusAway.Model.Regions.xml"));
          using (var streamReader = new StreamReader(await cachedRegions.OpenStreamForReadAsync()))
          {
            string xml = streamReader.ReadToEnd();
            regionsDoc = XDocument.Parse(xml);
          }
        }

        discoveredRegions = (from regionElement in regionsDoc.Descendants("region")
                             let region = new Region(regionElement)
                             where region.IsActive && region.SupportsObaRealtimeApis && region.SupportsObaDiscoveryApis
                             select region).ToList();

      }
      return discoveredRegions;
    }
    #endregion

    #region Public static methods

    /// <summary>
    /// Connects to the regions webservice to find the URL of the closets server to us so
    /// that we can support multiple regions.
    /// </summary>
    public static async Task<string> WebServiceUrlForLocationAsync(Geopoint location)
    {
      // Find the region closets to us and return it's URL:
      return (await ClosestRegionAsync(location)).RegionUrl;
    }

    /// <summary>
    /// Finds the closest region to the current location.
    /// </summary>
    public static async Task<Region> ClosestRegionAsync(Geopoint location)
    {
      var regions = await GetRegionsAsync();
      var sortedRegions = (from region in regions
                           let distance = region.DistanceFrom(location)
                           orderby distance ascending
                           select region);
      return sortedRegions.First();
    }

    public static Geopoint GetRoundedLocation(Geopoint location)
    {
      //// Round off coordinates so that we can exploit caching
      double lat = Math.Round(location.Position.Latitude * multiplier, roundingLevel) / multiplier;
      double lon = Math.Round(location.Position.Longitude * multiplier, roundingLevel) / multiplier;

      // Round off the extra decimal places to prevent double precision issues
      // from causing multiple cache entires
      Geopoint roundedLocation =
          new Geopoint(new BasicGeoposition
          {
            Latitude = Math.Round(lat, roundingLevel + 1),
            Longitude = Math.Round(lon, roundingLevel + 1)
          });

      return roundedLocation;
    }

    #endregion
  }
}
