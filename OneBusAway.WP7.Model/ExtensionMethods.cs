using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model
{
  public static class ExtensionMethods
  {
    private static double ConvertDegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

    public static double GetDistanceTo(this Geopoint thisPoint, Geopoint thatPoint)
    {
      const double earthRadius = 6371;
      var thisLatitude = ConvertDegreesToRadians(thisPoint.Position.Latitude);
      var thatLatitude = ConvertDegreesToRadians(thatPoint.Position.Latitude);
      var latitudeDifference = thatLatitude - thisLatitude;
      var longitudeDifference = ConvertDegreesToRadians(thatPoint.Position.Longitude) - ConvertDegreesToRadians(thisPoint.Position.Longitude);

      var haversine = Math.Pow(Math.Sin(latitudeDifference / 2), 2) + (Math.Cos(thisLatitude) * Math.Cos(thatLatitude) * Math.Pow(Math.Sin(longitudeDifference / 2), 2));

      return 2 * earthRadius * Math.Asin(Math.Sqrt(haversine));
    }
  }
}
