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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;

// TODO this file should probably live in the Model.
// Plumbing through dependencies was getting tricky...

namespace OneBusAway.ViewModel
{
  /// <summary>
  /// The bit field flags that hold what type of request to make to the Tile web service
  /// </summary>
  /// <remarks>This is not the values used by the notification service.</remarks>
  [Flags]
  public enum NotificationFlagType
  {
    Tile = 1,
    Toast = 2,
    Raw = 4
  }

  /// <summary>
  /// The imagery set used for the tile
  /// </summary>
  /// <remarks>These are not the values used by the notification service.</remarks>
  public enum ImagerySet
  {
    /// <summary>
    /// No imagery set was defined
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Aerial imagery
    /// </summary>
    Aerial = 1,

    /// <summary>
    /// Aerial imagery with a road overlay
    /// </summary>
    AerialWithLabels = 2,

    /// <summary>
    /// Roads without additional imagery
    /// </summary>
    Road = 3
  }

  /// <summary>
  /// A client for a transit tile notification service
  /// </summary>
  public class TripService
  {
    //private IsolatedStorageSettings UserSettings = IsolatedStorageSettings.ApplicationSettings;

    private const string ChannelName = "BusStopLightChannel";
    private const string ServiceName = "api.busstoplight.com";
    private const string DefaultBaseAddress = "http://api.BusStopLight.com/TripService.svc/";

    /// <summary>
    /// True if we are subscribed to Raw notifications
    /// </summary>
    private bool IsRawBound;

    /// <summary>
    /// The active channel to the notification service
    /// </summary>
    private PushNotificationChannel NotificationChannel;

    /// <summary>
    /// The default server base address
    /// </summary>
    private Uri BaseAddress = new Uri(DefaultBaseAddress);

    /// <summary>
    /// The bit field flags that hold the current request types
    /// </summary>
    /// <remarks>This is not the values used by the notification service.</remarks>
    private NotificationFlagType NotificationFlag;

    /// <summary>
    /// The type of imagery to use
    /// </summary>
    private ImagerySet Imagery;

    /// <summary>
    /// The notification data returned from the server that drives the UI
    /// </summary>
    private NotificationData NotifyData;

    #region Constructor and Initialization
    public TripService(NotificationFlagType notificationTypes)
    {
      NotifyData = new NotificationData();

      NotificationFlag = notificationTypes;

      IsRawBound = false;

      Imagery = ImagerySet.Unknown;
    }

    public async Task<bool> Initialize()
    {
      var result = false;
      var channel = await EstablishChannelAsync();
      if (channel != null)
      {
        NotificationChannel = channel;
        result = true;
      }
      return result;
    }
    #endregion
 
    #region TestUI

    public async Task<bool> StartSubscriptionAsync(string stopId, string tripId, int minutes)
    {
      //
      // Do not allow a Start to occur if there is not an active channel to the notification service
      //

      if (NotificationChannel == null || NotificationChannel.Uri == null)
      {
        UpdateStatus("Channel not open!");
        return false;
      }

      //
      // Clear out any previous results
      //

      NotifyData.Text1 = null;
      NotifyData.Text2 = null;
      NotifyData.Count = 0;
      NotifyData.Title = null;
      NotifyData.BackgroundUri = null;

      UpdateStatus("Registering...");
      return await SubscribeToTripService(BaseAddress,
                                          stopId,
                                          tripId,
                                          minutes,
                                          Imagery,
                                          new Uri(NotificationChannel.Uri));
    }


    /// <summary>
    /// When Raw is checked, request Raw notifications
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void RawCheckBox_Checked()
    {
      //
      // If there is no active channel, don't allow the check to occur, provides
      // positive feedback that it is not yet ready.
      //

      if (NotificationChannel.Uri == null)
      {
        NotificationFlag &= ~NotificationFlagType.Raw;

        UpdateStatus("Channel not open!");
        return;
      }

      //
      // Add Raw to the Tile service Uri and subscribe to Raw eents
      //

      NotificationFlag |= NotificationFlagType.Raw;
      SubscribeToRawNotifications();
    }

    /// <summary>
    /// Clear the Raw notification request
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void RawCheckBox_Unchecked()
    {
      NotificationFlag &= ~NotificationFlagType.Raw;

      UnSubscribeToRawNotifications();
    }

