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
using System.Runtime.Serialization;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.LocationServiceDataStructures
{
    public enum Confidence : int
    { 
        High = 0,
        Medium = 1, 
        Low = 2,
        Unknown = -1
    };

    [DataContract]
    public class LocationForQuery
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public Geopoint location { get; set; }
        [DataMember]
        public Confidence confidence { get; set; }
        [DataMember]
        public GeoboundingBox boundingBox { get; set; }
    }
}
