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
using ESRI.ArcGIS.Client;

namespace Environment
{
    public delegate void AnnoEventHandler(object sender, RoutedEventArgs e);

    public static class Ambient
    {
        private static Graphic _sym = null;

        public static Grid LayoutRoot { get; set; }
        public static Graphic SelectedSymbol
        {
            get
            {
                return _sym;
            }
            set
            {
                _sym = value;
                if (SelectedSymbolChanged != null)
                    SelectedSymbolChanged(null, null);
            }
        }
        public static event AnnoEventHandler RemoveText;
        public static event AnnoEventHandler ChangeText;
        public static event EventHandler SelectedSymbolChanged;

        public static void RemoveTextHandler(object sender, RoutedEventArgs e)
        {
            if (RemoveText != null)
                RemoveText(sender, e);
        }

        public static void ChangeTextHandler(object sender, RoutedEventArgs e)
        {
            if (ChangeText != null)
                ChangeText(sender, e);
        }
    }
}