    /// <summary>
    /// When Toast is checked, request Toast notifications
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void ToastCheckBox_Checked()
    {
      //
      // If there is no active channel, don't allow the check to occur, provides
      // positive feedback that it is not yet ready.
      //

      if (NotificationChannel.Uri == null)
      {
        NotificationFlag &= ~NotificationFlagType.Toast;

        UpdateStatus("Channel not open!");
        return;
      }

      //
      // Add Toast to the Tile service Uri and subscribe to Toast eents
      //

      NotificationFlag |= NotificationFlagType.Toast;
    }

    /// <summary>
    /// Clear the Toast notification request
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void ToastCheckBox_Unchecked()
    {
      NotificationFlag &= ~NotificationFlagType.Toast;
    }

    /// <summary>
    /// When Tile is checked, request Tile notifications
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void TileCheckBox_Checked()
    {
      if (NotificationChannel.Uri == null)
      {
        NotificationFlag &= ~NotificationFlagType.Tile;

        UpdateStatus("Channel not open!");
        return;
      }

      NotificationFlag |= NotificationFlagType.Tile;
    }

    /// <summary>
    /// Clear the Tile notification request
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void TileCheckBox_Unchecked()
    {
      NotificationFlag &= ~NotificationFlagType.Tile;
    }

    /// <summary>
    /// Set the Aerial imagery set
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    /// <remarks>Only one imagery checkbox can be set</remarks>
    public void AerialCheckBox_Checked()
    {
      Imagery = ImagerySet.Aerial;
    }

    /// <summary>
    /// Clear the Aerial imagery set
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void AerialCheckBox_Unchecked()
    {
      if (Imagery == ImagerySet.Aerial)
        Imagery = ImagerySet.Unknown;
    }

    /// <summary>
    /// Set the AerialWithLabels imagery set
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    /// <remarks>Only one imagery checkbox can be set</remarks>
    public void LabelsCheckBox_Checked()
    {
      Imagery = ImagerySet.AerialWithLabels;
    }

    /// <summary>
    /// Clear the AerialWithLabels imagery set
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void LabelsCheckBox_Unchecked()
    {
      if (Imagery == ImagerySet.AerialWithLabels)
        Imagery = ImagerySet.Unknown;
    }

    /// <summary>
    /// Set the Road imagery set
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    /// <remarks>Only one imagery checkbox can be set</remarks>
    public void RoadCheckBox_Checked()
    {
      Imagery = ImagerySet.Road;
    }

    /// <summary>
    /// Clear the Road imagery set
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    public void RoadCheckBox_Unchecked()
    {
      if (Imagery == ImagerySet.Road)
        Imagery = ImagerySet.Unknown;
    }

    /// <summary>
    /// A simple helper utility to update the status textblock, used mostly in delegates
    /// </summary>
    /// <param name="message">The message to display</param>
    private void UpdateStatus(string message)
    {
      Trace("UpdateStatus: " + message);
    }

    #endregion

    /// <summary>
    /// A general DEBUG tracing utility
    /// </summary>
    /// <param name="message">The trace message to display</param>
    [Conditional("DEBUG")]
    private void Trace(string message)
    {
      Debug.WriteLine(message);
    }

    /// <summary>
    /// Establish a channel to the notification service and register for events.
    /// </summary>
    /// <returns>The notification channel</returns>
    /// <remarks>The channel may not be active on return.</remarks>
    private async Task<PushNotificationChannel> EstablishChannelAsync()
    {
      PushNotificationChannel httpChannel = null;

      try
      {
        var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
        channel.PushNotificationReceived += new Windows.Foundation.TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs>(HandlePushNotificationReceived);
      }
      catch (Exception ex)
      {
        UpdateStatus("Channel error: " + ex.Message);
      }

      return httpChannel;
    }

