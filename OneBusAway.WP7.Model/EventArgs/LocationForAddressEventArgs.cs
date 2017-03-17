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
using OneBusAway.Model.LocationServiceDataStructures;
using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.EventArgs
{
  public class LocationForAddressEventArgs : AModelEventArgs
    {
        public List<LocationForQuery> locations { get; private set; }
        public string query { get; private set; }
        public Geocoordinate searchNearLocation { get; private set; }

        public LocationForAddressEventArgs(List<LocationForQuery> locations, string query, Geocoordinate searchNearLocation, Exception error)
            : this(locations, query, searchNearLocation, error, null)
        {

        }

        public LocationForAddressEventArgs(List<LocationForQuery> locations, string query, Geocoordinate searchNearLocation, Exception error, object state)
            : base(error, state)
        {
            this.query = query;
            this.locations = locations;
            this.searchNearLocation = searchNearLocation;
        }
    }
}

