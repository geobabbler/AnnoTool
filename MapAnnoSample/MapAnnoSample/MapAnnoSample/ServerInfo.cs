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

//using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MapAnnoSample
{
    //[Serializable]
    [DataContract()]
    public class ServerInfo
    {
        [DataMember(Name = "folders")]
        public List<string> Folders { get; set; }
        [DataMember(Name = "services")]
        public List<ServiceDef> Services { get; set; }

    }

    [DataContract]
    public class ServiceDef
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "type")]
        public string Type { get; set; }
    }

    public class ArcServerFolder
    {
        public string folderUrl { get; set; }
        public string parentUrl { get; set; }
        public string folderName { get; set; }

        private List<ServiceDef> _services = null;
        private string _jsonUrl = "";

        public event EventHandler ServicesLoaded;

        public List<ServiceDef> Services
        {
            get
            {
                return _services;
            }
        }

        public ArcServerFolder(string foldername, string parenturl)
        {
            parentUrl = parenturl;
            folderName = foldername;
            folderUrl = parenturl + "/" + foldername;
            _jsonUrl = folderUrl + "?f=json";
        }

        public void GetServices()
        {
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            client.DownloadStringAsync(new Uri(_jsonUrl));
        }

        void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string s = e.Result;
            byte[] b = Encoding.Unicode.GetBytes(s);
            MemoryStream st = new MemoryStream(b);
            DataContractJsonSerializer ds = new DataContractJsonSerializer(typeof(ServerInfo));
            ServerInfo si = (ServerInfo)ds.ReadObject(st);
            _services = si.Services;
            if (this.ServicesLoaded != null)
                ServicesLoaded(this, null);
        }
    }
}
