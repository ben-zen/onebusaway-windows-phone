﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneBusAway.Model.AppDataDataStructures
{
  public class RecentStop
  {
    public DateTime LastAccessed { get; set; }

    /// <summary>
    /// Id is unique within a given TransitService.
    /// </summary>
    public string Id { get; set; }
    public string Name { get; set; }
    public string Direction { get; set; }

    public Uri TransitServiceUri { get; set; }
    public string TransitServiceName { get; set; }
  }
}
