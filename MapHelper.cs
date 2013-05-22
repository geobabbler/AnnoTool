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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Symbols;
using Utilities;

namespace Main.Helpers
{
    public class MapHelper
    {
        private Map _map = null;
        private Stack<Envelope> _prevExtents = new Stack<Envelope>();
        private Stack<Envelope> _nextExtents = new Stack<Envelope>();

        public MapHelper(Map map)
        {
            _map = map;
        }

        public Map Map
        {
            get { return _map; }
            //set
            //{
            //    _map = value;
            //    _extents = new Stack<Envelope>();
            //}
        }

        public Stack<Envelope> PreviousExtents
        {
            get { return _prevExtents; }
        }

        public Stack<Envelope> NextExtents
        {
            get { return _nextExtents; }
        }

        public Envelope GetPreviousExtent()
        {
            Envelope retval = null;
            try
            {
                if (_prevExtents.Count > 0)
                {
                    retval = _prevExtents.Pop();
                    _nextExtents.Push(_map.Extent);
                }
                return retval;
            }
            catch
            {
                return null;
            }
        }

        public Envelope GetNextExtent()
        {
            Envelope retval = null;
            try
            {
                if (_nextExtents.Count > 0)
                {
                    retval = _nextExtents.Pop();
                    _prevExtents.Push(_map.Extent);
                }
                return retval;
            }
            catch
            {
                return null;
            }
        }

        public void SaveMap()
        {
            DebugConsole.debug("Save happens here");
        }

        public void LoadMap()
        {
            DebugConsole.debug("Load happens here");
        }

        public void addKmlLayer(string kmlUri, string layerID, double opacity)
        {
            try
            {
                if (layerID == "")
                {
                    layerID = Guid.NewGuid().ToString();
                }
                ESRI.ArcGIS.Samples.Kml.KmlLayer lyr = new ESRI.ArcGIS.Samples.Kml.KmlLayer();
                lyr.Initialized += new EventHandler<EventArgs>(lyr_Initialized);
                lyr.ProxyUrl = "../ProxyHandler.ashx";
                lyr.Source = new Uri(kmlUri);
                lyr.Opacity = opacity;
                lyr.ID = layerID;
                lyr.Visible = true;
                
                lyr.Initialize();
                //_map.Layers.Add(lyr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("HapHelper: " + ex.ToString(), "Error", MessageBoxButton.OK);
            }
        }

        void lyr_Initialized(object sender, EventArgs e)
        {
            ESRI.ArcGIS.Samples.Kml.KmlLayer lyr = sender as ESRI.ArcGIS.Samples.Kml.KmlLayer;
            //DebugConsole.debug(lyr.Graphics.Count.ToString());
            try
            {
                _map.Layers.Add(lyr);
            }
            catch (Exception ex)
            {
                DebugConsole.debug("MapHelper: " + ex.ToString());
            }
            //throw new NotImplementedException();
        }

        public void addGeoRssLayer(string uri, string layerID, string symbolUri, bool enableAutoRefresh, double opacity)
        {
            try
            {
                if (layerID == "")
                {
                    layerID = Guid.NewGuid().ToString();
                }
                if (symbolUri == "")
                {
                    symbolUri = "Images/Symbols/rss16.png";
                }
                ESRI.ArcGIS.Samples.GeoRssLayer lyr = new ESRI.ArcGIS.Samples.GeoRssLayer();
                lyr.Source = new Uri(uri);
                PictureMarkerSymbol sym = new PictureMarkerSymbol();
                System.Windows.Media.Imaging.BitmapImage bmi = new System.Windows.Media.Imaging.BitmapImage(new Uri(symbolUri, UriKind.RelativeOrAbsolute));
                sym.Source = bmi;
                //sym.Width = symwidth;
                //sym.Height = symheight;
                lyr.Symbol = sym;
                lyr.Opacity = opacity;
                lyr.ID = layerID;
                if (enableAutoRefresh)
                {
                    lyr.EnableAutoRefresh();
                }
                lyr.Visible = true;
                _map.Layers.Add(lyr);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Map Helper" + ex.ToString(), "Error", MessageBoxButton.OK);
            }
            //sym.Source = 
        }

        /// <summary>
        /// Adds an OGC Web Mapping Services (WMS) layer to the map
        /// </summary>
        /// <param name="serverUrl">Base URL to the WMS server</param>
        /// <param name="layerID">ID of the WMS layer to be added (from the WMS GetCapabilities response)</param>
        /// <param name="opacity">The level of opacity desired for the layer. 0 (transparent) to 1 (opaque).</param>
        /// <param name="WmsVersion">The version of the WMS spec to be handled</param>
        /// <param name="ProxyUrl">The URL of the proxy page to broker requests</param>
        public void addWmsLayer(string serverUrl, string layerID, double opacity, string WmsVersion, string ProxyUrl)
        {
            try
            {
                ESRI.ArcGIS.Samples.WMS.WMSMapServiceLayer lyr = new ESRI.ArcGIS.Samples.WMS.WMSMapServiceLayer();
                lyr.Url = serverUrl;
                lyr.ID = layerID;
                lyr.Opacity = opacity;
                lyr.ProxyUrl = ProxyUrl;

                string[] lyrs = new string[1] { layerID };
                lyr.Layers = lyrs;
                lyr.Version = WmsVersion;
                lyr.Visible = true;
                _map.Layers.Add(lyr);
            }
            catch (Exception ex)
            {
            }
        }

