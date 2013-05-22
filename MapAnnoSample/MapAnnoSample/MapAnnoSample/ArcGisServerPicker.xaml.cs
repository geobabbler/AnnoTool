using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using ESRI.ArcGIS.Client;


namespace MapAnnoSample
{
    public partial class ArcGisServerPicker : ChildWindow
    {
        public ArcGisServerPicker()
        {
            InitializeComponent();
            this.baseUrl = "http://server.arcgisonline.com/ArcGIS/rest/services";
        }

        public string baseUrl { get; set; }
        public Map BoundMap { get; set; }

        private Dictionary<string, string> _layers = new Dictionary<string, string>();

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._layers.Count > 0)
            {
                foreach (string key in _layers.Keys)
                {
                    ArcGISDynamicMapServiceLayer lyr = new ArcGISDynamicMapServiceLayer();
                    lyr.Url = key;
                    BoundMap.Layers.Add(lyr);
                }
            }
            this.DialogResult = true;
        }

        public void loadServices()
        {
            string url = this.baseUrl + "?f=json";
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new System.Net.DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            client.DownloadStringAsync(new Uri(url));
        }

        void client_DownloadStringCompleted(object sender, System.Net.DownloadStringCompletedEventArgs e)
        {
            string s = e.Result;
            byte[] b = Encoding.Unicode.GetBytes(s);
            MemoryStream st = new MemoryStream(b);
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(ServerInfo));
            ServerInfo si = (ServerInfo)ds.ReadObject(st);
            loadFolders(si);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void loadFolders(ServerInfo si)
        {
            foreach (string folder in si.Folders)
            {
                ArcServerFolder f = new ArcServerFolder(folder, this.baseUrl);
                f.ServicesLoaded += new EventHandler(f_ServicesLoaded);
                f.GetServices();
                
            }

        }

        void f_ServicesLoaded(object sender, EventArgs e)
        {
            ArcServerFolder f = sender as ArcServerFolder;
            AccordionItem itm = new AccordionItem();
            itm.Header = f.folderName;
            ScrollViewer scroll = new ScrollViewer();
            scroll.Width = this.accordion1.ActualWidth - 10;
            StackPanel stack = new StackPanel();
            foreach (ServiceDef svc in f.Services)
            {
                if (svc.Type.ToLower() == "mapserver")
                {
                    CheckBox chk = new CheckBox();
                    chk.Content = svc.Name.Replace(f.folderName + "/", "");
                    chk.Tag = f.parentUrl + "/" + svc.Name + "/MapServer";
                    chk.Checked += new RoutedEventHandler(chk_Checked);
                    chk.Unchecked += new RoutedEventHandler(chk_Unchecked);
                    stack.Children.Add(chk);
                }
            }
            scroll.Content = stack;
            itm.Content = scroll;
            if (stack.Children.Count > 0)
            {
                this.accordion1.Items.Add(itm);
            }
            
        }

        void chk_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            try
            {
                _layers.Remove(chk.Tag.ToString());
            }
            catch
            {
            }
        }

        void chk_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = sender as CheckBox;
            try
            {
                _layers.Add(chk.Tag.ToString(), chk.Tag.ToString());
            }
            catch { }
        }
    }
}

