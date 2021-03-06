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

namespace OneBusAway.Model.BusServiceDataStructures
{
    public class ScheduleStopTime
    {
        public DateTime ArrivalTime { get; set; }
        public DateTime DepartureTime { get; set ;}
        public string ServiceId { get; set; }
        public string TripId { get; set; }
    }
}
