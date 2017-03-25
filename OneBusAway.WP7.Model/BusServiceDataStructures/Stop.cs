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
using System.Runtime.Serialization;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.BusServiceDataStructures
{
    [DataContract()]
    public class Stop
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string direction { get; set; }
        [DataMember]
        public string name { get; set; }
        public List<Route> routes { get; set; }
        [DataMember]
        public Coordinate coordinate { get; set; }

        public Geopoint location
        {
            get
            {
                if (coordinate != null)
                {
                    return new Geopoint (new BasicGeoposition
                    {
                        Latitude = coordinate.Latitude,
                        Longitude = coordinate.Longitude
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
                    coordinate = new Coordinate
                    {
                        Latitude = value.Position.Latitude,
                        Longitude = value.Position.Longitude
                    };
                }
                else
                {
                    coordinate = null;
                }
            }
        }

        private const double kmPerMile = 1.60934400000644;

        public double CalculateDistanceInMiles(Geopoint location2)
        {
            double meters = location.GetDistanceTo(location2);
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

            return ((Stop)obj).id == this.id;
        }

        public override string ToString()
        {
            return string.Format("Stop: name='{0}'", name);
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
                result = x.name.CompareTo(y.name);
            }

            return result;
        }
        
    }
}
