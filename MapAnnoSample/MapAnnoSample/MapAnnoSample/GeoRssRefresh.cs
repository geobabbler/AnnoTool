using System;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Threading;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Toolkit.DataSources;
using System.Windows;

namespace Zekiah.ArcGIS.Behaviors
{
    /// <summary>
    /// Adds auto-refresh capability to GeoRSS layers 
    /// </summary>
    public class GeoRssRefresh : Behavior<GeoRssLayer>
    {
        private DispatcherTimer _tmr = null;


        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            base.OnAttached();
            initTimer();
        }

        public TimeSpan Interval
        {
            get
            {
                if (_tmr == null)
                {
                    initTimer();
                }
                return _tmr.Interval;
            }
            set
            {
                _tmr.Interval = value;
            }
        }

        public void Start()
        {
            if (_tmr == null)
                initTimer();
            if (!_tmr.IsEnabled)
                _tmr.Start();
        }

        public void Stop()
        {
            if ((_tmr != null) && (_tmr.IsEnabled))
                _tmr.Stop();
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, 
        /// but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            _tmr = null;
        }

        private void initTimer()
        {
            _tmr = new DispatcherTimer();
            _tmr.Interval = new TimeSpan(0, 0, 5); //default five second interval
            _tmr.Tick += new EventHandler(_tmr_Tick);
        }

        void _tmr_Tick(object sender, EventArgs e)
        {
            //MessageBox.Show(DateTime.Now.ToString());
            this.AssociatedObject.Update();
        }

    }
}