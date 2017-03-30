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
using OneBusAway.Model.BusServiceDataStructures;
using OneBusAway.ViewModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace OneBusAway.View
{
  public partial class BusDirectionPage : Page
  {

    public BusDirectionVM VM
    {
      get => (App.Current as App).BusDirection;
    }

    private bool informationLoaded;

    public BusDirectionPage()
        : base()
    {
      InitializeComponent();

      informationLoaded = false;

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);

      // This prevents us from refreshing the data when someone comes back to this page
      // since the bus directions aren't going to change
      if (informationLoaded == false)
      {
        VM.LoadRouteDirections(VM.CurrentViewState.CurrentRoutes);
        informationLoaded = true;
      }
      else
      {
        // If the information was already loaded clear the selection
        // This way if they navigated back to this page one entry
        // won't already be selected
        BusDirectionListBox.SelectedIndex = -1;
      }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);
    }

    private void BusDirectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count > 0)
      {
        VM.CurrentViewState.CurrentRoute = ((RouteStops)e.AddedItems[0]).route;
        VM.CurrentViewState.CurrentRouteDirection = (RouteStops)e.AddedItems[0];

        VM.CurrentViewState.CurrentStop = VM.CurrentViewState.CurrentRouteDirection.stops[0];
        foreach (Stop stop in VM.CurrentViewState.CurrentRouteDirection.stops)
        {
          // TODO: Make this call location-unknown safe.  The CurrentLocation could be unknown
          // at this point during a tombstoning scenario
          var location = VM.LocationTracker.CurrentLocation;

          if (VM.CurrentViewState.CurrentStop.CalculateDistanceInMiles(location) > stop.CalculateDistanceInMiles(location))
          {
            VM.CurrentViewState.CurrentStop = stop;
          }
        }

        (App.Current as App).RootFrame.Navigate(typeof(StopDetails), null); // This is a horrible hack and should not stand. There needs to be a better solution.
      }
    }
  }
}