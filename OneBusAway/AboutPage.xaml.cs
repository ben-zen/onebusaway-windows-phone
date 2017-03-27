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


using OneBusAway.ViewModel;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace OneBusAway.View
{
  public partial class AboutPage : Page
  {
    public AboutPage()
    {
      InitializeComponent();
      var version = Package.Current.Id.Version;
      VersionTextBlock.Text = "Version " + string.Format("{0}.{1}.{2}.{3}",version.Major, version.Minor, version.Build, version.Revision);

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif
    }

    // Methods overridden for analytics purposes
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
      base.OnNavigatedTo(e);
    }

    // Methods overridden for analytics purposes
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
      base.OnNavigatedFrom(e);
    }
  }
}
