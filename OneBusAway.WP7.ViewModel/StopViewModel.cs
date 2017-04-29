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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace OneBusAway.ViewModel
{
  public class StopViewModel : INotifyPropertyChanged
  {

    #region Private Variables, Properties, and Methods
    private Stop _stop;
    private Route _routeFilter = null;
    private Route RouteFilter
    {
      get => _routeFilter;
      set
      {
        _routeFilter = value;
        OnPropertyChanged("IsFiltered");
      }
    }
    private List<ArrivalAndDeparture> _unfilteredArrivals = new List<ArrivalAndDeparture>();
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
    private Object arrivalsLock = new object();

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion

    #region Constructors

    // TODO: We need to convert the VM's to a Singleton, or add a Dispose method
    // currently a new VM is created every time a new route details page is opened
    // and the old event hanlders keep getting called, wasting perf
    private static List<StopViewModel> _stops = new List<StopViewModel>();
    public static StopViewModel GetVMForStop(Stop stop)
    {
      var stopVM = _stops.Find(x => x.Id == stop.Id);
      if (stopVM == null)
      {
        stopVM = new StopViewModel(stop);
        _stops.Add(stopVM);
      }
      return stopVM;
    }

    internal StopViewModel(Stop stop)
    {
      _stop = stop;
      Id = stop.Id;
      Direction = stop.Direction;
      Name = stop.Name;
    }

    private void FilterArrivals()
    {
      var filteredArrivals = new List<ArrivalAndDeparture>();
      if (UnfilteredArrivals != null)
      {
        if (RouteFilter != null)
        {
          filteredArrivals.AddRange(UnfilteredArrivals.Where(arrival => arrival.RouteId == RouteFilter.Id));
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

    public string Id { get; private set; }
    public string Direction { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public List<RouteVM> Routes { get; private set; }
    public List<Agency> Agencies { get; private set; }
    private List<ArrivalAndDeparture> _arrivalsForStop = new List<ArrivalAndDeparture>();
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

    public bool IsFavorite
    {
      get
      {
        return FavoritesVM.Instance.FavoriteStops.Any(x => x.Id == Id);
      }
    }
    public bool IsFiltered => RouteFilter != null;


    public bool NoResultsAvailable
    {
      get
      {
        return ArrivalsForStop.Count == 0;
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Public Methods

    public async Task<bool> SubscribeToToastNotification(string stopId, string tripId, int minutes)
    {
      return await TripService.Instance.StartSubscriptionAsync(stopId, tripId, minutes);
    }

    public async void RefreshArrivalsAsync()
    {
      var location = await LocationTracker.Tracker.GetLocationAsync();
      try
      {
        var arrivals = await BusServiceModel.Singleton.ArrivalsForStopAsync(location, _stop);
        var refreshedArrivals = ArrivalsForStop.Intersect(arrivals).Union(arrivals); // First, remove any elements that are not already present, then add any that were not already found.
        UnfilteredArrivals = new List<ArrivalAndDeparture>(refreshedArrivals);
      }
      catch (Exception /* e */)
      {
        // We might be under lock, we might have had some other network failure. Consider creating a new piece of UI to show this.
      }
    }

    public void ChangeFilterForArrivals(Route routeFilter)
    {
      RouteFilter = routeFilter;
      FilterArrivals();
    }
    #endregion
  }
}
