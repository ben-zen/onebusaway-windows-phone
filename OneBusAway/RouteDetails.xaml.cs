using OneBusAway.Model.BusServiceDataStructures;
using OneBusAway.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace OneBusAway.View
{
  /// <summary>
  /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
  /// </summary>
  public sealed partial class RouteDetails : Page
  {
    #region Properties
    public RouteVM VM { get; set; }
    public bool StopsLoaded { get; set; }
    #endregion
    public RouteDetails()
    {
      this.InitializeComponent();
    }

    protected async override void OnNavigatedTo(NavigationEventArgs args)
    {
      // Start the marching ants, once I figure out how to do that.
      var route = (args.Parameter as Route);
      VM = await RouteVM.GetVMForRoute(route);
      StopsLoaded = await VM.LoadStops();
      RouteStopsViewSource.Source = VM.RouteDirections;
      RecentsVM.Instance.AddRecentRoute(route);
    }

    private void StopClicked(object sender, ItemClickEventArgs e)
    {
      var stop = e.ClickedItem as Stop;
      (Window.Current.Content as Frame).Navigate(typeof(StopDetails), stop);
    }
  }
}
