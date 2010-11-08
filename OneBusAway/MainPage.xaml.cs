﻿using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Controls.Maps.Core;
using Microsoft.Phone.Shell;
using OneBusAway.WP7.ViewModel;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Diagnostics;
using System.Windows.Threading;

namespace OneBusAway.WP7.View
{
    public partial class MainPage : AViewPage
    {
        private MainPageVM viewModel;
        private bool firstLoad;
        private Popup popup;

        public MainPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            // It is the first launch of the app if this key doesn't exist.  Otherwise we are returning
            // to the main page after tombstoning and showing the splash screen looks bad
            if (PhoneApplicationService.Current.State.ContainsKey("MainPageSelectedPivot") == false)
            {
                ShowLoadingSplash();
            }

            viewModel = aViewModel as MainPageVM;
            firstLoad = true;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            SupportedOrientations = SupportedPageOrientation.Portrait;
        }

        private void ShowLoadingSplash()
        {
            ApplicationBar.IsVisible = false;

            this.popup = new Popup();
            this.popup.Child = new PopupSplash();
            this.popup.IsOpen = true;

            DispatcherTimer splashTimer = new DispatcherTimer();
            splashTimer.Interval = new TimeSpan(0, 0, 0, 3, 0); // 5 secs
            splashTimer.Tick += new EventHandler(splashTimer_Tick);
            splashTimer.Start();
        }

        void splashTimer_Tick(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(() => { HideLoadingSplash(); });

            (sender as DispatcherTimer).Stop();
        }

