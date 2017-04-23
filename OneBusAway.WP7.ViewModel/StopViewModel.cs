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
using OneBusAway.Model;
using OneBusAway.Model.AppDataDataStructures;
using OneBusAway.Model.BusServiceDataStructures;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OneBusAway.ViewModel
{
  public class StopViewModel : AViewModel
  {

    #region Private Variables and Properties

    private Route routeFilter;
    private List<ArrivalAndDeparture> _unfilteredArrivals;
    private List<ArrivalAndDeparture> UnfilteredArrivals
    {
      get
      {
        return _unfilteredArrivals;
      }
      set
      {
        _unfilteredArrivals = value;
        FilterArrivals();
      }
    }
    private Object arrivalsLock;
    private TripService tripService;
    private bool resultsLoaded;

    #endregion

    #region Constructors

    // TODO: We need to convert the VM's to a Singleton, or add a Dispose method
    // currently a new VM is created every time a new route details page is opened
    // and the old event hanlders keep getting called, wasting perf
    public static StopViewModel Singleton = new StopViewModel();

    public StopViewModel(BusServiceModel busServiceModel = null, AppDataModel appDataModel = null)
        : base(busServiceModel, appDataModel)
    {
      Initialize();
    }

    private void Initialize()
    {
      UnfilteredArrivals = new List<ArrivalAndDeparture>();
      routeFilter = null;
      arrivalsLock = new Object();
      tripService = TripServiceFactory.Singleton.TripService;
      resultsLoaded = false;
    }

    private void FilterArrivals()
    {
      var filteredArrivals = new List<ArrivalAndDeparture>();
      if (UnfilteredArrivals != null)
      {
        if (routeFilter != null)
        {
          filteredArrivals.AddRange(UnfilteredArrivals.Where(arrival => arrival.RouteId == routeFilter.Id));
        }
        else
        {
          filteredArrivals.AddRange(UnfilteredArrivals);
        }
      }
      ArrivalsForStop = filteredArrivals;
    }

    #endregion

    #region Public Properties

    private List<ArrivalAndDeparture> _arrivalsForStop;
    public List<ArrivalAndDeparture> ArrivalsForStop
    {
      get
      {
        return _arrivalsForStop;
      }
      set
      {
        _arrivalsForStop = value;
        OnPropertyChanged("ArrivalsForStop");
        OnPropertyChanged("NoResultsAvailable");
      }
    }

    public bool NoResultsAvailable
    {
      get
      {
        return ArrivalsForStop.Count == 0;
      }
    }

    #endregion

    #region Public Methods

    public async Task<bool> SubscribeToToastNotification(string stopId, string tripId, int minutes)
    {
      return await tripService.StartSubscriptionAsync(stopId, tripId, minutes);
    }

    public async void SwitchToRouteByArrivalAsync(ArrivalAndDeparture arrival, Action uiCallback)
    {
      operationTracker.WaitForOperation("StopsForRoute", string.Format("Loading details for route {0}...", arrival.RouteShortName));

      Route placeholder = new Route() { Id = arrival.RouteId, ShortName = arrival.RouteShortName };
      // This will at least cause the route number to immediately update
      CurrentViewState.CurrentRoute = placeholder;
      CurrentViewState.CurrentRouteDirection = new RouteStops();

      var stops = await BusServiceModel.StopsForRouteAsync(LocationTracker.CurrentLocation, placeholder);

      stops.ForEach(routeStop =>
      {
              // These aren't always the same, hopefully this comparison will work
              if (routeStop.Name.Contains(arrival.TripHeadsign) || arrival.TripHeadsign.Contains(routeStop.Name))
        {
          CurrentViewState.CurrentRouteDirection = routeStop;
          CurrentViewState.CurrentRoute = routeStop.Route;
        }
      }
                  );

      ChangeFilterForArrivals(placeholder);
    }

    public void SwitchToStop(Stop stop)
    {
      CurrentViewState.CurrentStop = stop;
      LoadArrivalsForStopAsync(stop);
    }

    public async void LoadArrivalsForStopAsync(Stop stop, Route routeFilter = null)
    {
      UnfilteredArrivals = null;

      this.routeFilter = routeFilter;
      RefreshArrivalsForStopAsync(stop);

      // We've sent our first call off, set resultsLoaded to true
      resultsLoaded = true;
    }

    public async void RefreshArrivalsForStopAsync(Stop stop)
    {
      if (stop != null)
      {
        var location = await LocationTracker.GetLocationAsync();
        var arrivals = await BusServiceModel.ArrivalsForStopAsync(location, stop);
        var refreshedArrivals = ArrivalsForStop.Intersect(arrivals).Union(arrivals); // First, remove any elements that are not already present, then add any that were not already found.
        UnfilteredArrivals = new List<ArrivalAndDeparture>(refreshedArrivals);
      }
    }

    public void ChangeFilterForArrivals(Route routeFilter)
    {
      this.routeFilter = routeFilter;
      FilterArrivals();
    }
    #endregion
  }
}
