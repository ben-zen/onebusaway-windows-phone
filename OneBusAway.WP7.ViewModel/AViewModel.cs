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
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;
using OneBusAway.Model;
using OneBusAway.ViewModel.EventArgs;
using Windows.Storage;

namespace OneBusAway.ViewModel
{
  public abstract class AViewModel : INotifyPropertyChanged
  {
    #region Constructors
    public AViewModel(BusServiceModel busServiceModel = null, IAppDataModel appDataModel = null, ILocationModel locationModel = null)
    {

      if (!IsInDesignMode)
      {
        operationTracker = new AsyncOperationTracker();
      }
    }

    #endregion

    #region Private/Protected Properties

    // Always use the same search radius so the cache will be much
    // more efficient.  500 m is small enough that we almost never
    // exceed the 100 stop limit, even downtown.
    protected int defaultSearchRadius = 500;

    protected BusServiceModel BusServiceModel => BusServiceModel.Singleton;

    protected AppDataModel appDataModel => AppDataModel.Singleton;

    protected LocationModel locationModel => LocationModel.Singleton;

    protected bool IsInDesignMode
    {
      get
      {
        return IsInDesignModeStatic;
      }
    }

    /// <summary>
    /// Subclasses should queue and dequeue their async calls onto this object to tie into the Loading property.
    /// </summary>
    public AsyncOperationTracker operationTracker { get; private set; }

    #endregion

    #region Private/Protected Methods

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

    protected void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    void locationTracker_ErrorHandler(object sender, ErrorHandlerEventArgs e)
    {
      ErrorOccured(this, e.error);
    }

    #endregion

    #region Public Members

    public event EventHandler<ErrorHandlerEventArgs> ErrorHandler;

    public const string FeedbackEmailAddress = "wp7@onebusaway.org";

    public ViewState CurrentViewState
    {
      get
      {
        return ViewState.Instance;
      }
    }

    public LocationTracker LocationTracker
    {
      get
      {
        return LocationTracker.Tracker;
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static bool? isInDesignModeStatic = null;
    public static bool IsInDesignModeStatic // Convenient method that can be accessed out of an inherited class
    {
      get
      {
        if (isInDesignModeStatic.HasValue)
        {
          // only do the check once and use the last value forever
          return isInDesignModeStatic.Value;
        }
        try
        {
          var isoStor = ApplicationData.Current.LocalSettings.Values.ContainsKey("asasdasd");
          isInDesignModeStatic = false;
          return isInDesignModeStatic.Value;
        }
        catch (Exception)
        {
          // Toss out any errors we get
        }
        // If we get here that means we got an error
        isInDesignModeStatic = true;
        return isInDesignModeStatic.Value;
      }
    }


    #endregion

  }
}
