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
using System.Linq;
using OneBusAway.ViewModel;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;

namespace OneBusAway.View
{
  public partial class SettingsPage : Page
  {
    public SettingsVM VM => SettingsVM.Instance;

    public SettingsPage()
        : base()
    {
      InitializeComponent();

      this.Loaded += new RoutedEventHandler(SettingsPage_Loaded);
    }

    void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
      // Add the event handlers here instead of in XAML so they aren't called when the initial
      // selection is made on page load
      DefaultPivotLp.SelectionChanged += new SelectionChangedEventHandler(DefaultPivotLp_SelectionChanged);
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

    private void appbar_clear_history_Click(object sender, RoutedEventArgs e)
    {
      VM.Clear();
    }

    // Created for analytics
    private Dictionary<string, string> NewDefaultPivot
    {
      get
      {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("NewDefaultPivot", DefaultPivotLp.SelectedItem.ToString());
        return data;
      }
    }

    // Created for analytics
    private void DefaultPivotLp_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    // Created for analytics
    private void ReportUsageTs_Click(object sender, RoutedEventArgs e)
    {

    }

    async void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
      var popup = new MessageDialog("You will need to restart OneBusAway for this change to take effect");
      await popup.ShowAsync();
    }

    async void UseLocationTs_Click(object sender, RoutedEventArgs e)
    {
      var popup = new MessageDialog("You will need to restart OneBusAway for this change to take effect");
      await popup.ShowAsync();
    }
  }
}