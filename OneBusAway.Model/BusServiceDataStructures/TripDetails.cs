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
using System.ComponentModel;
using Windows.Devices.Geolocation;

namespace OneBusAway.Model.BusServiceDataStructures
{
    public class TripDetails : INotifyPropertyChanged
    {
        public string TripId { get; set; }
        public DateTime ServiceDate { get; set; }
        public int? ScheduleDeviationInSec { get; set; }
        public string ClosestStopId { get; set; }
        public int? ClosestStopTimeOffset { get; set; }

        private Coordinate coordinatePrivate;

        public Coordinate Coordinate 
        {
            get
            {
                return coordinatePrivate;
            }

            set
            {
                coordinatePrivate = value;

                OnPropertyChanged("Coordinate");
                OnPropertyChanged("LocationKnown");
                OnPropertyChanged("Location");
            }
        }

        public bool LocationKnown
        {
            get
            {
                return Coordinate != null;
            }
        }

        public Geopoint Location
        {
            get
            {
                if (Coordinate == null) return null;

                return new Geopoint (new BasicGeoposition
                {
                    Latitude = Coordinate.Latitude,
                    Longitude = Coordinate.Longitude
                });
            }

            set
            {
                if (value != null)
                {
                    Coordinate = new Coordinate
                    {
                        Latitude = value.Position.Latitude,
                        Longitude = value.Position.Longitude
                    };
                }
                else
                {
                    Coordinate = null;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
