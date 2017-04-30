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
  public class TransitServiceViewModel : INotifyPropertyChanged
  {
    #region Properties and Events
    public event PropertyChangedEventHandler PropertyChanged;

    private List<Route> _routes;
    public List<Route> Routes
    {
      get => _routes;
      private set
      {
        _routes = value;
        OnPropertyChanged("Routes");
      }
    }

    private List<RouteVM> _routeVMs;
    public List<RouteVM> RouteVMs
    {
      get => _routeVMs;
      private set
      {
        _routeVMs = value;
        OnPropertyChanged("RouteVMs");
      }
    }

    public RouteListVM RouteList { get; } = RouteListVM.Instance;

    private List<Stop> _stops;
    public List<Stop> Stops
    {
      get => _stops;
      private set
      {
        _stops = value;
        OnPropertyChanged("Stops");
      }
    }

    #endregion
    #region Constructor, Singleton
    public static TransitServiceViewModel Instance { get; } = new TransitServiceViewModel();

    public TransitServiceViewModel()
    {
      Routes = new List<Route>();
      Stops = new List<Stop>();
      RefreshContent();
    }
    #endregion
    #region Public methods
    public async void RefreshContent()
    {
      var location = await LocationTracker.Tracker.GetLocationAsync();
      var info = await BusServiceModel.Singleton.CombinedInfoForLocationAsync(location, 500); // Needs handling for failures.
      Routes = new List<Route>(Routes.Intersect(info.Item2).Union(info.Item2));
      var routeVMs = new List<RouteVM>();
      foreach (var route in Routes)
      {
        routeVMs.Add(await RouteListVM.Instance.GetVMForRoute(route));
      }
      RouteVMs = routeVMs;
      Stops = new List<Stop>(Stops.Intersect(info.Item1).Union(info.Item1));
    }
    #endregion 
    #region Private methods
    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
