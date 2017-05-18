// Copyright (C) 2017 Ben Lewis <benjf5+github@gmail.com>
using OneBusAway.Model;
using OneBusAway.Model.BusServiceDataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneBusAway.ViewModel
{
  public class RouteListVM : INotifyPropertyChanged
  {
    #region Properties
    private List<RouteVM> _routes = new List<RouteVM>();
    public List<RouteVM> Routes
    {
      get => _routes;
      private set
      {
        _routes = value;
        OnPropertyChanged("Routes");
      }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
    #region Public methods
    public async Task<RouteVM> GetVMForRoute(Route route)
    {
      // First, search the list of RouteVMs for this route.
      var routeVM = Routes.Find(x => x.Id == route.Id);
      if (routeVM == null)
      {
        routeVM = new RouteVM(route);
        _routes.Add(routeVM);
      }
      return routeVM;
    }

    public async Task<RouteVM> GetVMForRouteIdAsync(string routeId)
    {
      var route = Routes.Find(x => x.Id == routeId);
      if (route == null)
      {
        var location = await LocationTracker.Tracker.GetLocationAsync();
        route = await GetVMForRoute(await BusServiceModel.Singleton.GetRouteForIdAsync(location, routeId));
      }
      return route;
    }

    public async void RefreshRoutes()
    {
      try
      {
        var location = await LocationTracker.Tracker.GetLocationAsync();
        var routes = await BusServiceModel.Singleton.RoutesForLocationAsync(location, 500);
        foreach (var route in routes)
        {
          await GetVMForRoute(route);
        }
        OnPropertyChanged("Routes");
      }
      catch (Exception /* e */)
      {
        // Report an error through some service?
      }
    }
    #endregion
    #region Constructor, accessor
    public static RouteListVM Instance { get; } = new RouteListVM();
    public RouteListVM()
    {
    }
    #endregion
    #region Private methods
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
  }
}
