/* Copyright (C) 2017 Ben Lewis.
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
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.AppDataDataStructures
{
  public class FavoriteStop
  {
    public Geopoint Location { get; private set; }

    public string Name { get; private set; }

    public string Direction { get; set; }

    public string Id { get; set; }

    public string TransitService { get; private set; }
    public Uri TransitServiceUrl { get; private set; }
    public ReportingServiceInstance ServiceInstance { get; set; }
  }
}
