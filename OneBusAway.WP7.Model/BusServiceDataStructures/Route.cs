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
using OneBusAway.Model;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.BusServiceDataStructures
{
  public enum TransportationMethod
  {
    Tram,
    Subway,
    Rail,
    Bus,
    Ferry,
    CableCar,
    Gondola,
    Funicular
  }
  public class Route
  {
    #region Properties
    public string Id { get; private set; }
    public string ShortName { get; private set; }
    public string LongName { get; private set; }
    public string Description { get; private set; }
    public Uri Url { get; private set; }
    public Agency Agency { get; private set; }
    public Stop ClosestStop { get; set; }
    public TransportationMethod Kind { get; private set; }
    #endregion
    #region Parsers and accessor

    private static TransportationMethod ParseTransportationMethod(string method)
    {
      int intMethod;
      if (!int.TryParse(method, out intMethod))
      {
        // Handle this parse error.
      }

      if (intMethod > 7 || intMethod < 0)
      {
        // invalid TransportationMethod value.
      }

      return (TransportationMethod)intMethod;
    }
    private static List<Route> _routes = new List<Route>();
    public static Route GetRouteForXML(XElement xRoute, Agency agency)
    {
      var route = _routes.Find(x => x.Id == xRoute.Element("id").Value);
      if (route == null)
      {
        route = new Route
        {
          Id = xRoute.Element("id").Value,
          ShortName = XmlUtilities.SafeGetValue(xRoute.Element("shortName")),
          LongName = XmlUtilities.SafeGetValue(xRoute.Element("longName")),
          Url = (XmlUtilities.SafeGetValue(xRoute.Element("url")) != String.Empty) ? new Uri(XmlUtilities.SafeGetValue(xRoute.Element("url"))) : agency.Url,
          Description = XmlUtilities.SafeGetValue(xRoute.Element("description")),
          Kind = ParseTransportationMethod(XmlUtilities.SafeGetValue(xRoute.Element("type"))),
          Agency = agency
        };
        _routes.Add(route);
      }
      return route;
    }

    public static Route GetRouteForId(string id)
    {
      return _routes.Find(x => x.Id == id);
    }

    #endregion
    public override bool Equals(object obj)
    {
      if (obj is Route)
      {
        Route otherRoute = (Route)obj;
        if (otherRoute.Id == this.Id)
        {
          return true;
        }
        else
        {
          return false;
        }
      }
      else
      {
        return false;
      }
    }

    public override string ToString()
    {
      return string.Format("Route: ID='{0}', description='{1}'", ShortName, Description);
    }
  }

  public class RouteDistanceComparer : IComparer<Route>
  {
    private Geopoint center;

    public RouteDistanceComparer(Geopoint center)
    {
      this.center = center;
    }

    public int Compare(Route x, Route y)
    {
      if (x.ClosestStop == null && y.ClosestStop == null)
      {
        return 0;
      }

      if (x.ClosestStop == null)
      {
        return -1;
      }

      if (y.ClosestStop == null)
      {
        return 1;
      }

      int result = x.ClosestStop.Location.GetDistanceTo(center).CompareTo(y.ClosestStop.Location.GetDistanceTo(center));

      // If the bus routes have the same closest stop sort by route number
      if (result == 0)
      {
        result = x.ShortName.CompareTo(y.ShortName);
      }

      return result;
    }
  }
}
