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
    public class Stop
    {
        public string Id { get; set; }
        public string Direction { get; set; }
        public string Name { get; set; }
        public List<Route> Routes { get; set; }
        public Coordinate Coordinate { get; set; }

        public Geopoint Location
        {
            get
            {
                if (Coordinate != null)
                {
                    return new Geopoint (new BasicGeoposition
                    {
                        Latitude = Coordinate.Latitude,
                        Longitude = Coordinate.Longitude
                    });
                }
                else
                {
                    return null;
                }
            }

            set
            {
                if (value != null)
                {
                    Coordinate = new Coordinate
                    {
                        Latitude = value.Position.Latitude,
                        Longitude = value.Position.Longitude
                    };
                }
                else
                {
                    Coordinate = null;
                }
            }
        }

        private const double kmPerMile = 1.60934400000644;

        public double CalculateDistanceInMiles(Geopoint location2)
        {
            double meters = Location.GetDistanceTo(location2);
            return meters / (1000.0 * kmPerMile);
        }

        private static double toRadian(double val)
        {
            return (Math.PI / 180) * val;
        }

        public override bool Equals(object obj)
        {
            if (obj is Stop == false)
            {
                return false;
            }

            return ((Stop)obj).Id == this.Id;
        }

        public override string ToString()
        {
            return string.Format("Stop: name='{0}'", Name);
        }
    }

    public class StopDistanceComparer : IComparer<Stop>
    {
        private Geopoint center;

        public StopDistanceComparer(Geopoint center)
        {
            this.center = center;
        }

        public int Compare(Stop x, Stop y)
        {
            int result = x.CalculateDistanceInMiles(center).CompareTo(y.CalculateDistanceInMiles(center));

            // If stops are the same distance sort alphabetically
            if (result == 0)
            {
                result = x.Name.CompareTo(y.Name);
            }

            return result;
        }
        
    }
}
