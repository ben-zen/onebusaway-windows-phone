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
using System;
using OneBusAway.Model.AppDataDataStructures;
using System.Collections.Generic;
using OneBusAway.Model.EventArgs;

namespace OneBusAway.Model
{
    public enum FavoriteType
    {
        Favorite,
        Recent
    };

    public interface IAppDataModel
    {

        event EventHandler<FavoritesChangedEventArgs> FavoritesChanged;

        event EventHandler<FavoritesChangedEventArgs> RecentsChanged;

        void AddFavorite(FavoriteRouteAndStop favorite, FavoriteType type);

        List<FavoriteRouteAndStop> GetFavorites(FavoriteType type);

        void DeleteFavorite(FavoriteRouteAndStop favorite, FavoriteType type);

        void DeleteAllFavorites(FavoriteType type);

        bool IsFavorite(FavoriteRouteAndStop favorite, FavoriteType type);

    }
}
