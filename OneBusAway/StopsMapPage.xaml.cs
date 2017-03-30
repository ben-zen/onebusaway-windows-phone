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
using OneBusAway.ViewModel;
using OneBusAway.Model.BusServiceDataStructures;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;

namespace OneBusAway.View
{
  /// <summary>
  /// A full screen map of stops.
  /// </summary>
  /// <remarks>
  /// Supports user interaction.  Will reload stops when moved.  Touch a stop to bring up its detail page.
  /// </remarks>
  public partial class StopsMapPage : Page
  {
    private bool mapHasMoved;

    internal static int minZoomLevel = 16; //below this level we don't even bother querying

    public StopsMapPage()
        : base()
    {
      InitializeComponent();
      mapHasMoved = false;
      Loaded += new RoutedEventHandler(FullScreenMapPage_Loaded);
      DetailsMap.CenterChanged += DetailsMap_CenterChanged;

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif
    }

    #region Properties

    public StopsMapVM VM
    {
      get
      {
        return (App.Current as App).StopsMap;
      }
    }

    #endregion

    #region EventHandlers
#if DEBUG
    void zoomOutBtn_Click(object sender, RoutedEventArgs e)
    {
      DetailsMap.ZoomLevel--;
    }
#endif

    private void BusStopPushpin_Click(object sender, RoutedEventArgs e)
    {
      string selectedStopId = ((Button)sender).Tag as string;

      foreach (object item in StopsMapItemsControl.Items)
      {
        Stop stop = item as Stop;
        if (stop != null && stop.id == selectedStopId)
        {
          /*if (selectedStopId.Equals(StopInfoBox.Tag))
          {
            // This is the currently selected stop, hide the popup
            StopInfoBox.Visibility = Visibility.Collapsed;
            StopInfoBox.Tag = null;
          }
          else
          {
            // open the popup with details about the stop
            StopName.Text = stop.name;
            StopRoutes.Text = (string)new StopRoutesConverter().Convert(stop, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
            StopDirection.Text = (string)new StopDirectionConverter().Convert(stop, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
            StopInfoBox.Visibility = Visibility.Visible;
            StopInfoBox.Location = stop.location;
            StopInfoBox.PositionOrigin = PositionOrigin.BottomLeft;
            StopInfoBox.Tag = stop.id;
          }*/

          break;
        }
      }
    }

    async void FullScreenMapPage_Loaded(object sender, RoutedEventArgs e)
    {
      if (VM.CurrentViewState.CurrentSearchLocation != null)
      {
        // Using mapHasMoved prevents us from relocating the map if the user reloads this
        // page from the back stack
        if (mapHasMoved == false)
        {
          if (await DetailsMap.TrySetViewBoundsAsync(VM.CurrentViewState.CurrentSearchLocation.BoundingBox, null, MapAnimationKind.Default))
          {
            VM.LoadStopsForLocation(VM.CurrentViewState.CurrentSearchLocation.Location);
          }
        }
      }
      else
      {
        var location = await VM.LocationTracker.GetLocationAsync();
        if (!mapHasMoved)
        {
          DetailsMap.Center = location;
        }

        VM.LoadStopsForLocation(location);
      }
    }

    void DetailsMap_CenterChanged(MapControl sender, object e)
    {
      var center = sender.Center;
      mapHasMoved = true;

      if (sender.ZoomLevel >= minZoomLevel)
      {
        VM.LoadStopsForLocation(center);
      }
    }

    #endregion

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);
    }

    private void NavigateToDetailsPage(Stop stop)
    {
      VM.CurrentViewState.CurrentStop = stop;
      VM.CurrentViewState.CurrentRoute = null;
      VM.CurrentViewState.CurrentRouteDirection = null;

      (App.Current as App).RootFrame.Navigate(typeof(StopDetails), null);
    }

    /*private void PopupBtn_Click(object sender, RoutedEventArgs e)
    {
      string selectedStopId = StopInfoBox.Tag as string;

      foreach (object item in StopsMapItemsControl.Items)
      {
        Stop stop = item as Stop;
        if (stop != null && stop.id == selectedStopId)
        {
          // Hide the pop-up for when they return
          StopInfoBox.Visibility = Visibility.Collapsed;
          StopInfoBox.Tag = null;

          NavigateToDetailsPage(stop);

          break;
        }
      }
    } */
  }
}
