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
    #region Constructors
    public TransitServiceViewModel()
    {
      Routes = new List<Route>();
      Stops = new List<Stop>();
      Initialize();
    }
    #endregion
    #region Public methods

    #endregion 
    #region Private methods
    private async void Initialize()
    {
      var location = await LocationTracker.Tracker.GetLocationAsync();
      var info = await BusServiceModel.Singleton.CombinedInfoForLocationAsync(location, 500); // Needs handling for failures.
      Routes = new List<Route>(Routes.Intersect(info.Item2).Union(info.Item2));
      Stops = new List<Stop>(Stops.Intersect(info.Item1).Union(info.Item1));
    }
    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
