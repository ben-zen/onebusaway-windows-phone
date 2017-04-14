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

    private static FavoritesVM _instance = null;
    public static FavoritesVM Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new FavoritesVM();
        }
        return _instance;
      }
    }

    private List<Stop> _favoriteStops;
    public List<Stop> FavoriteStops
    {
      get => _favoriteStops;
      set
      {
        _favoriteStops = value;
        OnPropertyChanged("FavoriteStops");
      }
    }

    private List<FavoriteRouteAndStop> _favorites;
    public List<FavoriteRouteAndStop> Favorites
    {
      get => _favorites;
      set
      {
        _favorites = value;
        OnPropertyChanged("Favorites");
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private FavoritesVM()
    {
      Favorites = new List<FavoriteRouteAndStop>();
      Initialize();
    }

    private async void Initialize()
    {
      Favorites = await AppDataModel.Singleton.GetFavorites(FavoriteType.Favorite);
      AppDataModel.Singleton.FavoritesChanged += FavoritesChanged;
    }

    private void FavoritesChanged(object sender, FavoritesChangedEventArgs e)
    {
      throw new NotImplementedException();
    }

    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

  }
}
