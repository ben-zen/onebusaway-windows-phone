using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
  }
}
