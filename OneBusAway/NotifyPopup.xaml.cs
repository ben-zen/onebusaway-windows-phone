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
using Windows.UI.Xaml.Controls;

namespace OneBusAway.View
{
  public partial class NotifyPopup : UserControl
  {
    #region Events

    public event EventHandler<NotifyEventArgs> Notify_Completed;
    #endregion

    public NotifyPopup()
    {
      InitializeComponent();
    }


    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      Notify_Completed?.Invoke(this, new NotifyEventArgs(null, false, 0));

      Dismiss();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      if (Notify_Completed != null)
      {
        int numMinutes = TimePicker.SelectedIndex * 5 + 5;
        Notify_Completed(this, new NotifyEventArgs(null, true, numMinutes));
      }

      Dismiss();
    }

  }

  public class NotifyEventArgs : System.EventArgs
  {
    public int minutes { get; private set; }
    public bool okSelected { get; private set; }

    public NotifyEventArgs(Exception error, bool okSelected, int minutes)
    {
      this.minutes = minutes;
      this.okSelected = okSelected;
    }
  }
}
