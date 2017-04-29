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
using OneBusAway.ViewModel;
using OneBusAway.Model.AppDataDataStructures;
using OneBusAway.Model.BusServiceDataStructures;
using OneBusAway.Model.LocationServiceDataStructures;
using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace OneBusAway.View
{
  public partial class MainPage : Page
  {
    public FavoritesVM Favorites => FavoritesVM.Instance;
    public RecentsVM Recents => RecentsVM.Instance;
    public MainPageVM VM => (App.Current as App).MainPageVM;
    public TransitServiceViewModel TransitService => (App.Current as App).TransitService;

    private bool firstLoad;
    private bool navigatedAway;
    private Object navigationLock;
    private const string searchErrorMessage =
        "Search for a route: 44\r\n" +
        "Search by stop number: 11132\r\n" +
        "Find a landmark: Space Needle\r\n" +
        "Or an address: 1 Microsoft Way";

    public MainPage()
        : base()
    {
      InitializeComponent();

      firstLoad = true;
      navigatedAway = false;
      navigationLock = new Object();

      this.Loaded += new RoutedEventHandler(MainPage_Loaded);
    }

    async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {

      if (firstLoad == true)
      {
        // Since this is the first load, pull down the bus and stop info
        // VM.LoadInfoForLocation();
        /*
        // In this case, we've been re-created after a tombstone, resume their previous pivot
        if (PhoneApplicationService.Current.State.ContainsKey("MainPageSelectedPivot") == true)
        {
          PC.SelectedIndex = (int)(MainPagePivots)PhoneApplicationService.Current.State["MainPageSelectedPivot"];
        }
        // The app was started fresh, not from tombstone.  Check pivot settings.  If there isn't a setting,
        // default to the last used pivot
        else if (IsolatedStorageSettings.ApplicationSettings.Contains("DefaultMainPagePivot") == true &&
                (MainPagePivots)IsolatedStorageSettings.ApplicationSettings["DefaultMainPagePivot"] >= 0)
        {
          PC.SelectedIndex = (int)(MainPagePivots)IsolatedStorageSettings.ApplicationSettings["DefaultMainPagePivot"];
        }
        else
        {
          // Is is set to use the previous pivot, if this key doesn't exist just leave
          // the pivot selection at the default
          if (IsolatedStorageSettings.ApplicationSettings.Contains("LastUsedMainPagePivot") == true)
          {
            PC.SelectedIndex = (int)(MainPagePivots)IsolatedStorageSettings.ApplicationSettings["LastUsedMainPagePivot"];
          }
        }
        */
      }
      firstLoad = false;

      // Load favorites every time because they might have changed since the last load
      //Favorites.LoadFavorites();
      /*
      var supported = await VM.CheckForLocalTransitData();
      if (supported)
      {
        VM.LoadInfoForLocation();
      }
      else
      {
        var modal = new MessageDialog("Currently the OneBusAway service does not support your location." +
                                      "Many functions of this app will not work.");
        await modal.ShowAsync();
      }
      */
      var location = await VM.LocationTracker.GetLocationAsync();
      StopsMap.Center = location;
    }

    #region Navigation

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      navigatedAway = false;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);
      /*
      // Store it in the state variable for tombstoning
      PhoneApplicationService.Current.State["ShowLoadingSplash"] = false;
      PhoneApplicationService.Current.State["MainPageSelectedPivot"] = (MainPagePivots)PC.SelectedIndex;

      // This is for the last-used pivot on fresh load
      IsolatedStorageSettings.ApplicationSettings["LastUsedMainPagePivot"] = (MainPagePivots)PC.SelectedIndex;
      */
    }

    #endregion

    #region UI element event handlers

    private void appbar_refresh_Click(object sender, RoutedEventArgs e)
    {
      VM.LoadInfoForLocation(true);
    }

    private void appbar_search_Click(object sender, RoutedEventArgs e)
    {
      if (SearchPanel.Opacity == 0)
      {
        SearchInputBox.Focus(FocusState.Programmatic);
        SearchInputBox.SelectAll();
      }
      else
      {
        ProcessSearch(SearchInputBox.Text);
      }
    }


    private void SearchInputBox_LostFocus(object sender, RoutedEventArgs e)
    {
      this.Focus(FocusState.Programmatic);
    }

    private void SearchInputBox_KeyUp(object sender, object e)
    {
      string searchString = SearchInputBox.Text;

      /*if (e.Key == Key.Enter)
      {
        ProcessSearch(searchString);
      }*/
    }

    private async void ProcessSearch(string searchString)
    {
      int number = 0;
      bool canConvert = int.TryParse(searchString, out number); //check if it's a number
      if (canConvert == true) //it's a route or stop number
      {
        if (number < 1000) //route number
        {
          var routeFound = await VM.SearchByRouteAsync(searchString);

          if (routeFound)
          {
            (Window.Current.Content as Frame).Navigate(typeof(BusDirectionPage), null);
          }
        }
        else //stop number
        {
          var stopFound = await VM.SearchByStopAsync(searchString);

          if (stopFound)
          {
            (Window.Current.Content as Frame).Navigate(typeof(StopDetails), null);
          }
        }
      }
      else if (string.IsNullOrEmpty(searchString) == false) // Try to find the location
      {
        var addressFound = await VM.SearchByAddressAsync(searchString);
        if (addressFound)
        {
          (Window.Current.Content as Frame).Navigate(typeof(StopsMapPage), null);
        }
      }
    }


    private void appbar_settings_Click(object sender, RoutedEventArgs e)
    {
      (Window.Current.Content as Frame).Navigate(typeof(SettingsPage), null);
    }

    private void appbar_about_Click(object sender, RoutedEventArgs e)
    {
      (Window.Current.Content as Frame).Navigate(typeof(AboutPage), null);
    }

    private void stopsMapBtn_Click(object sender, RoutedEventArgs e)
    {
      VM.CurrentViewState.CurrentRoute = null;
      VM.CurrentViewState.CurrentRouteDirection = null;
      VM.CurrentViewState.CurrentSearchLocation = null;
      VM.CurrentViewState.CurrentStop = null;

      (Window.Current.Content as Frame).Navigate(typeof(StopsMapPage), null);
    }

    private void RouteDirection_Tap(object sender, object e)
    {
      RouteStops routeStops = (sender as FrameworkElement).DataContext as RouteStops;
      VM.CurrentViewState.CurrentRoutes = new List<Route>() { (Route)routeStops.Route };

      VM.CurrentViewState.CurrentRoute = routeStops.Route;
      VM.CurrentViewState.CurrentRouteDirection = routeStops;

      VM.CurrentViewState.CurrentStop = VM.CurrentViewState.CurrentRouteDirection.Stops[0];
      foreach (Stop stop in VM.CurrentViewState.CurrentRouteDirection.Stops)
      {
        // TODO: Make this call location-unknown safe.  The CurrentLocation could be unknown
        // at this point during a tombstoning scenario
        Geopoint location = VM.LocationTracker.CurrentLocation;

        if (VM.CurrentViewState.CurrentStop.CalculateDistanceInMiles(location) > stop.CalculateDistanceInMiles(location))
        {
          VM.CurrentViewState.CurrentStop = stop;
        }
      }

      (Window.Current.Content as Frame).Navigate(typeof(StopDetails), null);
    }

    private void PC_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //we bind the DataContext only when the pivot is naivgated to. This improves perf if Favs or Recent are the first pivots
      FrameworkElement selectedElement = ((sender as Pivot).SelectedItem as PivotItem).Content as FrameworkElement;
      selectedElement.DataContext = VM;
    }

    #endregion


    private void StopsListClick(object sender, ItemClickEventArgs e)
    {
      (Window.Current.Content as Frame).Navigate(typeof(StopDetails), e.ClickedItem);
    }

    private void RouteListClick(object sender, ItemClickEventArgs e)
    {
      (Window.Current.Content as Frame).Navigate(typeof(RouteDetails), e.ClickedItem);
    }

    private void RecentRouteClicked(object sender, ItemClickEventArgs e)
    {
      var route = TransitService.Routes.Find((x) => x.Id == (e.ClickedItem as RecentRoute).Id);
      if (route != null)
      {
        (Window.Current.Content as Frame).Navigate(typeof(RouteDetails), route);
      }
    }

    private void RecentStopClicked(object sender, ItemClickEventArgs e)
    {
      var stop = TransitService.Stops.Find(x => x.Id == (e.ClickedItem as RecentStop).Id);
      if (stop != null)
      {
        (Window.Current.Content as Frame).Navigate(typeof(StopDetails), e.ClickedItem);
      }
    }

    private void FavoriteRouteClicked(object sender, ItemClickEventArgs e)
    {
      var route = TransitService.Routes.Find(x => x.Id == (e.ClickedItem as FavoriteRoute).Id);
      if (route != null)
      {
        (Window.Current.Content as Frame).Navigate(typeof(RouteDetails), route);
      }
    }

    private void FavoriteStopClicked(object sender, ItemClickEventArgs e)
    {
      var stop = TransitService.Stops.Find(x => x.Id == (e.ClickedItem as FavoriteStop).Id);
      if (stop != null)
      {
        (Window.Current.Content as Frame).Navigate(typeof(StopDetails), stop);
      }
    }
  }
}
