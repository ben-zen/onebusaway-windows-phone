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
using System.Collections.ObjectModel;
using OneBusAway.ViewModel.BusServiceDataStructures;
using System.Diagnostics;
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

    public StopsMapVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
        : base(busServiceModel, appDataModel)
    {
      Initialize();
    }

    private void Initialize()
    {
      stopsForLocationCompletedLock = new Object();
      stopsForLocationLock = new Object();
      stopsForLocationIndex = new Dictionary<string, Stop>();
      StopsForLocation = new ObservableCollection<Stop>();
      previousQuery = null;
    }

    #endregion

    #region Public Methods/Properties

    public ObservableCollection<Stop> StopsForLocation { get; private set; }

    public async void LoadStopsForLocation(Geopoint center)
    {
      // If the two queries are being rounded to the same coordinate, no 
      // reason to re-parse the data out of the cache
      if (busServiceModel.AreLocationsEquivalent(previousQuery, center) == true)
      {
        return;
      }

      operationTracker.WaitForOperation("StopsForLocation", "Loading stops...");

      previousQuery = center;

      try
      {
        var stops = await busServiceModel.StopsForLocation(center, defaultSearchRadius);
        var newStops = new Dictionary<string, Stop>();
        foreach (var stop in stops)
        {
          newStops.Add(stop.id, stop);
        }

        SetStopsForLocation(newStops);
      }
      catch (Exception e)
      {
        ErrorOccured(this, e);
      }
      operationTracker.DoneWithOperation("StopsForLocation");
    }
    #endregion

    /// <summary>
    /// Sets the contents of StopsForLocation to the Values of the specified Dictionary.
    /// </summary>
    /// <remarks>
    /// Implementation does not clear StopsForLocation (so as not to clear the screen).
    /// Instead, removes all stops that don't belong and adds ones that do.
    /// </remarks>
    /// <param name="newStops"></param>
    private void SetStopsForLocation(IDictionary<string, Stop> newStops)
    {
      // .NET for the phone doesn't support HashSet.... Use Dictionary as a poor-man's hashset.
      // linear pass to calculate the set of stops to remove
      IDictionary<string, Stop> stopsToRemove = new Dictionary<string, Stop>();
      lock (stopsForLocationLock)
      {
        foreach (string stopId in stopsForLocationIndex.Keys)
        {
          if (!newStops.ContainsKey(stopId))
          {
            stopsToRemove.Add(stopId, stopsForLocationIndex[stopId]);
          }
        }
      }

      if (stopsToRemove.Count > 0)
      {
        // O(n^2) pass to remove them.
        // An ObservableSet or ObservableDictionary would be nice.
        UIAction(() =>
        {
          lock (stopsForLocationLock)
          {
            foreach (Stop s in stopsToRemove.Values)
            {
              StopsForLocation.Remove(s);
              stopsForLocationIndex.Remove(s.id);
            }
          }
        });
      }

      // and a linear pass to add any new stops
      if (newStops.Count > 0)
      {
        lock (stopsForLocationLock)
        {
          foreach (Stop s in newStops.Values)
          {
            if (!stopsForLocationIndex.ContainsKey(s.id))
            {
              // Create a local reference so it will still be valid when
              // the UI thread executes
              Stop currentStop = s;
              UIAction(() =>
              {
                lock (stopsForLocationLock)
                {
                                // Check this again to make sure another thread didn't
                                // add this stop while we were waiting
                                if (!stopsForLocationIndex.ContainsKey(currentStop.id))
                  {
                    StopsForLocation.Add(currentStop);
                    stopsForLocationIndex.Add(currentStop.id, currentStop);
                  }
                }
              });
            }
          }
        }
      }
    }
  }
}
