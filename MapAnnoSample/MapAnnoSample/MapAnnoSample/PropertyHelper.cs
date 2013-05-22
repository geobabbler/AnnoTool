using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ESRI.ArcGIS.Client;

namespace MapAnnoSample
{
    public class DynamicLayerTimer : DispatcherTimer
    {
        public DynamicLayer BoundLayer { get; set; }
    }

    public static class PropertyHelper
    {
        //DependencyProperty.Register("RefreshTimer", typeof(System.Windows.Threading.DispatcherTimer), typeof(ESRI.ArcGIS.Client.ArcGISDynamicMapServiceLayer), new PropertyMetadata(new System.Windows.Threading.DispatcherTimer()));
        //ESRI.ArcGIS.Client.ArcGISDynamicMapServiceLayer lyr = new ESRI.ArcGIS.Client.ArcGISDynamicMapServiceLayer();
        public static DependencyProperty RefreshTimerProperty = DependencyProperty.RegisterAttached(
         "RefreshTimer",
         typeof(DynamicLayerTimer),
         typeof(DynamicLayer),
         new PropertyMetadata(null)
        );

        public static void InitRefreshTimer(this DynamicLayer lyr, TimeSpan interval)
        {
            DynamicLayerTimer tmr = new DynamicLayerTimer { BoundLayer = lyr, Interval = interval };
            lyr.SetValue(RefreshTimerProperty, tmr);
            tmr.Tick += new EventHandler(tmr_Tick);
        }

        public static void StartRefreshTimer(this DynamicLayer lyr)
        {
            DynamicLayerTimer tmr = (DynamicLayerTimer)lyr.GetValue(RefreshTimerProperty);
            tmr.Start();
        }

        public static void StopRefreshTimer(this DynamicLayer lyr)
        {
            DynamicLayerTimer tmr = (DynamicLayerTimer)lyr.GetValue(RefreshTimerProperty);
            tmr.Stop();
        }
        
        static void tmr_Tick(object sender, EventArgs e)
        {
            DynamicLayerTimer tmr = sender as DynamicLayerTimer;
            tmr.BoundLayer.Refresh();
        }

    }
}
