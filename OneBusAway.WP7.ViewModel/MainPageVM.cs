﻿using System;
using System.Net;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;
using System.Reflection;
using System.Diagnostics;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.Collections.Generic;

namespace OneBusAway.WP7.ViewModel
{
    public class MainPageVM : IViewModel
    {

        #region Private Variables

        private IBusServiceModel busServiceModel;
        private IAppDataModel appDataModel;

        private int maxRoutes = 30;
        private int maxStops = 30;

        #endregion

        #region Constructors

        public MainPageVM()
            : this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.BusServiceModel")
                .GetField("Singleton")
                .GetValue(null),
                (IAppDataModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.AppDataModel")
                .GetField("Singleton")
                .GetValue(null)
            )
        {

        }

        public MainPageVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
        {
            this.busServiceModel = busServiceModel;
            this.appDataModel = appDataModel;

            StopsForLocation = new ObservableCollection<Stop>();
            RoutesForLocation = new ObservableCollection<Route>();
            Favorites = new ObservableCollection<FavoriteRouteAndStop>();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<Stop> StopsForLocation { get; private set; }
        public ObservableCollection<Route> RoutesForLocation { get; private set; }
        public ObservableCollection<FavoriteRouteAndStop> Favorites { get; private set; }

        #endregion

        #region Public Methods

        public void LoadInfoForLocation(GeoCoordinate location, int radiusInMeters)
        {
            StopsForLocation.Clear();
            busServiceModel.StopsForLocation(location, radiusInMeters);

            RoutesForLocation.Clear();
            busServiceModel.RoutesForLocation(location, radiusInMeters);
        }

        // Location is used for sorting the favorites
        public void LoadFavorites(GeoCoordinate location)
        {
            Favorites.Clear();
            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites();
            favorites.Sort(new FavoriteDistanceComparer(location));
            favorites.ForEach(favorite => Favorites.Add(favorite));
        }

        #endregion

        #region Event Handlers

        void busServiceModel_StopsForLocation_Completed(object sender, EventArgs.StopsForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.stops.Sort(new StopDistanceComparer(e.location));
                StopsForLocation.Clear();

                int count = 0;
                foreach(Stop stop in e.stops)
                {
                    if (count > maxStops)
                    {
                        break;
                    }

                    StopsForLocation.Add(stop);
                    count++;
                }
            }
        }

        void busServiceModel_RoutesForLocation_Completed(object sender, EventArgs.RoutesForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.routes.Sort(new RouteDistanceComparer(e.location));
                RoutesForLocation.Clear();

                int count = 0;
                foreach (Route route in e.routes)
                {
                    if (count > maxRoutes)
                    {
                        break;
                    }

                    RoutesForLocation.Add(route);
                    count++;
                }
            }
        }

        void appDataModel_Favorites_Changed(object sender, EventArgs.FavoritesChangedEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                // Can't sort here right now because we don't have access to the current location
                //e.newFavorites.Sort(new FavoriteDistanceComparer());
                Favorites.Clear();
                e.newFavorites.ForEach(favorite => Favorites.Add(favorite));
            }
        }

        #endregion

        public void RegisterEventHandlers()
        {
            this.busServiceModel.RoutesForLocation_Completed += new EventHandler<EventArgs.RoutesForLocationEventArgs>(busServiceModel_RoutesForLocation_Completed);
            this.busServiceModel.StopsForLocation_Completed += new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);

            this.appDataModel.Favorites_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
        }

        public void UnregisterEventHandlers()
        {
            this.busServiceModel.RoutesForLocation_Completed -= new EventHandler<EventArgs.RoutesForLocationEventArgs>(busServiceModel_RoutesForLocation_Completed);
            this.busServiceModel.StopsForLocation_Completed -= new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);

            this.appDataModel.Favorites_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
        }
    }
}
