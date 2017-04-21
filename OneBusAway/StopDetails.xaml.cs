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
using System.Diagnostics;
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

    private string unfilterRoutesText = "all routes";
    private string filterRoutesText = "filter routes";
    private string addFavoriteText = "add";
    private string deleteFavoriteText = "delete";

    private DispatcherTimer busArrivalUpdateTimer;

    private bool isFavorite;
    private bool isFiltered;

    private const double minimumZoomRadius = 100 * 0.009 * 0.001; // 100 meters in degrees
    private const double maximumZoomRadius = 250 * 0.009; // 250 km in degrees

    #region Properties
    private string isFilteredStateId
    {
      get
      {
        string s = Guid.NewGuid().ToString();
        if (VM != null && VM.CurrentViewState != null && VM.CurrentViewState.CurrentStop != null)
        {
          s = string.Format("DetailsPage-IsFiltered-{0}", VM.CurrentViewState.CurrentStop.id);
          if (VM.CurrentViewState.CurrentRouteDirection != null && VM.CurrentViewState.CurrentRoute != null)
          {
            s += string.Format("-{0}-{1}", VM.CurrentViewState.CurrentRoute.id, VM.CurrentViewState.CurrentRouteDirection.name);
          }
        }

        return s;
      }
    }

    public StopViewModel VM => (App.Current as App).RouteDetails;
    public Stop CurrentStop { get; set; }
    #endregion

    public StopDetails()
        : base()
    {
      InitializeComponent();

      //Loaded += new RoutedEventHandler(DetailsPage_Loaded);
      //Unloaded += new RoutedEventHandler(DetailsPage_Unloaded);

      busArrivalUpdateTimer = new DispatcherTimer();
      busArrivalUpdateTimer.Interval = new TimeSpan(0, 0, 0, 30, 0); // 30 secs 
      busArrivalUpdateTimer.Tick += new EventHandler<object>(busArrivalUpdateTimer_Tick);

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      
      busArrivalUpdateTimer.Start();
      CurrentStop = e.Parameter as Stop;
      

      VM.LoadArrivalsForStopAsync(CurrentStop, null);

      // When we enter this page after tombstoning often the location won't be available when the map
      // data binding queries CurrentLocationSafe.  The center doesn't update when the property changes
      // so we need to explicitly set the center once the location is known.
      var location = await VM.LocationTracker.GetLocationAsync();
      DetailsMap.Center = location;

      //calculate distance to current stop and zoom map
      if (CurrentStop != null)
      {
        Geopoint stoplocation = new Geopoint(CurrentStop.location.Position);
        double radius = 2 * location.GetDistanceTo(stoplocation) * 0.009 * 0.001; // convert metres to degrees and double
        radius = Math.Max(radius, minimumZoomRadius);
        radius = Math.Min(radius, maximumZoomRadius);

        await DetailsMap.TrySetViewAsync(stoplocation, radius);
      }
    }

    void busArrivalUpdateTimer_Tick(object sender, object e)
    {
      VM.RefreshArrivalsForStopAsync(CurrentStop);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);

      busArrivalUpdateTimer.Stop();
      // PhoneApplicationService.Current.State[isFilteredStateId] = isFiltered;

      RouteInfo.DataContext = null;
    }

    private void appbar_favorite_Click(object sender, RoutedEventArgs e)
    {
      if (FavoritesVM.Instance.FavoriteStops.Contains(CurrentStop))
      {
        FavoritesVM.Instance.RemoveFavoriteStop(CurrentStop);
      }
      else
      {
        FavoritesVM.Instance.AddFavoriteStop(CurrentStop);
      }

      SetFavoriteIcon();
    }

    private void appbar_allroutes_Click(object sender, EventArgs e)
    {
      if (isFiltered == true)
      {
        VM.ChangeFilterForArrivals(null);
        isFiltered = false;
      }
      else
      {
        VM.ChangeFilterForArrivals(VM.CurrentViewState.CurrentRoute);
        isFiltered = true;
      }

      //SetFilterRoutesIcon();
    }

    /*private void SetFilterRoutesIcon()
    {
      if (isFiltered == false)
      {
        appbar_filter.IconUri = filterRoutesIcon;
        appbar_allroutes.Text = filterRoutesText;
      }
      else
      {
        appbar_allroutes.IconUri = unfilterRoutesIcon;
        appbar_allroutes.Text = unfilterRoutesText;
      }
    }*/

    private void SetFavoriteIcon()
    {
      if (isFavorite == true)
      {
        appbar_favorite.Label = deleteFavoriteText;
      }
      else
      {
        appbar_favorite.Label = addFavoriteText;
      }
    }

    private void ArrivalsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count != 0)
      {
        ArrivalAndDeparture arrival = (ArrivalAndDeparture)e.AddedItems[0];
        VM.SwitchToRouteByArrivalAsync(arrival, () => { });
      }
    }

    private void BusStopPushpin_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button)
      {
        string selectedStopId = (string)((Button)sender).Tag;

        Stop selectedStop = null;
        /*foreach (object item in BusStopItemsControl.Items)
        {
          Stop stop = item as Stop;
          if (stop != null && stop.id == selectedStopId)
          {
            selectedStop = stop;
            VM.SwitchToStop(selectedStop);

            break;
          }
        }*/

        Debug.Assert(selectedStop != null);

      }
    }

    private void appbar_refresh_Click(object sender, RoutedEventArgs e)
    {
      if (VM.operationTracker.Loading == false && VM.CurrentViewState.CurrentStop != null)
      {
        NoResultsTextBlock.Visibility = Visibility.Collapsed;
        VM.LoadArrivalsForStopAsync(VM.CurrentViewState.CurrentStop);
      }
    }

    private void ZoomToBus_Click(object sender, RoutedEventArgs e)
    {
      ArrivalAndDeparture a = (ArrivalAndDeparture)(((FrameworkElement)sender).DataContext);

      if (a.tripDetails != null && a.tripDetails.locationKnown == true && a.tripDetails.coordinate != null)
      {
        Geopoint location = new Geopoint(new BasicGeoposition { Latitude = a.tripDetails.coordinate.Latitude, Longitude = a.tripDetails.coordinate.Longitude });
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
          await VM.SubscribeToToastNotification(a.stopId, a.tripId, args.Minutes);
        }
      };
      await notifyPopup.ShowAsync();
    }
  }
}
