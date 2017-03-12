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
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using OneBusAway.ViewModel.EventArgs;
using System.Diagnostics;
using Windows.Devices.Geolocation;
using Windows.Storage;


namespace OneBusAway.ViewModel
{
  internal static class LocationTrackerStatic
  {
    private static Geolocator locationWatcher;

    public static Geocoordinate LastKnownLocation { get; set; }
    public static PositionStatus LocationStatus
    {
      get
      {
        return locationWatcher.LocationStatus;
      }
    }

    public static event EventHandler<PositionChangedEventArgs> PositionChanged;
    public static event EventHandler<StatusChangedEventArgs> StatusChanged;

    static LocationTrackerStatic()
    {
      LastKnownLocation = null;

      locationWatcher = new Geolocator { DesiredAccuracyInMeters = 0, MovementThreshold = 5 };
      locationWatcher.MovementThreshold = 5; // 5 meters
      locationWatcher.PositionChanged += new TypedEventHandler<PositionChangedEventArgs>(LocationWatcher_PositionChanged);
      locationWatcher.StatusChanged += new TypedEventHandler<StatusChangedEventArgs>(locationWatcher_StatusChanged);
    }

    static void locationWatcher_StatusChanged(object sender, StatusChangedEventArgs e)
    {
      if (StatusChanged != null)
      {
        StatusChanged(sender, e);
      }
    }

    private static void LocationWatcher_PositionChanged(object sender, PositionChangedEventArgs e)
    {
      // The location service will return the last known location of the phone when it first starts up.  Since
      // we can't refresh the home screen wait until a recent location value is found before using it.  The
      // location must be less than 5 minute old.
      if (e.Position.Location.IsUnknown == false)
      {
        if ((DateTime.Now - e.Position.Timestamp.DateTime) < new TimeSpan(0, 5, 0))
        {
          LastKnownLocation = e.Position.Location;
        }
      }

      if (PositionChanged != null)
      {
        PositionChanged(sender, e);
      }
    }
  }

  public class LocationTracker : INotifyPropertyChanged
  {

    #region Private Variables

    private bool locationLoading;
    private Timer methodsRequiringLocationTimer;
    private const int timerIntervalMs = 500;
    private Object methodsRequiringLocationLock;
    private List<RequiresKnownLocation> methodsRequiringLocation;
    private AsyncOperationTracker operationTracker;
#if DEBUG
    private Timer timer = null;
#endif

    #endregion

    #region Constructor & Initializers

    public LocationTracker()
    {
      locationLoading = false;

      // Set up the default action, just execute in the same thread
      UIAction = (uiAction => uiAction());

      methodsRequiringLocation = new List<RequiresKnownLocation>();
      methodsRequiringLocationLock = new Object();
      // Create the timer but don't run it until methods are added to the queue
      methodsRequiringLocationTimer = new Timer(new TimerCallback(RunMethodsRequiringLocation), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Initialize(AsyncOperationTracker operationTracker)
    {
      this.operationTracker = operationTracker;

      //#if DEBUG
      //            if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
      //            {
      //                //LocationTrackerStatic.LastKnownLocation = null;
      //                Random random = new Random();
      //                int timeoutMs = random.Next(0, 5000);
      //                timer = new Timer(param =>
      //                {
      //                    UIAction(() =>
      //                    {
      //                        LocationTrackerStatic.LastKnownLocation = new GeoCoordinate(47.675888, -122.320763);
      //                        GeoPositionChangedEventArgs<GeoCoordinate> args =
      //                            new GeoPositionChangedEventArgs<GeoCoordinate>(new GeoPosition<GeoCoordinate>(DateTime.Now, LocationTrackerStatic.LastKnownLocation));
      //                        LocationWatcher_LocationKnown(
      //                            this,
      //                            args
      //                            );
      //                        locationWatcher_PositionChanged_NotifyPropertyChanged(this, args);
      //                    });
      //                },
      //                    null,
      //                    timeoutMs,
      //                    Timeout.Infinite
      //                    );
      //            }
      //#endif

      LocationTrackerStatic.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(locationWatcher_PositionChanged_NotifyPropertyChanged);

      if (LocationKnown == false)
      {
        if (LocationTrackerStatic.LocationStatus == PositionStatus.Disabled)
        {
          LocationDisabled(true);
        }
        else if (ApplicationData.Current.LocalSettings.Values.ContainsKey("UseLocation") &&
                (bool)IsolatedStorageSettings.ApplicationSettings["UseLocation"] == false)
        {
          LocationDisabled(false);
        }
        else
        {
          operationTracker.WaitForOperation("LoadLocation", "Finding your location...");
          locationLoading = true;
          LocationTrackerStatic.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_LocationKnown);
          LocationTrackerStatic.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(LocationWatcher_StatusChanged);
        }
      }
      else
      {
        locationWatcher_PositionChanged_NotifyPropertyChanged(this, new GeoPositionChangedEventArgs<GeoCoordinate>(new GeoPosition<GeoCoordinate>(DateTime.Now, CurrentLocation)));
      }
    }

    public void Uninitialize()
    {
      LocationTrackerStatic.PositionChanged -= LocationWatcher_LocationKnown;
      LocationTrackerStatic.StatusChanged -= LocationWatcher_StatusChanged;
      LocationTrackerStatic.PositionChanged -= locationWatcher_PositionChanged_NotifyPropertyChanged;
    }

    #endregion

    #region Public Members

    public Action<Action> UIAction { get; set; }
    public event EventHandler<ErrorHandlerEventArgs> ErrorHandler;

    public Geocoordinate CurrentLocation
    {
      get
      {
#if !DEBUG
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    //return new GeoCoordinate(47.645181, -122.140825); // Micorosft Studios
                    return new GeoCoordinate(47.675888, -122.320763); // Greenlake P&R
                    //return new GeoCoordinate(30.266, -97.742); // Austin, TX
                }
#endif

        if (LocationTrackerStatic.LastKnownLocation != null)
        {
          return LocationTrackerStatic.LastKnownLocation;
        }

        throw new LocationUnavailableException("The location is currently unavailable: " + LocationTrackerStatic.LocationStatus, LocationTrackerStatic.LocationStatus);
      }
    }

