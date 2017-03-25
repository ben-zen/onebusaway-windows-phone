using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model
{
    public static class ExtensionMethods
    {
        private static double ConvertDegreesToRadians(double degrees) => degrees * (360.0 / (2 * Math.PI));

        public static double GetDistanceTo(this Geopoint thisPoint, Geopoint thatPoint)
        {
            const double earthRadius = 6371.0;
            var thisLatitude = ConvertDegreesToRadians(thisPoint.Position.Latitude);
            var thatLatitude = ConvertDegreesToRadians(thatPoint.Position.Latitude);
            var latitudeDifference = thatLatitude - thisLatitude;
            var longitudeDifference = ConvertDegreesToRadians((thatPoint.Position.Longitude - thisPoint.Position.Longitude));

            var a = Math.Pow(Math.Sin(latitudeDifference / 2), 2) + Math.Cos(thisLatitude) * Math.Cos(thatLatitude) * Math.Pow(Math.Sin(longitudeDifference / 2), 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }
    }
}
