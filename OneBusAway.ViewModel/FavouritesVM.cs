/* Copyright 2017 Ben Lewis.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *    http://www.apache.org/licenses/LICENSE-2.0
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneBusAway.ViewModel
{
  public class FavoritesVM : INotifyPropertyChanged
  {
    private AppDataModel DataModel { get; } = AppDataModel.Instance;
    public static FavoritesVM Instance { get; } = new FavoritesVM();


    private List<FavoriteRoute> _favoriteRoutes;
    public List<FavoriteRoute> FavoriteRoutes
    {
      get => _favoriteRoutes;
      set
      {
        _favoriteRoutes = value;
        OnPropertyChanged("FavoriteRoutes");
      }
    }

    private List<FavoriteStop> _favoriteStops;
    public List<FavoriteStop> FavoriteStops
    {
      get => _favoriteStops;
      set
      {
        _favoriteStops = value;
        OnPropertyChanged("FavoriteStops");
      }
    }

    public async void AddFavoriteRoute(Route route)
    {
      if (!FavoriteRoutes.Any(x => x.Id == route.Id))
      {
        await DataModel.AddFavoriteRoute(route);
        // refresh the favorites list.
      }
    }

    public void AddFavoriteStop(Stop stop)
    {
      throw new NotImplementedException();
    }

    public void RemoveFavoriteRoute(Route route)
    {
      throw new NotImplementedException();
    }

    public void RemoveFavoriteStop(Stop stop)
    {
      throw new NotImplementedException();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private FavoritesVM()
    {
      FavoriteRoutes = new List<FavoriteRoute>();
      FavoriteStops = new List<FavoriteStop>();
      Initialize();
    }

    private void Initialize()
    {
    }

    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

  }
}
