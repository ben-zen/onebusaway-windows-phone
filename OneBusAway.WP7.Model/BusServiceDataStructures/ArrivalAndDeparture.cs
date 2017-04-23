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
using System.ComponentModel;

namespace OneBusAway.Model.BusServiceDataStructures
{
    public class ArrivalAndDeparture : INotifyPropertyChanged
    {
        public string RouteId { get; set; }
        public string RouteShortName { get; set; }
        public string TripId { get; set; }
        public string TripHeadsign { get; set; }
        public string StopId { get; set; }

        public DateTime? PredictedArrivalTime 
        {
            get { return privatePredictedArrivalTime; }
            set 
            { 
                privatePredictedArrivalTime = value;
                OnPropertyChanged("PredictedArrivalTime");
            }
        }
        private DateTime? privatePredictedArrivalTime;
        
        public DateTime ScheduledArrivalTime { get; set; }
        
        public DateTime? PredictedDepartureTime 
        {
            get { return privatePredictedDepartureTime; }
            set
            {
                privatePredictedDepartureTime = value;
                OnPropertyChanged("PredictedDepartureTime");
                OnPropertyChanged("NextKnownDeparture");
                OnPropertyChanged("BusDelay");
            }
        }
        private DateTime? privatePredictedDepartureTime;

        public TimeSpan? BusDelay
        {
            get
            {
                if (PredictedDepartureTime != null)
                {
                    return (DateTime)PredictedDepartureTime - ScheduledDepartureTime;
                }
                else
                {
                    return null;
                }
            }
        }

        public DateTime ScheduledDepartureTime { get; set; }
        public string Status { get; set; }
        public TripDetails TripDetails { get; set; }

        public DateTime NextKnownDeparture
        {
            get
            {
                return PredictedDepartureTime != null ? (DateTime)PredictedDepartureTime : ScheduledDepartureTime;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Arrival: Route='{0}', Destination='{1}', NextArrival='{2}'",
                RouteShortName,
                TripHeadsign,
                NextKnownDeparture.ToString("HH:mm")
                );
        }

        public override bool Equals(object obj)
        {
            if (obj is ArrivalAndDeparture == false)
            {
                return false;
            }

            return ((ArrivalAndDeparture)obj).TripId == this.TripId 
                && ((ArrivalAndDeparture)obj).RouteId == this.RouteId;
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

    public class DepartureTimeComparer : IComparer<ArrivalAndDeparture>
    {
        public int Compare(ArrivalAndDeparture x, ArrivalAndDeparture y)
        {
            return x.NextKnownDeparture.CompareTo(y.NextKnownDeparture);
        }
    }
}
