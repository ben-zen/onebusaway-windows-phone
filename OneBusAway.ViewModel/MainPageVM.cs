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
using OneBusAway.Model.EventArgs;
using OneBusAway.Model.LocationServiceDataStructures;
using OneBusAway.ViewModel.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace OneBusAway.ViewModel
{
  public class MainPageVM : AViewModel
  {

    #region Private Variables
    private int maxRoutes = 30;
    private int maxStops = 30;
    #endregion

    #region Constructors
    public static MainPageVM Instance { get; } = new MainPageVM();

    private MainPageVM()
        : base()
    {
      Initialize();
    }

    private void Initialize()
    {
      StopsForLocation = new List<Stop>();
      directionHelper = new Dictionary<string, ObservableCollection<RouteStops>>();
    }

    #endregion

    #region Public Properties

    private List<Stop> _stopsForLocation;
    public List<Stop> StopsForLocation
    {
      get { return _stopsForLocation; }

      private set
      {
        _stopsForLocation = value;
        OnPropertyChanged("StopsForLocation");
      }
    }

    private IDictionary<string, ObservableCollection<RouteStops>> directionHelper;
    #endregion

    #region Public Methods

    public async Task<bool> SearchByRouteAsync(string routeNumber)
    {
      var location = await LocationTracker.Tracker.GetLocationAsync();
      var routes = await BusServiceModel.SearchForRoutesAsync(location, routeNumber);
      routes.Sort(new RouteDistanceComparer(location));
      CurrentViewState.CurrentRoutes = routes;
      return routes.Count != 0;
    }

    public async Task<bool> SearchByStopAsync(string stopNumber)
    {
      var stops = await BusServiceModel.SearchForStopsAsync(await LocationTracker.Tracker.GetLocationAsync(), stopNumber);
      if (StopsForLocation != null)
      {
        StopsForLocation = new List<Stop>(StopsForLocation.Union(stops));
      }
      else
      {
        StopsForLocation = stops;
      }

      if (stops.Count > 0)
      {
        CurrentViewState.CurrentStop = stops[0];
      }

      return stops.Count != 0;
    }

    public async Task<bool> SearchByAddressAsync(string addressString)
    {
      var location = await locationModel.GetLocationForAddressAsync(addressString, await LocationTracker.Tracker.GetLocationAsync());
      CurrentViewState.CurrentSearchLocation = location;
      return true;
    }

    public async Task<bool> CheckForLocalTransitData()
    {
      var locationKnown = false;
      Geopoint location = null;
      try
      {
        location = await LocationTracker.Tracker.GetLocationAsync();
        locationKnown = true;
      }
      catch (Exception)
      {
        locationKnown = false;
      }
      return locationKnown && ((await BusServiceModel.DistanceFromClosestSupportedRegionAsync(location)) < 150000);
    }

    #endregion
  }

  public class DisplayRoute : INotifyPropertyChanged
  {
    public ObservableCollection<RouteStops> RouteStops { get; private set; }
    public event PropertyChangedEventHandler PropertyChanged;

    public DisplayRoute()
    {
      RouteStops = new ObservableCollection<RouteStops>();
      route = null;
    }

    private Route route;
    public Route Route
    {
      get
      {
        return this.route;
      }

      set
      {
        if (value != this.route)
        {
          this.route = value;
          NotifyPropertyChanged("Route");
        }
      }
    }

    private void NotifyPropertyChanged(String info)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(info));
      }
    }
  }
}
