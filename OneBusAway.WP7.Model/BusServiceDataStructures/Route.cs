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
using System.Collections.Generic;
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
    public string Id { get; set; }
    public string ShortName { get; set; }
    public string LongName { get; set; }
    public string Description { get; set; }
    public Uri Url { get; set; }
    public Agency Agency { get; set; }
    public Stop ClosestStop { get; set; }
    public TransportationMethod Kind { get; set; }

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
