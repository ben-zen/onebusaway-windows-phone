﻿/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
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
using OneBusAway.Model.BusServiceDataStructures;
using OneBusAway.Model.EventArgs;
using Windows.Devices.Geolocation;

namespace OneBusAway.ViewModel.EventArgs
{
    public class CombinedInfoForLocationEventArgs : AModelEventArgs
    {
        public List<Stop> stops { get; private set; }
        public List<Route> routes { get; private set; }
        public Geocoordinate location { get; private set; }

        public CombinedInfoForLocationEventArgs(List<Stop> stops, List<Route> routes, Geocoordinate location, Exception error)
            : base(error)
        {
            this.stops = stops;
            this.routes = routes;
            this.location = location;
        }
    }
}
