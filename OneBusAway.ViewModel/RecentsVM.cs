// Copyright (C) 2017, Ben Lewis <benjf5+github@gmail.com>
using OneBusAway.Model;
using OneBusAway.Model.AppDataDataStructures;
using OneBusAway.Model.BusServiceDataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace OneBusAway.ViewModel
{
  public class RecentsVM : INotifyPropertyChanged
  {
    #region Properties
    private List<RecentRoute> _recentRoutes;
    public List<RecentRoute> RecentRoutes
    {
      get => _recentRoutes;
      private set
      {
        _recentRoutes = value;
        OnPropertyChanged("RecentRoutes");
      }
    }

    private List<RecentStop> _recentStops;
    public List<RecentStop> RecentStops
    {
      get => _recentStops;
      private set
      {
        _recentStops = value;
        OnPropertyChanged("RecentStops");
      }
    }

    #endregion
    #region Public methods
    public void AddRecentRoute(RouteVM route)
    {
      var time = DateTime.Now;
      var previousRecentRoute = RecentRoutes.Find((x) => (x.Id == route.Id) && (route.Agency.Name == x.AgencyName));
      if (previousRecentRoute != null)
      {
        RecentRoutes.Remove(previousRecentRoute);
      }
      var recentRoute = new RecentRoute
      {
        Id = route.Id,
        Name = route.ShortName,
        Description = route.Description,
        LastAccessed = time,
        AgencyName = route.Agency.Name,
        //TransitServiceUri = new Uri(route.Agency.Region.RegionUrl)
      };
      RecentRoutes.Insert(0, recentRoute);
      DataModel.AddRecentRoute(recentRoute);
      OnPropertyChanged("RecentRoutes");
    }

    public void AddRecentStop(Stop stop)
    {
      var time = DateTime.Now;
      var previousRecentStop = RecentStops.Find(x => (x.Id == stop.Id));
      if (previousRecentStop != null)
      {
        RecentStops.Remove(previousRecentStop);
      }
      var recentStop = new RecentStop
      {
        Id = stop.Id,
        Name = stop.Name,
        LastAccessed = time,
        Direction = stop.Direction
      };
      RecentStops.Insert(0, recentStop);
      DataModel.AddRecentStop(recentStop);
      OnPropertyChanged("RecentStops");
    }

    public void ClearRecents()
    {
      RecentRoutes.Clear();
      RecentStops.Clear();
      DataModel.ClearRecentRoutes();
      DataModel.ClearRecentStops();
    }
    #endregion
    #region Events
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
    #region Constructor, Initialization, Singleton
    private RecentsVM()
    {
      RecentRoutes = new List<RecentRoute>();
      RecentStops = new List<RecentStop>();
      Initialize();
    }

    private void Initialize()
    {

    }
    public static RecentsVM Instance { get; } = new RecentsVM();
    #endregion
    #region Private methods, properties, etc.
    void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static AppDataModel DataModel { get; } = AppDataModel.Instance;
    #endregion
  }
}
