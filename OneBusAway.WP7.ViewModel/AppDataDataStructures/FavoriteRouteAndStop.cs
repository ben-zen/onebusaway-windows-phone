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
using OneBusAway.ViewModel.BusServiceDataStructures;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace OneBusAway.ViewModel.AppDataDataStructures
{
    [DataContract()]
    public class FavoriteRouteAndStop
    {
        public const int CurrentVersion = 2;

        [DataMember]
        public Route route { get; set; }
        [DataMember]
        public RouteStops routeStops { get; set; }
        [DataMember]
        public Stop stop { get; set; }
        [DataMember]
        public int version { get; set; }

        public FavoriteRouteAndStop()
        {
            version = CurrentVersion;
        }

        public string Title
        {
            get
            {
                if (routeStops != null)
                {
                    return routeStops.name;
                }
                else
                {
                    return stop.name;
                }
            }
        }

        public string Detail
        {
            get
            {
                if (routeStops != null)
                {
                    return stop.name;
                }
                else
                {
                    //string routes = "Routes: ";
                    //stop.routes.ForEach(route => routes += route.shortName + ", ");

                    //return routes.Substring(0, routes.Length - 2); // remove the trailing ", "
                    return null;
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is FavoriteRouteAndStop)
            {
                FavoriteRouteAndStop otherFavorite = (FavoriteRouteAndStop)obj;
                
                if (
                    Object.Equals(this.route, otherFavorite.route) &&
                    Object.Equals(this.stop, otherFavorite.stop) &&
                    Object.Equals(this.routeStops, otherFavorite.routeStops)
                    )
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
            return string.Format("Favorite: Route='{0}', Stop='{1}', Direction{2}", route, stop, routeStops);
        }
    }

    public class FavoriteDistanceComparer : IComparer<FavoriteRouteAndStop>
    {
        private Geocoordinate center;

        public FavoriteDistanceComparer(Geocoordinate center)
        {
            this.center = center;
        }

        public int Compare(FavoriteRouteAndStop x, FavoriteRouteAndStop y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (y == null)
            {
                return -1;
            }

            if (x == null)
            {
                return 1;
            }

            int result = x.stop.location.GetDistanceTo(center).CompareTo(y.stop.location.GetDistanceTo(center));

            if (result == 0)
            {
                if (x.route == null && y.route == null)
                {
                    result = 0;
                }
                else if (y.route == null)
                {
                    result = -1;
                }
                else if (x.route == null)
                {
                    result = 1;
                }
                else
                {
                    result = x.route.shortName.CompareTo(y.route.shortName);
                }
            }

            return result;
        }

    }
}