    public bool LocationKnown
    {
      get
      {
#if !DEBUG
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return true;
                }
#endif

        return LocationTrackerStatic.LastKnownLocation != null;
      }
    }

    /// <summary>
    /// Returns a default location to use when our current location is
    /// unavailable.  This is downtown Seattle.
    /// </summary>
    public Geocoordinate DefaultLocation
    {
      get
      {
        return new Geocoordinate(47.60621, -122.332071);
      }
    }

    public Geocoordinate CurrentLocationSafe
    {
      get
      {
        if (LocationKnown == true)
        {
          return CurrentLocation;
        }
        else
        {
          return DefaultLocation;
        }
      }
    }

    #endregion

    #region Private Methods

    private void LocationWatcher_StatusChanged(object sender, StatusChangedEventArgs e)
    {
      if (e.Status == PositionStatus.Disabled)
      {
        LocationDisabled(true);
      }
    }

    private void LocationDisabled(bool systemServiceDisabled)
    {
      // Status disabled means the user has disabled the location service on their phone
      // and we won't be getting a location.  Go ahead and stop loading the location and
      // set it to the default
      if (locationLoading == true)
      {
        locationLoading = false;
        operationTracker.DoneWithOperation("LoadLocation");
      }

      LocationTrackerStatic.LastKnownLocation = DefaultLocation;
      OnPropertyChanged("CurrentLocation");
      OnPropertyChanged("CurrentLocationSafe");
      OnPropertyChanged("LocationKnown");

      // Let them know OneBusAway is pretty useless without location
      string errorMessage;
      if (systemServiceDisabled == true)
      {
        errorMessage =
            "We couldn't find your location, " +
            "make sure your location services are turned on in the phone's settings. " +
            "OneBusAway will default to downtown Seattle.";
      }
      else
      {
        errorMessage =
            "We couldn't find your location because you have disabled OneBusAway's location access " +
            "in the settings menu. OneBusAway will default to downtown Seattle.";
      }

      ErrorOccured(this, new LocationUnavailableException(errorMessage, LocationTrackerStatic.LocationStatus));
    }

    private void LocationWatcher_LocationKnown(object sender, PositionChangedEventArgs e)
    {
      if (e.Position.IsUnknown == false)
      {
        if (locationLoading == true)
        {
          // We know where we are now, decrease the pending count
          locationLoading = false;
          operationTracker.DoneWithOperation("LoadLocation");

          // Remove this handler now that the location is known
          LocationTrackerStatic.PositionChanged -= new EventHandler<PositionChangedEventArgs>(LocationWatcher_LocationKnown);
        }
      }
    }

    private void locationWatcher_PositionChanged_NotifyPropertyChanged(object sender, PositionChangedEventArgs e)
    {
      if (e.Position.Location.IsUnknown == false)
      {
        OnPropertyChanged("CurrentLocation");
        OnPropertyChanged("CurrentLocationSafe");
        OnPropertyChanged("LocationKnown");
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
      {
        UIAction(() =>
        {
                  // Check again in case it has changed while we waited to execute on the UI thread
                  if (PropertyChanged != null)
          {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
          }
        }
        );
      }
    }

    protected void ErrorOccured(object sender, Exception e)
    {
      Debug.Assert(false);

      // The VM should always be subscribed to the ErrorHandler event
      Debug.Assert(ErrorHandler != null);

      if (ErrorHandler != null)
      {
        ErrorHandler(sender, new ErrorHandlerEventArgs(e));
      }
    }

    #endregion

    #region Run Requiring Location methods

    private void RunMethodsRequiringLocation(object param)
    {
      if (LocationKnown == true)
      {
        lock (methodsRequiringLocationLock)
        {
          Exception error = null;
          foreach (RequiresKnownLocation method in methodsRequiringLocation)
          {
            try
            {
              method(CurrentLocation);
            }
            catch (Exception e)
            {
              Debug.Assert(false);

              // Queue up errors so that all methods will be executed the and list will be 
              // cleared even if exceptions occur.  If there is more than one error, just report the last one
              error = e;
            }
          }

          methodsRequiringLocation.Clear();
          // Disable the timer now that no methods are in the queue
          methodsRequiringLocationTimer.Change(Timeout.Infinite, Timeout.Infinite);

          if (error != null)
          {
            throw error;
          }
        }
      }
    }

    public delegate void RequiresKnownLocation(Geocoordinate location);
    public void RunWhenLocationKnown(RequiresKnownLocation method)
    {
      if (LocationKnown == true)
      {
        method(CurrentLocation);
      }
      else
      {
        lock (methodsRequiringLocationLock)
        {
          methodsRequiringLocation.Add(method);
          methodsRequiringLocationTimer.Change(timerIntervalMs, timerIntervalMs);
        }
      }
    }

    #endregion

  }

  public class LocationUnavailableException : Exception
  {
    public PositionStatus Status { get; private set; }

    public LocationUnavailableException(string message, PositionStatus status)
        : base(message)
    {
      Status = status;
    }

    public override string ToString()
    {
      return string.Format(
          "{0} \r\n" +
          "LocationStatus: {1}",
          base.ToString(),
          Status
          );
    }
  }
}
