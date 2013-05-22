//using System;
//using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Animation;
//using System.Windows.Shapes;
//using System.Windows.Threading;
//using ESRI.ArcGIS.Client;

//namespace Zekiah.ArcGIS.Layers
//{
//    public class ArcGISDynamicMapServiceLayerEx : ArcGISDynamicMapServiceLayer
//    {
//        private DispatcherTimer _tmr = new DispatcherTimer();
//        private bool _autoRefresh = false;

//        public ArcGISDynamicMapServiceLayerEx()
//        {
//            _tmr.Interval = new TimeSpan(0, 0, 5);
//            _tmr.Tick += new EventHandler(_tmr_Tick);
//        }

//        private void _tmr_Tick(object sender, EventArgs e)
//        {
//            //ArcGISDynamicMapServiceLayer lyr = base;
//            //lyr.Refresh();
//        }

//        public bool AutoRefresh {
//            get
//            {
//                return _tmr.IsEnabled;
//            }
//            set
//            {
//                if (value)
//                {
//                    if (_tmr.Interval == null)
//                    {
//                        _tmr.Interval = new TimeSpan(0, 0, 5);
//                    }
//                    _tmr.Start();
//                }
//                else
//                {
//                    _tmr.Stop();
//                }
//            }
//        }

//        public TimeSpan Interval
//        {
//            get
//            {
//                return _tmr.Interval;
//            }
//            set
//            {
//                _tmr.Interval = value;
//            }
//        }
        

//    }
//}
