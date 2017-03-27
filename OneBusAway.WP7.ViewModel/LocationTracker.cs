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
using System.Threading.Tasks;
using OneBusAway.ViewModel.EventArgs;
using System.Diagnostics;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage;


namespace OneBusAway.ViewModel
{
  public class LocationTracker : INotifyPropertyChanged
  {

    #region Private Variables

    private bool locationLoading;

    private Geolocator Locator { get; set; }
    private Geocoordinate LocationInternal { get; set; }
    private bool hasInitialized;

    private static object m_trackerLock = new object();
    private static LocationTracker m_tracker = null;


    #endregion

    #region Constructor & Initializers

    private LocationTracker()
    {
      locationLoading = false;
    }

    /// <summary>
    /// Initializes the locator service, connecting to events and caching the internal Locator object.
    /// </summary>
    /// <returns>If the locator service is allowed and active, returns true.</returns>
    private async Task<bool> Initialize()
    {
      var result = false;
      if (hasInitialized)
      {
        result = (AccessStatus == GeolocationAccessStatus.Allowed);
      }
      else
      {
        var accessStatus = await Geolocator.RequestAccessAsync();

        switch (accessStatus)
        {
          case GeolocationAccessStatus.Allowed:
            Locator = new Geolocator { };
            Locator.PositionChanged += new TypedEventHandler<Geolocator, PositionChangedEventArgs>(locationWatcher_PositionChanged_NotifyPropertyChanged);
            Locator.StatusChanged += new TypedEventHandler<Geolocator, StatusChangedEventArgs>(LocationWatcher_StatusChanged);
            result = true;
            break;
          case GeolocationAccessStatus.Denied:
            break;
          case GeolocationAccessStatus.Unspecified:
            break;
        }
        AccessStatus = accessStatus;


        if (LocationKnown == false)
        {
          if (Locator.LocationStatus == PositionStatus.Disabled)
          {
            LocationDisabled(true);
          }
          else
          {
            locationLoading = true;
            // var position = await Locator.GetGeopositionAsync();

          }
        }

        hasInitialized = true;
      }
      return result;
    }

    public async Task<Geopoint> GetLocationAsync()
    {
      if (!await Initialize())
      {
        throw new Exception("Location not allowed.");
      }
      return (await Locator.GetGeopositionAsync()).Coordinate.Point;
    }

    #endregion

    #region Public Members

    public static LocationTracker Tracker
    {
      get
      {
        if (m_tracker == null)
        {
          lock (m_trackerLock)
          {
            // If we somehow have two threads try to create this object at the same time, one of them got here first and created the tracker. Don't overwrite the object.
            if (m_tracker == null)
            {
              m_tracker = new LocationTracker();
            }
          }
        }
        return m_tracker;
      }
    }

    public GeolocationAccessStatus AccessStatus { get; private set; }

    public PositionStatus PositionStatus { get; private set; }

    public bool LocatorAllowed
    {
      get
      {
        return AccessStatus == GeolocationAccessStatus.Allowed;
      }
    }

    public event EventHandler<ErrorHandlerEventArgs> ErrorHandler;

    public Geopoint CurrentLocation
    {
      get
      {
        return LocationInternal?.Point;
      }
    }



    public bool LocationKnown
    {
      get
      {
        return LocationInternal != null;
      }
    }

    /// <summary>
    /// Returns a default location to use when our current location is
    /// unavailable.  This is downtown Seattle.
    /// </summary>
    public Geopoint DefaultLocation
    {
      get
      {
        return new Geopoint(new BasicGeoposition { Latitude = 47.60621, Longitude = -122.332071 });
      }
    }

    #endregion

    #region Private Methods

    private void LocationWatcher_StatusChanged(object sender, StatusChangedEventArgs e)
    {
      PositionStatus = e.Status;
      switch (e.Status)
      {
        case PositionStatus.NotAvailable:
        case PositionStatus.Disabled:
          LocationDisabled(true);
          break;
      }
    }

    private void LocationDisabled(bool systemServiceDisabled)
    {
      // Status disabled means the user has disabled the location service on their phone
      // and we won't be getting a location.  Go ahead and stop loading the location and
      // set it to the default
      locationLoading = false;

      OnPropertyChanged("CurrentLocation");
      OnPropertyChanged("CurrentLocationSafe");
      OnPropertyChanged("LocationKnown");

      // Let them know OneBusAway is pretty useless without location
      string errorMessage;
      if (systemServiceDisabled)
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

      ErrorOccured(this, new LocationUnavailableException(errorMessage, PositionStatus));
    }

    private void locationWatcher_PositionChanged_NotifyPropertyChanged(Geolocator sender, PositionChangedEventArgs e)
    {

      OnPropertyChanged("CurrentLocation");
      OnPropertyChanged("CurrentLocationSafe");
      OnPropertyChanged("LocationKnown");
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
