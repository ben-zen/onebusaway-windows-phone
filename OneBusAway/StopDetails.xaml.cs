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
using OneBusAway.ViewModel;
using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;


namespace OneBusAway.View
{
  public partial class StopDetails : Page
  {
    private Uri unfilterRoutesIcon = new Uri("/Images/appbar.add.rest.png", UriKind.Relative);
    private Uri filterRoutesIcon = new Uri("/Images/appbar.minus.rest.png", UriKind.Relative);

    private DispatcherTimer busArrivalUpdateTimer;

    private const double minimumZoomRadius = 100 * 0.009 * 0.001; // 100 meters in degrees
    private const double maximumZoomRadius = 250 * 0.009; // 250 km in degrees

    #region Properties
    public StopViewModel VM { get; private set; }
    public Stop CurrentStop { get; set; }
    #endregion

    public StopDetails()
        : base()
    {
      InitializeComponent();

      busArrivalUpdateTimer = new DispatcherTimer();
      busArrivalUpdateTimer.Interval = new TimeSpan(0, 0, 0, 30, 0); // 30 secs 
      busArrivalUpdateTimer.Tick += new EventHandler<object>(busArrivalUpdateTimer_Tick);
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);
      
      busArrivalUpdateTimer.Start();
      CurrentStop = e.Parameter as Stop;
      RecentsVM.Instance.AddRecentStop(CurrentStop);

      VM = StopViewModel.GetVMForStop(CurrentStop);
      VM.RefreshArrivalsAsync();

      // When we enter this page after tombstoning often the location won't be available when the map
      // data binding queries CurrentLocationSafe.  The center doesn't update when the property changes
      // so we need to explicitly set the center once the location is known.
      var location = await LocationTracker.Tracker.GetLocationAsync();
      DetailsMap.Center = location;
      DetailsMap.ZoomLevel = 17;

      // If we're able to do a more precise job, let's figure that out now.
      if (CurrentStop != null)
      {
        if (CurrentStop.Location.GetDistanceTo(location) < 30)
        {
          var positions = new List<BasicGeoposition> { location.Position, CurrentStop.Location.Position };
          var boundingBox = GeoboundingBox.TryCompute(positions);
          if (boundingBox != null)
          {
            await DetailsMap.TrySetViewBoundsAsync(boundingBox, null, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
          }
        }
        else
        {
          // We're too far away to reasonably show how to get to the bus stop.
          DetailsMap.Center = CurrentStop.Location;
        }
      }
    }

    void busArrivalUpdateTimer_Tick(object sender, object e)
    {
      VM.RefreshArrivalsAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);
      busArrivalUpdateTimer.Stop();
      RouteInfo.DataContext = null;
    }

    private void appbar_favorite_Click(object sender, RoutedEventArgs e)
    {
      if (VM.IsFavorite)
      {
        FavoritesVM.Instance.RemoveFavoriteStop(CurrentStop);
      }
      else
      {
        FavoritesVM.Instance.AddFavoriteStop(CurrentStop);
      }
    }

    private void appbar_filter_Click(object sender, RoutedEventArgs e)
    {
      if (VM.IsFiltered)
      {
        VM.ChangeFilterForArrivals(null);
      }
      else
      {
        FlyoutBase.ShowAttachedFlyout((sender as AppBarButton));
      }
    }

    private void ArrivalsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count != 0)
      {
        ArrivalAndDeparture arrival = (ArrivalAndDeparture)e.AddedItems[0];
        VM.ChangeFilterForArrivals(Route.GetRouteForId(arrival.RouteId));
      }
    }

    private void appbar_refresh_Click(object sender, RoutedEventArgs e) => VM.RefreshArrivalsAsync();

    private void ZoomToBus_Click(object sender, RoutedEventArgs e)
    {
      ArrivalAndDeparture a = (ArrivalAndDeparture)(((FrameworkElement)sender).DataContext);

      if (a.TripDetails != null && a.TripDetails.LocationKnown == true && a.TripDetails.Coordinate != null)
      {
        Geopoint location = new Geopoint(new BasicGeoposition { Latitude = a.TripDetails.Coordinate.Latitude, Longitude = a.TripDetails.Coordinate.Longitude });
        DetailsMap.Center = location;
        DetailsMap.ZoomLevel = 17;
      }
    }

    private async void NotifyArrival_Click(object sender, RoutedEventArgs e)
    {
      ArrivalAndDeparture a = (ArrivalAndDeparture)(((FrameworkElement)sender).DataContext);

      ArrivalNotificationRequest notifyPopup = new ArrivalNotificationRequest();
      notifyPopup.Notify_Completed += async delegate (object o, NotifyEventArgs args)
      {
        if (args.OkSelected)
        {
          await VM.SubscribeToToastNotification(a.StopId, a.TripId, args.Minutes);
        }
      };
      await notifyPopup.ShowAsync();
    }
  }
}
