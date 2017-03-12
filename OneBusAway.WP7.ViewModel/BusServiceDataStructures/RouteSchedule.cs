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
using System.Runtime.Serialization;

namespace OneBusAway.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class RouteSchedule
    {
        [DataMember()]
        public Route route { get; set; }
        [DataMember()]
        public List<DirectionSchedule> directions { get; set; }

        public override string ToString()
        {
            string s = string.Format("RouteSchedule: route='{0}'", route);
            foreach (DirectionSchedule direction in directions)
            {
                s += string.Format(", direction='{0}'", direction);
            }

            return s;
        }
    }
}
