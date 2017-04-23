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
using System.Collections.ObjectModel;
using Windows.Storage;

namespace OneBusAway.ViewModel
{
  public enum MainPagePivots : int
  {
    LastUsed = -100,
    Routes = 0,
    Stops = 1,
    Recents = 2,
    Favorites = 3
  };

  public class SettingsVM : AViewModel
  {

    #region Constructors
    public SettingsVM()
        : base()
    {
    }

    public SettingsVM(BusServiceModel busServiceModel, AppDataModel appDataModel)
        : base(busServiceModel, appDataModel)
    {
    }

    #endregion

    public bool FeedbackEnabled
    {
      get
      {
        var settings = ApplicationData.Current.LocalSettings;
        if (settings.Values.ContainsKey("FeedbackEnabled"))
        {
          return (bool)settings.Values["FeedbackEnabled"];
        }
        else
        {
          // Default to true if no user setting exists
          return true;
        }
      }

      set
      {
        ApplicationData.Current.LocalSettings.Values["FeedbackEnabled"] = value;
        OnPropertyChanged("FeedbackEnabled");
      }
    }

    public bool UseLocation
    {
      get
      {
        var settings = ApplicationData.Current.LocalSettings;
        if (settings.Values.ContainsKey("UseLocation"))
        {
          return (bool)settings.Values["UseLocation"];
        }
        else
        {
          // Default to true if no user setting exists
          return true;
        }
      }

      set
      {
        ApplicationData.Current.LocalSettings.Values["UseLocation"] = value;
        OnPropertyChanged("UseLocation");
      }
    }

    public bool UseNativeTheme
    {
      get
      {
        object theme;
        var settings = ApplicationData.Current.LocalSettings;
        if (!settings.Values.TryGetValue("Theme", out theme))
        {
          return false; // defaults to the OBA theme
        }
        return ((string)theme == "Native");
      }
      set
      {
        ApplicationData.Current.LocalSettings.Values["Theme"] = (value) ? "Native" : "OBA";
        OnPropertyChanged("UseNativeTheme");
      }
    }

    public ObservableCollection<MainPagePivots> MainPagePivotOptions
    {
      get
      {
        // Enum.GetValues() isn't supported, so guess I have to hardcode this
        ObservableCollection<MainPagePivots> list = new ObservableCollection<MainPagePivots>()
                {
                    MainPagePivots.LastUsed,
                    MainPagePivots.Routes,
                    MainPagePivots.Stops,
                    MainPagePivots.Favorites,
                    MainPagePivots.Recents
                };

        return list;
      }
    }

    public MainPagePivots SelectedMainPagePivot
    {
      get
      {
        var settings = ApplicationData.Current.LocalSettings;
        if (settings.Values.ContainsKey("DefaultMainPagePivot"))
        {
          return (MainPagePivots)settings.Values["DefaultMainPagePivot"];
        }
        else
        {
          // Default to LastUsed
          return MainPagePivots.LastUsed;
        }
      }

      set
      {
        ApplicationData.Current.LocalSettings.Values["DefaultMainPagePivot"] = value;
        OnPropertyChanged("SelectedPageOption");
      }
    }

    public void Clear()
    {
      // Needs to clear the recents from the data model.
      this.BusServiceModel.ClearCache();
    }
  }
}
