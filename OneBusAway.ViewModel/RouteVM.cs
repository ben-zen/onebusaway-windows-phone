/* Copyright (C) 2017 Ben Lewis <benjf5+github@gmail.com> 
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
using OneBusAway.Model.BusServiceDataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneBusAway.ViewModel
{
  public class RouteVM : INotifyPropertyChanged
  {
    private Route _serviceRoute;
    private bool _stopsLoaded = false;
    #region Properties
    public string Id { get; set; }
    public string ShortName { get; set; }
    public string LongName { get; set; }
    public string Description { get; set; }
    public string DisplayName => (LongName != null && LongName != String.Empty) ? LongName : Description;
    public Uri InformationUrl { get; set; }
    public TransportationMethod Kind { get; set; }
    public Agency Agency { get; set; }
    private List<RouteStops> _routeDirections;
    public List<RouteStops> RouteDirections
    {
      get => _routeDirections;
      set
      {
        _routeDirections = value;
        OnPropertyChanged("RouteDirections");
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    #endregion
    #region Constructor
    internal RouteVM(Route route)
    {
      Id = route.Id;
      ShortName = route.ShortName;
      LongName = route.LongName;
      Description = route.Description;
      InformationUrl = route.Url;
      Kind = route.Kind;
      Agency = route.Agency;
      RouteDirections = new List<RouteStops>();
      _serviceRoute = route;
    }
    #endregion
    #region Public methods
    public async Task<bool> LoadStops()
    {
      if (!_stopsLoaded)
      {
        var location = await LocationTracker.Tracker.GetLocationAsync();
        RouteDirections = await BusServiceModel.Singleton.StopsForRouteAsync(location, _serviceRoute);
        _stopsLoaded = true;
      }
      return _stopsLoaded;
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
