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
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.BusServiceDataStructures
{
    public class RouteStops
    {
        public Route Route { get; set; }
        public string Name { get; set; }
        public List<Stop> Stops { get; set; }
        public List<PolyLine> EncodedPolylines { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is RouteStops == false)
            {
                return false;
            }

            return ((RouteStops)obj).Name == Name;
        }

        public override string ToString()
        {
            return string.Format("RouteStops: name='{0}'", Name);
        }
    }

    public class RouteStopsDistanceComparer : IComparer<RouteStops>
    {
        private Geopoint center;

        public RouteStopsDistanceComparer(Geopoint center)
        {
            this.center = center;
        }

        public int Compare(RouteStops x, RouteStops y)
        {
            if (x.Route == null && y.Route == null)
            {
                return 0;
            }

            if (x.Route == null)
            {
                return -1;
            }

            if (y.Route == null)
            {
                return 1;
            }

            if (x.Route.ClosestStop == null && y.Route.ClosestStop == null)
            {
                return 0;
            }

            if (x.Route.ClosestStop == null)
            {
                return -1;
            }

            if (y.Route.ClosestStop == null)
            {
                return 1;
            }

            int result = x.Route.ClosestStop.Location.GetDistanceTo(center).CompareTo(y.Route.ClosestStop.Location.GetDistanceTo(center));

            // If the bus routes have the same closest stop sort by route number
            if (result == 0)
            {
                result = x.Route.ShortName.CompareTo(y.Route.ShortName);
            }

            // If the bus routes have the same stop and number (this will happen for the two different
            // directions of the same bus route) then sort alphabetically by description
            if (result == 0)
            {
                result = x.Name.CompareTo(y.Name);
            }

            return result;
        }
    }
}
