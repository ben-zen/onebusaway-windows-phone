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
using System.Xml.Linq;

namespace OneBusAway.Model.BusServiceDataStructures
{
  public class Agency
  {
    public string Id { get; private set; }
    public string Name { get; private set; }
    public Uri Url { get; private set; }
    public string TimeZone { get; private set; }
    public string Language { get; private set; }
    public string PhoneNumber { get; private set; }
    public bool PrivateService { get; private set; }
    public Region Region { get; private set; }

    private static List<Agency> _agencies = new List<Agency>();
    public static Agency GetAgencyForXML(XElement xAgency, Region region)
    {
      var agency = _agencies.Find(x => x.Id == xAgency.Element("id").Value);
      if (agency == null)
      {
        agency = new Agency
        {
          Id = xAgency.Element("id").Value,
          Name = xAgency.Element("name").Value,
          Url = new Uri(xAgency.Element("url").Value),
          TimeZone = xAgency.Element("timezone").Value,
          Language = xAgency.Element("lang")?.Value,
          PhoneNumber = xAgency.Element("phone")?.Value,
          PrivateService = bool.Parse(xAgency.Element("privateService")?.Value)
        };
        _agencies.Add(agency);
      }
      return agency;
    }

    public override string ToString()
    {
      return string.Format("Agency: name='{0}'", Name);
    }
  }
}
