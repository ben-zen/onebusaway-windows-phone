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
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.LocationServiceDataStructures
{
    public enum Confidence : int
    { 
        High = 3,
        Medium = 2, 
        Low = 1,
        Unknown = 0
    };

    public class LocationForQuery
    {
        public string Name { get; set; }
        public Geopoint Location { get; set; }
        public Confidence Confidence { get; set; }
        public GeoboundingBox BoundingBox { get; set; }
    }
}