     /// <summary>
    /// Contact the Tile web service to subscribe to a notification
    /// </summary>
    /// <param name="baseAddress">The base Uri of the service</param>
    /// <param name="stopId">The stop to notify on arrival</param>
    /// <param name="tripId">The trip to track</param>
    /// <param name="notify">The number of minutes before arrival to trigger a notification</param>
    /// <param name="imagery">The imagery set to use</param>
    /// <param name="subscription">The Uri for the channel from the notification service</param>
    private async Task<bool> SubscribeToTripService(Uri baseAddress, string stopId, string tripId, int notify, ImagerySet imagery, Uri subscription)
    {
      //
      // The Transit Tile Service REST request format (Note: "unregister" also exists with same format)
      //

      var UriFormat = "stop/{0}/register?trip={1}&notify={2}&type={3}&imageryset={4}&uri={5}";

      var callUri = String.Format(UriFormat, stopId, tripId, notify.ToString(), NotificationFlag.ToString().Replace(" ", ""),
          imagery, subscription.AbsoluteUri);

      var client = new HttpClient();
      var response = await client.GetAsync(new Uri(baseAddress, callUri));
      return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Contact the Tile web service to send a Background notification.
    /// </summary>
    /// <remarks>
    /// Returns a Windows Phone Tile with no Count or Title with the local Uri “Background.png”. This is the normal default application Tile image name.
    /// </remarks>
    /// <param name="baseAddress">The base Uri of the service</param>
    /// <param name="subscription">The Uri for the channel from the notification service</param>
    private async Task<bool> BackgroundTripService(Uri baseAddress, Uri subscription)
    {
      //
      // The Transit Tile Service REST request background format
      //

      var UriFormat = "background?uri={0}";

      var callUri = String.Format(UriFormat, subscription.AbsoluteUri);

      var client = new HttpClient();
      var response = await client.GetAsync(new Uri(baseAddress, callUri));
      return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Subscribe to raw notifications
    /// </summary>
    /// <param name="channel">The active notification channel</param>
    private void SubscribeToRawNotifications()
    {
      //
      // Bind to Raw Notification 
      //

      if (IsRawBound == true)
      {
        Trace("Already bounded (register) to Raw Notifications");
        return;
      }

      Trace("Registering to Raw Notifications");

      IsRawBound = true;
    }

    /// <summary>
    /// Unsubscribe to raw notifications
    /// </summary>
    /// <param name="channel">The active notification channel</param>
    private void UnSubscribeToRawNotifications()
    {
      //
      // UnBind to Raw Notification 
      //

      if (IsRawBound == false)
      {
        Trace("Already not bound to Raw Notifications");
        return;
      }

      Trace("Unbinding to Raw Notifications");

      IsRawBound = false;
    }

    /// <summary>
    /// Event signaled when a toast notification arrives
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    private void HandleToastNotificationReceived(PushNotificationChannel sender, ToastNotification notification)
    {
      Trace("===============================================");
      Trace("Toast notification arrived:");

      if ((NotificationFlag & NotificationFlagType.Toast) != NotificationFlagType.Toast)
      {
        Trace("Toast notifications not active");
        Trace("===============================================");
        return;
      }

      ToastNotificationManager.CreateToastNotifier().Show(notification);
    }

    /// <summary>
    /// Event signaled when a Raw notification arrives
    /// </summary>
    /// <param name="sender">The sending object</param>
    /// <param name="e">The event arguments</param>
    private void HandlePushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
    {

      // We received a notification. There's multiple types it could be!
      switch (e.NotificationType)
      {
        case PushNotificationType.Badge:
          break;
        case PushNotificationType.Raw:
          break;
        case PushNotificationType.Tile:
          break;
        case PushNotificationType.TileFlyout:
          break;
        case PushNotificationType.Toast:
          HandleToastNotificationReceived(sender, e.ToastNotification);
          break;
      }
      Trace("===============================================");
      Trace("RAW notification arrived:");

      if ((NotificationFlag & NotificationFlagType.Raw) != NotificationFlagType.Raw)
      {
        Trace("RAW notifications not active");
        Trace("===============================================");
        return;
      }

      //
      // Parse and display the main parts of a raw notification
      //

      Uri TileUri;
      string Title;
      int Count;
      string Text1;
      string Text2;

      if (ParseRAWPayload(e.RawNotification.Content, out TileUri, out Title, out Count, out Text1, out Text2))
      {
        Trace("Raw:");
        Trace(TileUri == null ? "" : TileUri.ToString());
        Trace(Title);
        Trace(Count.ToString());
        Trace(Text1);
        Trace(Text2);

        NotifyData.BackgroundUri = TileUri;
        NotifyData.Title = Title;
        NotifyData.Count = Count;
        NotifyData.Text1 = Text1;
        NotifyData.Text2 = Text2;
      }
      else
      {
        Trace("RAW notification did not parse");
      }



      Trace("===============================================");
    }

    /// <summary>
    /// Parse and return the main parts of a raw notification
    /// </summary>
    /// <param name="stream">The raw payload stream</param>
    /// <param name="tileUri">The background tile image Uri returned</param>
    /// <param name="title">The tile image title returned</param>
    /// <param name="count">The tile image count returned</param>
    /// <param name="text1">The toast Text1 returned</param>
    /// <param name="text2">The toast Text2 returned</param>
    /// <returns>True on successful parse</returns>
    private bool ParseRAWPayload(string content, out Uri tileUri, out string title, out int count, out string text1, out string text2)
    {
      tileUri = null;
      title = null;
      count = 0;
      text1 = null;
      text2 = null;

      XElement XmlResponse = XElement.Parse(content.Replace('\0', ' '));

      if (XmlResponse == null)
      {
        return false;
      }
      //
      // Raw payload format:
      //
      // <TileService>
      //   <BackgroundImage><background image path></BackgroundImage>
      //   <Count>3</Count>
      //   <Title>5 Northgate<Title>
      //   <Text1>5 Northgate<Text1>
      //   <Text2>3 Minutes Late<Text2>
      //   <stopId>1_590</stopId>
      //   <tripId>1_15437426</tripId>
      //   <status> 
      //     <serviceDate>1271401200000</serviceDate>            
      //     <position>                                           [if predicted]
      //       <lat>47.66166765182482</lat>                       [if predicted]
      //       <lon>-122.34439975182481</lon>                     [if predicted]
      //     </position>                                          [if predicted]
      //     <predicted>true</predicted> 
      //     <scheduleDeviation>13</scheduleDeviation> 
      //     <vehicleId>1_4207</vehicleId>                        [if predicted]
      //     <closestStop>1_29530</closestStop>                   [if predicted]
      //     <closestStopTimeOffset>-10</closestStopTimeOffset>   [if predicted]
      //   </status>
      // </TileService>

      if (XmlResponse.Name.LocalName == "TileService")
      {
        string Tile;

        Tile = (string)XmlResponse.Element("BackgroundImage");

        if (Tile != null)
          tileUri = new Uri(Tile);

        count = (int?)XmlResponse.Element("Count") ?? 0;
        title = (string)XmlResponse.Element("Title");

        text1 = (string)XmlResponse.Element("Text1");
        text2 = (string)XmlResponse.Element("Text2");

        //
        // If nothing parsed then we fail
        //

        if (Tile == null && count == 0 && title == null && text1 == null && text2 == null)
          return false;

        return true;
      }

      return false;
    }

  }

  #region TestUIVisualState

  /// <summary>
  /// A class to hold the visual data
  /// </summary>
  public class NotificationData : INotifyPropertyChanged
  {
    private string _Title;

    /// <summary>
    /// The background tile image title
    /// </summary>
    public string Title
    {
      get { return _Title; }
      set
      {
        _Title = value;
        NotifyPropertyChanged("Title");
      }
    }

    private int _Count;

    /// <summary>
    /// The tile count
    /// </summary>
    public int Count
    {
      get { return _Count; }
      set
      {
        _Count = value;
        NotifyPropertyChanged("Count");
      }
    }

    private Uri _BackgroundUri;

    /// <summary>
    /// The background tile image
    /// </summary>
    public Uri BackgroundUri
    {
      get { return _BackgroundUri; }
      set
      {
        _BackgroundUri = value;
        NotifyPropertyChanged("BackgroundUri");
      }
    }

    private string _Text1;

    /// <summary>
    /// The toast Text1 message
    /// </summary>
    public string Text1
    {
      get { return _Text1; }
      set
      {
        _Text1 = value;
        NotifyPropertyChanged("Text1");
      }
    }

    private string _Text2;

    /// <summary>
    /// The toast Text2 message
    /// </summary>
    public string Text2
    {
      get { return _Text2; }
      set
      {
        _Text2 = value;
        NotifyPropertyChanged("Text2");
      }
    }

    #region INotifyPropertyChanged members
    /// <summary>
    /// The change notification event
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged(string info)
    {
      var handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(info));
      }
    }
    #endregion
  }

  #endregion
}
