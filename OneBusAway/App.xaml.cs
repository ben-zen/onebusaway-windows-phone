﻿/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
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
using OneBusAway.Model.BusServiceDataStructures;
using OneBusAway.Model.LocationServiceDataStructures;
using OneBusAway.ViewModel;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


namespace OneBusAway.View
{
  public partial class App : Application
  {
    private bool previouslyLaunched = false; // On Phone, any time the app is launched from the tile or the app list, OnLaunched is called. This helps determine when the app was already running.
    private ViewState viewState = ViewState.Instance;
    private bool failureEncountered = false;

    #region Properties
    public Frame RootFrame { get; private set; }

    private BusDirectionVM _busDirection = null;
    public BusDirectionVM BusDirection
    {
      get
      {
        if (_busDirection == null)
        {
          _busDirection = new BusDirectionVM();
        }
        return _busDirection;
      }
    }

    public RouteListVM RouteList { get; } = new RouteListVM();

    #endregion
    public App()
    {
      InitializeComponent();
      UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionEncountered);
      Suspending += OnSuspending;
    }



    // Code to execute when the application is activated (brought to foreground)
    // This code will not execute when the application is first launched
    void Application_Activated(object sender, object e)
    {
      //if (e.IsApplicationInstancePreserved == false)
      {
        viewState.CurrentRoute = GetStateHelper<Route>("CurrentRoute");
        viewState.CurrentRoutes = GetStateHelper<List<Route>>("CurrentRoutes");
        viewState.CurrentRouteDirection = GetStateHelper<RouteStops>("CurrentRouteDirection");
        viewState.CurrentStop = GetStateHelper<Stop>("CurrentStop");
        viewState.CurrentSearchLocation = GetStateHelper<LocationForQuery>("CurrentSearchLocation");
      }
    }

    private T GetStateHelper<T>(string key)
    {
      return default(T);
    }

    // Code to execute when the application is deactivated (sent to background)
    // This code will not execute when the application is closing
    void Application_Deactivated(object sender, object e)
    {
      BusServiceModel.Singleton.SaveCache();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
      Frame rootFrame = Window.Current.Content as Frame;
      // Ne répétez pas l'initialisation de l'application lorsque la fenêtre comporte déjà du contenu,
      // assurez-vous juste que la fenêtre est active
      if (rootFrame == null)
      {
        // Créez un Frame utilisable comme contexte de navigation et naviguez jusqu'à la première page
        rootFrame = new Frame();
        rootFrame.CacheSize = 3;

        rootFrame.NavigationFailed += OnNavigationFailed;

        if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
        {
          //TODO: chargez l'état de l'application précédemment suspendue
        }

        // Placez le frame dans la fenêtre active
        Window.Current.Content = rootFrame;
        SystemNavigationManager.GetForCurrentView().BackRequested += HandleBackPressed;
      }

      if (e.PrelaunchActivated == false)
      {
        if (rootFrame.Content == null)
        {
          // Quand la pile de navigation n'est pas restaurée, accédez à la première page,
          // puis configurez la nouvelle page en transmettant les informations requises en tant que
          // paramètre
          rootFrame.Navigate(typeof(MainPage), e.Arguments);
        }
        // Vérifiez que la fenêtre actuelle est active
        Window.Current.Activate();
      }
    }

    private void HandleBackPressed(object sender, BackRequestedEventArgs e)
    {
      var canGoBack = (Window.Current?.Content as Frame)?.CanGoBack;
      if (canGoBack != null && (bool)canGoBack)
      {
        (Window.Current.Content as Frame).GoBack();
        e.Handled = true;
      }
    }

    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
      var deferral = e.SuspendingOperation.GetDeferral();
      //TODO: enregistrez l'état de l'application et arrêtez toute activité en arrière-plan
      deferral.Complete();
    }

    // Code to execute if a navigation fails
    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
      if (System.Diagnostics.Debugger.IsAttached)
      {
        // A navigation has failed; break into the debugger
        System.Diagnostics.Debugger.Break();
      }
    }

    private void UnhandledExceptionEncountered(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.Exception is ServiceRegionUnavailableException)
      {
        var regionUnavailable = e.Exception as ServiceRegionUnavailableException;
        // Check if we've already alerted the user that this region is inaccessible.
        if (!failureEncountered)
        {
          failureEncountered = true;
          var errorDialog = new ErrorDialog();
        }
        e.Handled = true;
      }
    }
  }
}