        private void HideLoadingSplash()
        {
            if (this.popup != null)
            {
                this.popup.IsOpen = false;
            }

            ApplicationBar.IsVisible = true;
            SystemTray.IsVisible = true;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.RegisterEventHandlers(Dispatcher);
            viewModel.LoadFavorites();
            viewModel.LoadInfoForLocation();

            viewModel.CheckForLocalTransitData(delegate(bool hasData)
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        if (hasData == false)
                        {
                            MessageBox.Show(
                                "Currently the OneBusAway service only supports Seattle and the surrounding counties. " +
                                "Many functions of this app will not work in your current location."
                                );
                        }
                    });
            });

            viewModel.LocationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    Dispatcher.BeginInvoke(() => StopsMap.Center = location);
                }
            );

            HideLoadingSplash();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (firstLoad == true && PhoneApplicationService.Current.State.ContainsKey("MainPageSelectedPivot") == true)
            {
                PC.SelectedIndex = Convert.ToInt32(PhoneApplicationService.Current.State["MainPageSelectedPivot"]);
                firstLoad = false;
            }

            // We refresh this info every load so clear the lists now
            // to avoid a flicker as the page comes back
            viewModel.DisplayRouteForLocation.Clear();
            viewModel.StopsForLocation.Clear();
            viewModel.Favorites.Clear();
            viewModel.Recents.Clear();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            PhoneApplicationService.Current.State["MainPageSelectedPivot"] = PC.SelectedIndex;

            viewModel.UnregisterEventHandlers();
        }

        private void appbar_refresh_Click(object sender, EventArgs e)
        {
            viewModel.LoadInfoForLocation(true);
        }

        private void FavoritesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                FavoriteRouteAndStop favorite = (FavoriteRouteAndStop)e.AddedItems[0];
                viewModel.CurrentViewState.CurrentRoute = favorite.route;
                viewModel.CurrentViewState.CurrentStop = favorite.stop;
                viewModel.CurrentViewState.CurrentRouteDirection = favorite.routeStops;

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

        private void RecentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FavoritesListBox_SelectionChanged(sender, e);
        }

        private void StopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.CurrentViewState.CurrentRoute = null;
                viewModel.CurrentViewState.CurrentRouteDirection = null;
                viewModel.CurrentViewState.CurrentStop = (Stop)e.AddedItems[0];

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

        private void appbar_search_Click(object sender, EventArgs e)
        {
            if (SearchPanel.Opacity == 0)
            {
                SearchStoryboard.Begin();
                SearchInputBox.Focus();
                SearchInputBox.SelectAll();
            }
            else
            {
                ProcessSearch(SearchInputBox.Text);
            }
        }


        private void SearchInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchStoryboard.Seek(TimeSpan.Zero);
            SearchStoryboard.Stop();
            this.Focus();
        }

        private void SearchByRouteCallback(List<Route> routes, Exception error)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    SearchStoryboard.Seek(TimeSpan.Zero);
                    SearchStoryboard.Stop();
                    this.Focus();
                });

            if (error != null)
            {
                viewModel_ErrorHandler(this, new ViewModel.EventArgs.ErrorHandlerEventArgs(error));
            }
            else if (routes.Count == 0)
            {
                Dispatcher.BeginInvoke(() => MessageBox.Show("No results found"));
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        viewModel.CurrentViewState.CurrentRoutes = routes;
                        NavigationService.Navigate(new Uri("/BusDirectionPage.xaml", UriKind.Relative));
                    });
            }
        }

        private void SearchByStopCallback(List<Stop> stops, Exception error)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    SearchStoryboard.Seek(TimeSpan.Zero);
                    SearchStoryboard.Stop();
                    this.Focus();
                });

            if (error != null)
            {
                viewModel_ErrorHandler(this, new ViewModel.EventArgs.ErrorHandlerEventArgs(error));
            }
            else if (stops.Count == 0)
            {
                MessageBox.Show("No results found");
            }
            else
            {
                viewModel.CurrentViewState.CurrentRoute = null;
                viewModel.CurrentViewState.CurrentRouteDirection = null;
                viewModel.CurrentViewState.CurrentStop = stops[0];
                viewModel.CurrentViewState.CurrentSearchLocation = null;

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

        private void SearchByLocationCallback(LocationForQuery location, Exception error)
        {
            Dispatcher.BeginInvoke(() =>
            {
                SearchStoryboard.Seek(TimeSpan.Zero);
                SearchStoryboard.Stop();
                this.Focus();
            });

            if (error != null)
            {
                viewModel_ErrorHandler(this, new ViewModel.EventArgs.ErrorHandlerEventArgs(error));
            }
            else if (location == null)
            {
                string message = 
                    "Search for a route: 44\r\n" +
                    "Search by stop number: 11132\r\n" + 
                    "Find a landmark: Space Needle\r\n" +
                    "Or an address: 1 Microsoft Way";
                MessageBox.Show(message, "No results found", MessageBoxButton.OK);
            }
            else
            {
                viewModel.CurrentViewState.CurrentRoute = null;
                viewModel.CurrentViewState.CurrentRouteDirection = null;
                viewModel.CurrentViewState.CurrentStop = null;
                viewModel.CurrentViewState.CurrentSearchLocation = location;

                NavigationService.Navigate(new Uri("/StopsMapPage.xaml", UriKind.Relative));
            }
        }

        private void SearchInputBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchString = SearchInputBox.Text;

            if (e.Key == Key.Enter)
            {
                ProcessSearch(searchString);
            }
        }

        private void ProcessSearch(string searchString)
        {
            int routeNumber = 0;

            bool canConvert = int.TryParse(searchString, out routeNumber); //check if it's a number
            if (canConvert == true) //it's a route or stop number
            {
                int number = int.Parse(searchString);
                if (number < 1000) //route number
                {
                    viewModel.SearchByRoute(searchString, SearchByRouteCallback);
                }
                else //stop number
                {
                    viewModel.SearchByStop(searchString, SearchByStopCallback);
                }
            }
            else // Try to find the location
            {
                viewModel.SearchByAddress(searchString, SearchByLocationCallback);
            }

            SearchStoryboard.Seek(TimeSpan.Zero);
            SearchStoryboard.Stop();
        }

        private void appbar_settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void appbar_about_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void stopsMapBtn_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CurrentViewState.CurrentRoute = null;
            viewModel.CurrentViewState.CurrentRouteDirection = null;
            viewModel.CurrentViewState.CurrentSearchLocation = null;
            viewModel.CurrentViewState.CurrentStop = null;

            NavigationService.Navigate(new Uri("/StopsMapPage.xaml", UriKind.Relative));
        }

        private void DirectionButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RouteStops routeStops = (sender as FrameworkElement).DataContext as RouteStops;
            viewModel.CurrentViewState.CurrentRoutes = new List<Route>() { (Route)routeStops.route };

            viewModel.CurrentViewState.CurrentRoute = routeStops.route;
            viewModel.CurrentViewState.CurrentRouteDirection = routeStops;

            viewModel.CurrentViewState.CurrentStop = viewModel.CurrentViewState.CurrentRouteDirection.stops[0];
            foreach (Stop stop in viewModel.CurrentViewState.CurrentRouteDirection.stops)
            {
                // TODO: Make this call location-unknown safe.  The CurrentLocation could be unknown
                // at this point during a tombstoning scenario
                GeoCoordinate location = viewModel.LocationTracker.CurrentLocation;

                if (viewModel.CurrentViewState.CurrentStop.CalculateDistanceInMiles(location) > stop.CalculateDistanceInMiles(location))
                {
                    viewModel.CurrentViewState.CurrentStop = stop;
                }
            }

            NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
        }

    }
}