        public void addArcGisTiledService(string url, string layerID, double opacity, string ProxyUrl)
        {
            try
            {
                if (layerID == "")
                {
                    layerID = Guid.NewGuid().ToString();
                }
                ESRI.ArcGIS.Client.ArcGISTiledMapServiceLayer lyr = new ESRI.ArcGIS.Client.ArcGISTiledMapServiceLayer();
                lyr.Url = url;
                lyr.ID = layerID;
                lyr.Opacity = opacity;
                lyr.Visible = true;
                lyr.ProxyURL = ProxyUrl;
                _map.Layers.Insert(0, lyr);
                //_map.Layers.Add(lyr);
            }
            catch (Exception ex)
            {
                DebugConsole.debug("MapHelper: " + ex.ToString());
            }
        }

        public void addArcGisDynamicService(string url, string layerID, double opacity, string ProxyUrl)
        {
            try
            {
                if (layerID == "")
                {
                    layerID = Guid.NewGuid().ToString();
                }
                ESRI.ArcGIS.Client.ArcGISDynamicMapServiceLayer lyr = new ESRI.ArcGIS.Client.ArcGISDynamicMapServiceLayer();
                lyr.Url = url;
                lyr.ID = layerID;
                lyr.Opacity = opacity;
                lyr.Visible = true;
                lyr.ProxyURL = ProxyUrl;
                _map.Layers.Add(lyr);
                //_map.Layers.Insert(0, lyr);

            }
            catch (Exception ex)
            {
                DebugConsole.debug("MapHelper: " + ex.ToString());
            }
        }

        private string agsUrl { get; set; }
        private string agsLayerID { get; set; }
        private double agsOpacity { get; set; }
        public string proxyUrl { get; set; }

        public void getArcGisService(string url, string layerID, double opacity, string ProxyUrl)
        {
            this.agsUrl = url;
            this.agsLayerID = layerID;
            this.agsOpacity = opacity;
            this.proxyUrl = ProxyUrl;
            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(this.DownloadMapServiceCompleted);
            Uri resource = PrefixProxy(ProxyUrl, url + "?f=json");
            //.Show(resource.ToString());

            webClient.DownloadStringAsync(resource);

        }

        private void DownloadMapServiceCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    DebugConsole.debug("DownloadMapServiceCompleted : " + e.Error);

                    //base.InitializationFailure = e.Error;
                    //base.Initialize();
                    //return;
                    throw e.Error;
                }
                if (string.IsNullOrEmpty(e.Result))
                {
                    DebugConsole.debug("DownloadMapServiceCompleted : " + e.Error);

                    //base.InitializationFailure = new Exception();
                    //base.Initialize();
                    //return;
                    throw new Exception("Call to external map server returned no response.");
                }

                string json = e.Result;
                Byte[] bytes = System.Text.Encoding.Unicode.GetBytes(json);
                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(bytes);

                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(EWEMA.Types.Mapping.ESRI.MapServiceInfo));
                EWEMA.Types.Mapping.ESRI.MapServiceInfo ms = dataContractJsonSerializer.ReadObject(memoryStream) as EWEMA.Types.Mapping.ESRI.MapServiceInfo;
                memoryStream.Close();

                if (ms.SingleFusedMapCache)
                {
                    //base.InitializationFailure = new Exception();
                    //base.Initialize();
                    this.addArcGisTiledService(this.agsUrl, this.agsLayerID, this.agsOpacity, this.proxyUrl);

                }
                else
                {
                    this.addArcGisDynamicService(this.agsUrl, this.agsLayerID, this.agsOpacity, this.proxyUrl);
                }
            }
            catch (Exception ex)
            {
               // DebugConsole.debug ("There was an error adding your layer. Error text: " + ex.ToString());
            }

            //Initialize(ms);

            //base.Initialize();
        }

        private Uri PrefixProxy(string ProxyUrl, string url)
        {
            if (string.IsNullOrEmpty(ProxyUrl))
                return new Uri(url, UriKind.RelativeOrAbsolute);
            string proxyUrl = ProxyUrl;
            if (!proxyUrl.Contains("?"))
            {
                if (!proxyUrl.EndsWith("?"))
                    proxyUrl = ProxyUrl + "?";
            }
            else
            {
                if (!proxyUrl.EndsWith("&"))
                    proxyUrl = ProxyUrl + "&";
            }

#if SILVERLIGHT
            if (ProxyUrl.StartsWith("~") || ProxyUrl.StartsWith("../")) //relative to xap root
            {
                string uri = Application.Current.Host.Source.AbsoluteUri;
                int count = proxyUrl.Split(new string[] { "../" }, StringSplitOptions.None).Length;
                for (int i = 0; i < count; i++)
                {
                    uri = uri.Substring(0, uri.LastIndexOf("/"));
                }
                if (!uri.EndsWith("/"))
                    uri += "/";
                proxyUrl = uri + proxyUrl.Replace("~", "").Replace("../", "");
            }
            else if (ProxyUrl.StartsWith("/")) //relative to domain root
            {
                proxyUrl = ProxyUrl.Replace("/", string.Format("{0}://{1}:{2}",
                    Application.Current.Host.Source.Scheme,
                    Application.Current.Host.Source.Host,
                    Application.Current.Host.Source.Port));
            }
#endif

            UriBuilder b = new UriBuilder(proxyUrl);
            b.Query = url;
            return b.Uri;
        }

    }
}
