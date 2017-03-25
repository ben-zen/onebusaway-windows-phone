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
using Windows.Devices.Geolocation;
using Windows.Storage;

namespace OneBusAway.ViewModel
{
    public abstract class AViewModel : INotifyPropertyChanged
    {
        #region Constructors
        public AViewModel(BusServiceModel busServiceModel = null, IAppDataModel appDataModel = null, ILocationModel locationModel = null)
        {
            this._busServiceModel = busServiceModel;
            this.lazyAppDataModel = appDataModel;

	        if (!IsInDesignMode)
	        {
		        operationTracker = new AsyncOperationTracker();
	        }

	        // Set up the default action, just execute in the same thread
            UIAction = (uiAction => uiAction());
            
            eventsRegistered = false;
        }

        #endregion

        #region Private/Protected Properties

        // Always use the same search radius so the cache will be much
        // more efficient.  500 m is small enough that we almost never
        // exceed the 100 stop limit, even downtown.
        protected int defaultSearchRadius = 500;

        private BusServiceModel _busServiceModel;
        protected BusServiceModel BusServiceModel
        {
            get
            {
                if (_busServiceModel == null)
                {
                    _busServiceModel = BusServiceModel.Singleton;
                    _busServiceModel.Initialize();
                }
                return _busServiceModel;
            }
        }
 
 	 	private IAppDataModel lazyAppDataModel;
 	 	protected IAppDataModel appDataModel
        {
            get
            {
                if (lazyAppDataModel == null)
                {
                    lazyAppDataModel = (IAppDataModel)Assembly.Load(new AssemblyName("OneBusAway.WP7.Model"))
                        .GetType("OneBusAway.WP7.Model.AppDataModel")
                        .GetField("Singleton")
                        .GetValue(null);
                }
                return lazyAppDataModel;
            }
        }

        private ILocationModel lazyLocationModel;
        protected ILocationModel locationModel
        {
            get
            {
                if (lazyLocationModel == null)
                {
                    lazyLocationModel = (ILocationModel)Assembly.Load(new AssemblyName("OneBusAway.WP7.Model"))
                        .GetType("OneBusAway.WP7.Model.LocationModel")
                        .GetField("Singleton")
                        .GetValue(null);
                }
                return lazyLocationModel;
            }
        }

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

        private bool eventsRegistered;

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

        void locationTracker_ErrorHandler(object sender, ErrorHandlerEventArgs e)
        {
            ErrorOccured(this, e.error);
        }

        #endregion

        #region Public Members

        private Action<Action> uiAction;
        public Action<Action> UIAction
        {
            get { return uiAction; }

            set
            {
                uiAction = value;

                // Set the ViewState's UIAction
                CurrentViewState.UIAction = uiAction;
                operationTracker.UIAction = uiAction;
            }
        }

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
				catch (Exception ex)
				{
					// Toss out any errors we get
				}
				// If we get here that means we got an error
				isInDesignModeStatic = true;
				return isInDesignModeStatic.Value;
			}
		}


        /// <summary>
        /// Registers all event handlers with the model.  Call this when 
        /// the page is first loaded.
        /// </summary>
        public virtual void RegisterEventHandlers(Windows.UI.Core.CoreDispatcher dispatcher)
        {
            // Set the UI Actions to occur on the UI thread
            UIAction = (uiAction => dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => uiAction()));
            Debug.Assert(eventsRegistered == false);
            eventsRegistered = true;
        }

        /// <summary>
        /// Unregisters all event handlers with the model. Call this when
        /// the page is navigated away from.
        /// </summary>
        public virtual void UnregisterEventHandlers()
        {
            Debug.Assert(eventsRegistered == true);
            eventsRegistered = false;
        }

        #endregion

    }
}
