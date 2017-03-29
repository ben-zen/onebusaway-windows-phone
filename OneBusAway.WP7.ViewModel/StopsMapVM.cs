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
using OneBusAway.Model;
using OneBusAway.Model.BusServiceDataStructures;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace OneBusAway.ViewModel
{
  public class StopsMapVM : AViewModel
  {
    private Object stopsForLocationCompletedLock;
    private Object stopsForLocationLock;
    // this is just a map of id -> stop that mirrors StopsForLocation.
    // used to do lookups.  too bad we can't bind directly to this object's Values property.
    private IDictionary<string, Stop> stopsForLocationIndex;
    private Geopoint previousQuery;

    #region Constructors

    public StopsMapVM()
        : base()
    {
      Initialize();
    }

    public StopsMapVM(BusServiceModel busServiceModel, AppDataModel appDataModel)
        : base(busServiceModel, appDataModel)
    {
      Initialize();
    }

    private void Initialize()
    {
      stopsForLocationCompletedLock = new Object();
      stopsForLocationLock = new Object();
      stopsForLocationIndex = new Dictionary<string, Stop>();
      StopsForLocation = new List<Stop>();
      previousQuery = null;
    }

    #endregion

    #region Public Methods/Properties
    private List<Stop> _stopsForLocation;
    public List<Stop> StopsForLocation
    {
      get
      {
        return _stopsForLocation;
      }
      private set
      {
        _stopsForLocation = value;
        OnPropertyChanged("StopsForLocation");
      }
    }

    public async void LoadStopsForLocation(Geopoint center)
    {
      // If the two queries are being rounded to the same coordinate, no 
      // reason to re-parse the data out of the cache
      if (BusServiceModel.AreLocationsEquivalent(previousQuery, center) == true)
      {
        return;
      }

      operationTracker.WaitForOperation("StopsForLocation", "Loading stops...");

      previousQuery = center;

      try
      {
        StopsForLocation = await BusServiceModel.StopsForLocationAsync(center, defaultSearchRadius);
      }
      catch (Exception e)
      {
        ErrorOccured(this, e);
      }
      operationTracker.DoneWithOperation("StopsForLocation");
    }
    #endregion

  }
}
